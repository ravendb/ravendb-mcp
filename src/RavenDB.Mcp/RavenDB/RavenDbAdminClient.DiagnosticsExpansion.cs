using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RavenDB.Mcp.Configuration;
using RavenDB.Mcp.Tools;

namespace RavenDB.Mcp.RavenDB;

public sealed partial class RavenDbAdminClient
{
    public async Task<GetServerDiagnosticsOverviewResult> GetServerDiagnosticsOverview(CancellationToken cancellationToken)
    {
        return new GetServerDiagnosticsOverviewResult(
            await TryGetServerJson("/debug/routes", cancellationToken),
            await TryGetServerJson("/admin/configuration/settings", cancellationToken),
            await TryGetServerJson("/admin/metrics", cancellationToken),
            await TryGetServerJson("/admin/debug/cpu/credits", cancellationToken),
            await TryGetServerJson("/admin/debug/databases/loaded", cancellationToken),
            await TryGetServerJson("/admin/debug/databases/idle", cancellationToken),
            await TryGetServerJson("/admin/license/connectivity", cancellationToken),
            await TryGetServerJson("/admin/debug/cluster/maintenance", cancellationToken));
    }

    public async Task<GetClusterDiagnosticsOverviewResult> GetClusterDiagnosticsOverview(CancellationToken cancellationToken)
    {
        return new GetClusterDiagnosticsOverviewResult(
            await TryGetServerJson("/admin/debug/cluster/observer-decisions", cancellationToken),
            await TryGetServerJson("/admin/debug/cluster/log", cancellationToken),
            await TryGetServerJson("/admin/debug/cluster/history", cancellationToken),
            await TryGetServerJson("/admin/debug/cluster/remote-connections", cancellationToken),
            await TryGetServerJson("/admin/debug/cluster/engine/logs", cancellationToken),
            await TryGetServerJson("/admin/debug/cluster/state-change-history", cancellationToken));
    }

    public async Task<PingClusterNodeResult> PingClusterNode(string url, CancellationToken cancellationToken)
    {
        ValidateName(url, "Node URL", nameof(url));

        var target = url.TrimEnd('/') + "/admin/debug/node-info";
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var response = await http.GetAsync(target, cancellationToken);
            stopwatch.Stop();

            return new PingClusterNodeResult(
                url,
                (int)response.StatusCode,
                response.IsSuccessStatusCode,
                stopwatch.ElapsedMilliseconds,
                null);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            return new PingClusterNodeResult(url, null, false, stopwatch.ElapsedMilliseconds, exception.Message);
        }
    }

    public async Task<DiagnosticTextSampleResult> SampleClusterDashboard(int seconds, CancellationToken cancellationToken)
    {
        return new DiagnosticTextSampleResult(
            "cluster_dashboard",
            Math.Clamp(seconds, 1, 30),
            await GetServerTextSample("/admin/debug/cluster/dashboard", seconds, cancellationToken));
    }

    public async Task<GetIndexStalenessResult> GetIndexStaleness(
        string databaseName,
        string indexName,
        CancellationToken cancellationToken)
    {
        ValidateName(indexName, "Index name", nameof(indexName));

        return new GetIndexStalenessResult(
            databaseName,
            indexName,
            await GetDatabaseJson(databaseName, "/indexes/staleness", cancellationToken, ("name", indexName)));
    }

    public async Task<GetIndexDebugDetailsResult> GetIndexDebugDetails(
        string databaseName,
        string indexName,
        CancellationToken cancellationToken)
    {
        ValidateName(indexName, "Index name", nameof(indexName));

        return new GetIndexDebugDetailsResult(
            databaseName,
            indexName,
            await TryGetDatabaseJson(databaseName, "/indexes/debug", cancellationToken, ("name", indexName)),
            await TryGetDatabaseJson(databaseName, "/indexes/debug/metadata", cancellationToken, ("name", indexName)),
            await TryGetDatabaseJson(databaseName, "/indexes/debug/history", cancellationToken, ("name", indexName)));
    }

    public async Task<GetQueryDiagnosticsResult> GetQueryDiagnostics(string databaseName, CancellationToken cancellationToken)
    {
        return new GetQueryDiagnosticsResult(
            databaseName,
            await TryGetDatabaseJson(databaseName, "/queries/running", cancellationToken),
            await TryGetDatabaseJson(databaseName, "/queries/cache", cancellationToken));
    }

    public async Task<GetOperationsOverviewResult> GetOperationsOverview(
        string? databaseName,
        CancellationToken cancellationToken)
    {
        return new GetOperationsOverviewResult(
            databaseName,
            string.IsNullOrWhiteSpace(databaseName)
                ? await TryGetServerJson("/operations", cancellationToken)
                : await TryGetDatabaseJson(databaseName, "/operations", cancellationToken),
            await TryGetServerJson("/admin/operations", cancellationToken));
    }

    public async Task<GetTransactionDiagnosticsResult> GetTransactionDiagnostics(
        string? databaseName,
        CancellationToken cancellationToken)
    {
        return new GetTransactionDiagnosticsResult(
            databaseName,
            await TryGetServerJson("/admin/debug/txinfo", cancellationToken),
            string.IsNullOrWhiteSpace(databaseName)
                ? ToJson(new { available = false, error = "databaseName was not provided." })
                : await TryGetDatabaseJson(databaseName, "/debug/txinfo", cancellationToken),
            string.IsNullOrWhiteSpace(databaseName)
                ? ToJson(new { available = false, error = "databaseName was not provided." })
                : await TryGetDatabaseJson(databaseName, "/debug/cluster/txinfo", cancellationToken));
    }

    public async Task<WaitForConditionResult> WaitForOperation(
        string databaseName,
        long operationId,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow.AddSeconds(Math.Clamp(timeoutSeconds, 1, 300));
        var polls = 0;
        JsonElement state = default;

        while (DateTime.UtcNow <= deadline)
        {
            polls++;
            state = (await GetOperationState(databaseName, operationId, cancellationToken)).State;

            if (LooksComplete(state))
                return new WaitForConditionResult("operation", databaseName, operationId, null, true, polls, state);

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }

        return new WaitForConditionResult("operation", databaseName, operationId, null, false, polls, state);
    }

    public async Task<WaitForConditionResult> WaitForIndexing(
        string databaseName,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow.AddSeconds(Math.Clamp(timeoutSeconds, 1, 300));
        var polls = 0;
        JsonElement state = default;

        while (DateTime.UtcNow <= deadline)
        {
            polls++;
            state = (await GetIndexingStatus(databaseName, cancellationToken)).Status;

            if (!state.GetRawText().Contains("\"Stale\":true", StringComparison.OrdinalIgnoreCase))
                return new WaitForConditionResult("indexing", databaseName, null, null, true, polls, state);

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }

        return new WaitForConditionResult("indexing", databaseName, null, null, false, polls, state);
    }

    public async Task<GetDocumentConflictsResult> GetDocumentConflicts(
        string databaseName,
        string documentId,
        CancellationToken cancellationToken)
    {
        ValidateName(documentId, "Document id", nameof(documentId));

        return new GetDocumentConflictsResult(
            databaseName,
            documentId,
            await GetDatabaseJson(databaseName, "/replication/conflicts", cancellationToken, ("docId", documentId)));
    }

    public async Task<GetBackupDiagnosticsResult> GetBackupDiagnostics(
        string databaseName,
        CancellationToken cancellationToken)
    {
        var tasks = await GetBackupTasks(databaseName, cancellationToken);

        return new GetBackupDiagnosticsResult(
            databaseName,
            tasks.Tasks,
            await TryGetDatabaseJson(databaseName, "/periodic-backup/next-backup", cancellationToken),
            await TryGetServerJson("/admin/configuration/server-wide/backup", cancellationToken));
    }

    public async Task<GetEtlDiagnosticsResult> GetEtlDiagnostics(
        string databaseName,
        CancellationToken cancellationToken)
    {
        var tasks = await GetEtlTasks(databaseName, cancellationToken);

        return new GetEtlDiagnosticsResult(
            databaseName,
            tasks.Tasks,
            await TryGetDatabaseJson(databaseName, "/etl/stats", cancellationToken),
            await TryGetDatabaseJson(databaseName, "/etl/performance", cancellationToken),
            await TryGetDatabaseJson(databaseName, "/etl/debug/stats", cancellationToken),
            await TryGetDatabaseJson(databaseName, "/etl/progress", cancellationToken));
    }

    public async Task<GetSubscriptionDiagnosticsResult> GetSubscriptionDiagnostics(
        string databaseName,
        CancellationToken cancellationToken)
    {
        var subscriptions = await GetSubscriptions(databaseName, cancellationToken);

        return new GetSubscriptionDiagnosticsResult(
            databaseName,
            subscriptions.Subscriptions,
            await TryGetDatabaseJson(databaseName, "/subscriptions/running", cancellationToken),
            await TryGetDatabaseJson(databaseName, "/subscriptions/connection-details", cancellationToken));
    }

    public async Task<GetTrafficWatchConfigurationResult> GetTrafficWatchConfiguration(CancellationToken cancellationToken)
    {
        return new GetTrafficWatchConfigurationResult(
            await GetServerJson("/admin/traffic-watch/configuration", cancellationToken));
    }

    public async Task<SearchLogsResult> SearchLogs(
        DateTime? from,
        DateTime? to,
        string? text,
        string? level,
        string? source,
        int? pageSize,
        CancellationToken cancellationToken)
    {
        return new SearchLogsResult(
            await GetServerJson(
                "/admin/logs/search",
                cancellationToken,
                ("from", from?.ToString("O")),
                ("to", to?.ToString("O")),
                ("text", text),
                ("level", level),
                ("source", source),
                ("pageSize", pageSize?.ToString())));
    }

    public Task<DiagnosticArtifactResult> ExportAdminLogs(
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken)
    {
        return SaveServerArtifact(
            "admin-logs",
            "/admin/logs/admin",
            cancellationToken,
            ("from", from?.ToString("O")),
            ("to", to?.ToString("O")));
    }

    public Task<DiagnosticArtifactResult> ExportAuditLogs(
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken)
    {
        return SaveServerArtifact(
            "audit-logs",
            "/admin/logs/audit",
            cancellationToken,
            ("from", from?.ToString("O")),
            ("to", to?.ToString("O")));
    }

    public Task<DiagnosticArtifactResult> ExportTrafficWatch(
        DateTime? from,
        DateTime? to,
        string? databaseName,
        CancellationToken cancellationToken)
    {
        return SaveServerArtifact(
            "traffic-watch",
            "/admin/traffic-watch",
            cancellationToken,
            ("from", from?.ToString("O")),
            ("to", to?.ToString("O")),
            ("database", databaseName));
    }

    public async Task<GetNotificationsResult> GetNotifications(
        string? databaseName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(databaseName))
            return new GetNotificationsResult(null, await GetServerJson("/notifications", cancellationToken));

        return new GetNotificationsResult(
            databaseName,
            await GetDatabaseJson(databaseName, "/notifications", cancellationToken));
    }

    public async Task<DiagnosticTextSampleResult> SampleAdminLogs(int seconds, CancellationToken cancellationToken)
    {
        return new DiagnosticTextSampleResult(
            "admin_logs",
            Math.Clamp(seconds, 1, 30),
            await GetServerTextSample("/admin/logs/watch", seconds, cancellationToken));
    }

    public async Task<DiagnosticTextSampleResult> SampleTrafficWatch(int seconds, CancellationToken cancellationToken)
    {
        return new DiagnosticTextSampleResult(
            "traffic_watch",
            Math.Clamp(seconds, 1, 30),
            await GetServerTextSample("/admin/traffic-watch/watch", seconds, cancellationToken));
    }

    public async Task<GetCollectionSampleShapeResult> GetCollectionSampleShape(
        string databaseName,
        string collectionName,
        int? sampleSize,
        CancellationToken cancellationToken)
    {
        ValidateName(collectionName, "Collection name", nameof(collectionName));

        var query = await PostDatabaseJson(
            databaseName,
            "/queries",
            new
            {
                Query = $"from {collectionName}",
                Start = 0,
                PageSize = Math.Clamp(sampleSize ?? 5, 1, 25)
            },
            cancellationToken);

        return new GetCollectionSampleShapeResult(
            databaseName,
            collectionName,
            SummarizeDocumentShape(query));
    }

    public async Task<GetHugeDocumentsReportResult> GetHugeDocumentsReport(
        string databaseName,
        CancellationToken cancellationToken)
    {
        return new GetHugeDocumentsReportResult(
            databaseName,
            await GetDatabaseJson(databaseName, "/debug/documents/huge", cancellationToken));
    }

    public Task<DiagnosticArtifactResult> ScanCorruptedDocumentIds(
        string databaseName,
        CancellationToken cancellationToken)
    {
        return SaveDatabaseArtifact(databaseName, "corrupted-document-ids", "/debug/documents/corrupted", cancellationToken);
    }

    public async Task<GetDocumentRevisionsResult> GetDocumentRevisions(
        string databaseName,
        string documentId,
        int? start,
        int? pageSize,
        CancellationToken cancellationToken)
    {
        ValidateName(documentId, "Document id", nameof(documentId));

        return new GetDocumentRevisionsResult(
            databaseName,
            documentId,
            await GetDatabaseJson(
                databaseName,
                "/revisions",
                cancellationToken,
                ("id", documentId),
                ("start", Math.Max(start ?? 0, 0).ToString()),
                ("pageSize", Math.Clamp(pageSize ?? 25, 1, 1024).ToString())));
    }

    public Task<DiagnosticArtifactResult> ExportDocumentIds(
        string databaseName,
        string? startsWith,
        int? pageSize,
        CancellationToken cancellationToken)
    {
        return SaveDatabaseArtifact(
            databaseName,
            "document-ids",
            "/docs",
            cancellationToken,
            ("startsWith", startsWith),
            ("metadataOnly", "true"),
            ("pageSize", Math.Clamp(pageSize ?? 1024, 1, 10_000).ToString()));
    }

    public Task<DiagnosticArtifactResult> FindMissingAttachments(
        string databaseName,
        CancellationToken cancellationToken)
    {
        return SaveDatabaseArtifact(databaseName, "missing-attachments", "/debug/attachments/missing", cancellationToken);
    }

    public async Task<GetRevisionsCollectionStatsResult> GetRevisionsCollectionStats(
        string databaseName,
        CancellationToken cancellationToken)
    {
        return new GetRevisionsCollectionStatsResult(
            databaseName,
            await GetDatabaseJson(databaseName, "/revisions/stats", cancellationToken));
    }

    public async Task<QueryMetadataOnlyResult> QueryMetadataOnly(
        string databaseName,
        string query,
        int? pageSize,
        CancellationToken cancellationToken)
    {
        ValidateName(query, "Query", nameof(query));

        var result = await PostDatabaseJson(
            databaseName,
            "/queries",
            new
            {
                Query = query,
                Start = 0,
                PageSize = Math.Clamp(pageSize ?? 0, 0, 32)
            },
            cancellationToken);

        return new QueryMetadataOnlyResult(databaseName, SelectQueryMetadata(result));
    }

    public Task<DiagnosticArtifactResult> CollectServerInfoPackage(CancellationToken cancellationToken)
    {
        return SaveServerArtifact("server-info-package", "/admin/debug/info-package", cancellationToken);
    }

    public Task<DiagnosticArtifactResult> CollectClusterInfoPackage(CancellationToken cancellationToken)
    {
        return SaveServerArtifact("cluster-info-package", "/admin/debug/cluster-info-package", cancellationToken);
    }

    public Task<DiagnosticArtifactResult> CollectDatabaseInfoPackage(
        string databaseName,
        CancellationToken cancellationToken)
    {
        return SaveDatabaseArtifact(databaseName, "database-info-package", "/debug/info-package", cancellationToken);
    }

    public async Task<CollectDiagnosticSnapshotResult> CollectDiagnosticSnapshot(
        string databaseName,
        CancellationToken cancellationToken)
    {
        var cluster = await GetClusterNodes(cancellationToken);
        var database = await GetDatabaseOverview(databaseName, cancellationToken);
        var indexes = await GetIndexingOverview(databaseName, cancellationToken);
        var tasks = await GetDatabaseTasks(databaseName, cancellationToken);
        var notifications = await TryGetDatabaseJson(databaseName, "/notifications", cancellationToken);
        var package = await CollectDatabaseInfoPackage(databaseName, cancellationToken);

        return new CollectDiagnosticSnapshotResult(
            databaseName,
            ToJson(cluster),
            ToJson(database),
            ToJson(indexes),
            tasks.Tasks,
            notifications,
            package);
    }

    private async Task<JsonElement> TryGetServerJson(
        string path,
        CancellationToken cancellationToken,
        params (string Name, string? Value)[] query)
    {
        try
        {
            return ToJson(new
            {
                available = true,
                value = await GetServerJson(path, cancellationToken, query)
            });
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            return ToJson(new
            {
                available = false,
                error = exception.Message
            });
        }
    }

    private async Task<JsonElement> TryGetDatabaseJson(
        string databaseName,
        string path,
        CancellationToken cancellationToken,
        params (string Name, string? Value)[] query)
    {
        try
        {
            return ToJson(new
            {
                available = true,
                value = await GetDatabaseJson(databaseName, path, cancellationToken, query)
            });
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            return ToJson(new
            {
                available = false,
                error = exception.Message
            });
        }
    }

    private async Task<JsonElement> PostDatabaseJson(
        string databaseName,
        string path,
        object body,
        CancellationToken cancellationToken,
        params (string Name, string? Value)[] query)
    {
        ValidateDatabaseName(databaseName);

        var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        using var response = await http.PostAsync(BuildDatabaseUrl(databaseName, path, query), content, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"POST {path} failed with {(int)response.StatusCode}: {responseText}");

        return JsonSerializer.Deserialize<JsonElement>(responseText);
    }

    private async Task<DiagnosticArtifactResult> SaveServerArtifact(
        string name,
        string path,
        CancellationToken cancellationToken,
        params (string Name, string? Value)[] query)
    {
        return await SaveArtifact(name, await GetBytes(BuildServerUrl(path, query), cancellationToken), cancellationToken);
    }

    private async Task<DiagnosticArtifactResult> SaveDatabaseArtifact(
        string databaseName,
        string name,
        string path,
        CancellationToken cancellationToken,
        params (string Name, string? Value)[] query)
    {
        return await SaveArtifact(
            $"{databaseName}-{name}",
            await GetBytes(BuildDatabaseUrl(databaseName, path, query), cancellationToken),
            cancellationToken);
    }

    private async Task<(byte[] Bytes, string ContentType)> GetBytes(string url, CancellationToken cancellationToken)
    {
        using var response = await http.GetAsync(url, cancellationToken);
        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"GET {url} failed with {(int)response.StatusCode}: {Encoding.UTF8.GetString(bytes)}");

        return (bytes, response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream");
    }

    private async Task<DiagnosticArtifactResult> SaveArtifact(
        string name,
        (byte[] Bytes, string ContentType) content,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(artifactsPath);

        var fileName = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}-{SanitizeFileName(name)}.bin";
        var path = Path.Combine(artifactsPath, fileName);
        await File.WriteAllBytesAsync(path, content.Bytes, cancellationToken);

        return new DiagnosticArtifactResult(path, content.ContentType, content.Bytes.LongLength);
    }

    private static JsonElement SummarizeDocumentShape(JsonElement queryResult)
    {
        if (!queryResult.TryGetProperty("Results", out var results) || results.ValueKind != JsonValueKind.Array)
            return ToJson(new { documents = 0, fields = new Dictionary<string, string[]>() });

        var fields = new Dictionary<string, SortedSet<string>>(StringComparer.OrdinalIgnoreCase);
        var count = 0;

        foreach (var document in results.EnumerateArray())
        {
            if (document.ValueKind != JsonValueKind.Object)
                continue;

            count++;

            foreach (var property in document.EnumerateObject())
            {
                if (property.NameEquals("@metadata"))
                    continue;

                if (!fields.TryGetValue(property.Name, out var kinds))
                {
                    kinds = [];
                    fields[property.Name] = kinds;
                }

                kinds.Add(property.Value.ValueKind.ToString());
            }
        }

        return ToJson(new
        {
            documents = count,
            fields = fields.ToDictionary(item => item.Key, item => item.Value.ToArray())
        });
    }

    private static JsonElement SelectQueryMetadata(JsonElement queryResult)
    {
        return ToJson(SelectProperties(
            queryResult,
            "DurationInMs",
            "TotalResults",
            "SkippedResults",
            "ScannedResults",
            "IndexName",
            "IsStale",
            "LastQueryTime",
            "ResultEtag"));
    }

    private static bool LooksComplete(JsonElement state)
    {
        var raw = state.GetRawText();
        return raw.Contains("\"Completed\":true", StringComparison.OrdinalIgnoreCase)
            || raw.Contains("\"Status\":\"Completed\"", StringComparison.OrdinalIgnoreCase)
            || raw.Contains("\"State\":\"Completed\"", StringComparison.OrdinalIgnoreCase);
    }

    private static string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(value.Length);

        foreach (var character in value)
            builder.Append(invalid.Contains(character) ? '-' : character);

        return builder.ToString();
    }
}
