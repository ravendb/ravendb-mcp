using System.Text.Json;
using System.Text.Json.Nodes;
using Raven.Client.Documents.Operations;
using Raven.Client.ServerWide.Commands;
using RavenDB.Mcp.Tools;

namespace RavenDB.Mcp.RavenDB;

public sealed partial class RavenDbAdminClient
{
    public async Task<GetServerDiagnosticsOverviewResult> GetServerDiagnosticsOverview(CancellationToken cancellationToken)
    {
        var metricsTask = TryGetServerJson("/admin/metrics", cancellationToken);
        var cpuCreditsTask = TryGetServerJson("/debug/cpu-credits", cancellationToken);
        var idleTask = TryGetServerJson("/admin/debug/databases/idle", cancellationToken);
        var licenseTask = TryGetServerJson("/license-server/connectivity", cancellationToken);
        var maintenanceTask = TryGetServerJson("/admin/cluster/maintenance-stats", cancellationToken);
        await Task.WhenAll(metricsTask, cpuCreditsTask, idleTask, licenseTask, maintenanceTask);

        return new GetServerDiagnosticsOverviewResult(
            await metricsTask,
            await cpuCreditsTask,
            await idleTask,
            await licenseTask,
            await maintenanceTask);
    }

    public async Task<JsonElement> GetServerSettings(string? keyPrefix, CancellationToken cancellationToken)
        => FilterServerSettings(
            RedactSecrets(await TryGetServerJson("/admin/configuration/settings", cancellationToken)),
            keyPrefix);

    // The full config dump is ~200KB, so this is progressive: no prefix returns an index of the
    // available key prefixes; a prefix returns the matching settings entries.
    //
    // The index counts overridden settings per prefix, so a caller can skip the prefixes that
    // hold nothing but defaults instead of fetching all of them to find out.
    internal static JsonElement FilterServerSettings(JsonElement settings, string? keyPrefix)
    {
        if (settings.ValueKind != JsonValueKind.Object)
            return settings;

        var root = JsonNode.Parse(settings.GetRawText())!;
        var container = root["value"] as JsonObject ?? root.AsObject();
        if (container["Settings"] is not JsonArray entries)
            return settings;

        static IEnumerable<string> KeysOf(JsonNode? entry) =>
            (entry?["Metadata"]?["Keys"] as JsonArray ?? []).Select(k => k!.GetValue<string>());

        // Settings keys are dotted paths, so "Indexing.MapTimeoutInSec" belongs to prefix
        // "Indexing". Cutting at the first dot avoids allocating the rest of the path, which is
        // thrown away.
        static string PrefixOf(string key)
        {
            var dot = key.IndexOf('.');
            return dot < 0 ? key : key[..dot];
        }

        if (string.IsNullOrWhiteSpace(keyPrefix))
        {
            var total = new SortedDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var overridden = new SortedDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in entries)
            {
                var isSet = entry?["ServerValues"] is JsonObject { Count: > 0 };
                foreach (var prefix in KeysOf(entry).Select(PrefixOf))
                {
                    total[prefix] = total.GetValueOrDefault(prefix) + 1;
                    if (isSet)
                        overridden[prefix] = overridden.GetValueOrDefault(prefix) + 1;
                }
            }

            return ToJson(new
            {
                available = true,
                totalEntries = entries.Count,
                overriddenTotal = overridden.Values.Sum(),
                overridden,
                prefixes = total,
                hint = overridden.Count > 0
                    ? "Only the prefixes under 'overridden' have a value set on this server; the rest "
                      + "are product defaults. Pass those to settingsPrefix, comma-separated, in one call."
                    : "Nothing is overridden on this server: every setting is at its product default, so "
                      + "fetching prefixes will return defaults only. Pass settingsPrefix just to read a "
                      + "specific default value.",
            });
        }

        // A string rather than an array: the package is published, so an array would break existing
        // callers, and a comma cannot occur in a configuration key.
        var wanted = keyPrefix.Split(',',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var totalEntries = entries.Count;

        // One pass over the entries rather than one per prefix, and nothing is removed from the
        // source array: Compact below deep-clones every value it keeps, so the original nodes are
        // never re-parented and detaching them buys nothing. An entry joins the first group it
        // matches, so overlapping prefixes ("Index,Indexing") cannot report the same setting twice.
        var groups = wanted
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(prefix => (Prefix: prefix, Entries: new List<JsonNode?>()))
            .ToList();

        foreach (var entry in entries)
            foreach (var group in groups)
                if (KeysOf(entry).Any(k => k.StartsWith(group.Prefix, StringComparison.OrdinalIgnoreCase)))
                {
                    group.Entries.Add(entry);
                    break;
                }

        // The "only what is set" rule applies within each requested prefix. Across the whole result
        // it would drop any prefix that is at its defaults, silently and without saying so.
        var matched = new List<JsonNode?>();
        var configured = new List<JsonNode?>();
        foreach (var (_, groupEntries) in groups)
        {
            var set = groupEntries.Where(e => e?["ServerValues"] is JsonObject { Count: > 0 }).ToList();
            configured.AddRange(set);
            matched.AddRange(set.Count > 0 ? set : groupEntries);
        }

        // Description, Type, SizeUnit and AvailableValues are most of an entry and never change.
        // Return the operational fields only.
        static JsonNode? Compact(JsonNode? entry)
        {
            var meta = entry?["Metadata"];
            var values = entry?["ServerValues"];
            var o = new JsonObject
            {
                ["Key"] = (meta?["Keys"] as JsonArray)?.FirstOrDefault()?.DeepClone(),
                ["DefaultValue"] = meta?["DefaultValue"]?.DeepClone(),
            };
            if (values is JsonObject { Count: > 0 } set)
                o["ServerValues"] = set.DeepClone();
            return o;
        }

        const int MaxSettingsEntries = 100;
        var page = matched.Take(MaxSettingsEntries).Select(Compact).ToArray();

        return ToJson(new
        {
            available = true,
            totalEntries,
            matched = matched.Count,
            configured = configured.Count,
            returned = page.Length,
            truncated = matched.Count > MaxSettingsEntries,
            note = configured.Count > 0
                ? "Showing settings with a value set on the server. Metadata (description, type, bounds) omitted."
                : "No setting under this prefix is overridden; showing defaults. Metadata omitted.",
            settings = new JsonArray(page),
        });
    }

    // All registered HTTP routes; large, hence its own facet.
    public Task<JsonElement> GetServerRoutes(CancellationToken cancellationToken) =>
        TryGetServerJson("/debug/routes", cancellationToken);

    public async Task<GetClusterDiagnosticsOverviewResult> GetClusterDiagnosticsOverview(CancellationToken cancellationToken)
    {
        var decisionsTask = TryGetServerJson("/admin/cluster/observer/decisions", cancellationToken);
        var remoteTask = TryGetServerJson("/admin/debug/node/remote-connections", cancellationToken);
        var engineTask = TryGetServerJson("/admin/debug/node/engine-logs", cancellationToken);
        var stateTask = TryGetServerJson("/admin/debug/node/state-change-history", cancellationToken);
        await Task.WhenAll(decisionsTask, remoteTask, engineTask, stateTask);

        return new GetClusterDiagnosticsOverviewResult(
            await decisionsTask,
            await remoteTask,
            await engineTask,
            await stateTask);
    }

    // Raft state-machine log; large, hence its own facet.
    public Task<JsonElement> GetClusterLog(CancellationToken cancellationToken) =>
        TryGetServerJson("/admin/cluster/log", cancellationToken);

    // Cluster history logs; largest cluster diagnostic, hence its own facet.
    public Task<JsonElement> GetClusterHistory(CancellationToken cancellationToken) =>
        TryGetServerJson("/admin/debug/cluster/history-logs", cancellationToken);

    public async Task<DiagnosticTextSampleResult> SampleClusterDashboard(int seconds, CancellationToken cancellationToken)
    {
        // The cluster-dashboard watch feed is a WebSocket and requires the target node tag.
        var node = await ExecuteServerCommand(new GetNodeInfoCommand(), cancellationToken);
        var sample = await GetServerWebSocketSample(
            "/cluster-dashboard/watch",
            seconds,
            cancellationToken,
            ("node", node.NodeTag));

        return new DiagnosticTextSampleResult(
            "cluster_dashboard",
            Math.Clamp(seconds, 1, 30),
            sample.Text,
            sample.Truncated,
            sample.Limit);
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
            await TryGetDatabaseJson(databaseName, "/indexes/history", cancellationToken, ("name", indexName)));
    }

    public async Task<GetQueryDiagnosticsResult> GetQueryDiagnostics(string databaseName, CancellationToken cancellationToken)
    {
        return new GetQueryDiagnosticsResult(
            databaseName,
            await TryGetDatabaseJson(databaseName, "/debug/queries/running", cancellationToken),
            await TryGetDatabaseJson(databaseName, "/debug/queries/cache/list", cancellationToken));
    }

    public async Task<GetOperationsOverviewResult> GetOperationsOverview(
        string? databaseName,
        CancellationToken cancellationToken)
    {
        return new GetOperationsOverviewResult(
            databaseName,
            string.IsNullOrWhiteSpace(databaseName)
                ? ToJson(new { available = false, error = "databaseName was not provided." })
                : await TryGetDatabaseJson(databaseName, "/operations", cancellationToken),
            await TryGetServerJson("/admin/debug/operations/longest-running", cancellationToken));
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
                : await TryGetDatabaseJson(databaseName, "/admin/debug/txinfo", cancellationToken),
            string.IsNullOrWhiteSpace(databaseName)
                ? ToJson(new { available = false, error = "databaseName was not provided." })
                : await TryGetDatabaseJson(databaseName, "/admin/debug/cluster/txinfo", cancellationToken));
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
            var operationState = await ForDatabase(databaseName).SendAsync(
                new GetOperationStateOperation(operationId),
                token: cancellationToken);
            state = ToJson(operationState);

            // A non-existent/expired operation id returns a null state; keep polling until the
            // timeout rather than dereferencing null (get_operation_state returns null gracefully).
            if (operationState is not null
                && operationState.Status is OperationStatus.Completed or OperationStatus.Faulted or OperationStatus.Canceled)
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
            var stats = await ForDatabase(databaseName).SendAsync(
                new GetStatisticsOperation(),
                token: cancellationToken);
            state = ToJson(stats);

            if (stats.Indexes.All(index => !index.IsStale))
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
            await TryGetDatabaseJson(databaseName, "/admin/debug/periodic-backup/timers", cancellationToken),
            // Server-wide backup config carries destination cloud credentials; redact like the record path.
            RedactSecrets(await TryGetServerJson("/admin/configuration/server-wide/backup", cancellationToken)));
    }

    // Rolling performance/conflict history is unbounded; omitted from the default bundle and fetched only on request.
    private static readonly JsonElement OmittedLargeAxis = JsonSerializer.SerializeToElement(
        new { omitted = true, hint = "Large rolling-history axis; set includePerformance=true on get_tasks to fetch it." });

    public async Task<GetEtlDiagnosticsResult> GetEtlDiagnostics(
        string databaseName,
        bool includePerformance,
        CancellationToken cancellationToken)
    {
        var tasks = await GetEtlTasks(databaseName, cancellationToken);

        return new GetEtlDiagnosticsResult(
            databaseName,
            tasks.Tasks,
            await TryGetDatabaseJson(databaseName, "/etl/stats", cancellationToken),
            includePerformance ? await TryGetDatabaseJson(databaseName, "/etl/performance", cancellationToken) : OmittedLargeAxis,
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
            await TryGetDatabaseJson(databaseName, "/subscriptions/state", cancellationToken),
            await TryGetDatabaseJson(databaseName, "/subscriptions/connection-details", cancellationToken));
    }

    public async Task<GetTrafficWatchConfigurationResult> GetTrafficWatchConfiguration(CancellationToken cancellationToken)
    {
        return new GetTrafficWatchConfigurationResult(
            await GetServerJson("/admin/traffic-watch/configuration", cancellationToken));
    }

    // The studio-config route 404s until set; availability-wrapped.
    public Task<JsonElement> GetServerStudioConfiguration(CancellationToken cancellationToken)
    {
        return TryGetServerJson("/configuration/studio", cancellationToken);
    }

    public Task<DiagnosticArtifactResult> ExportLogs(
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken)
    {
        return SaveServerArtifact(
            "logs",
            "/admin/logs/download",
            cancellationToken,
            ("from", from?.ToString("O")),
            ("to", to?.ToString("O")));
    }

    public async Task<DiagnosticTextSampleResult> SampleTrafficWatch(
        int seconds,
        string? databaseName,
        CancellationToken cancellationToken)
    {
        // Traffic-watch live feed is a WebSocket endpoint; listen for the sample window.
        var sample = await GetServerWebSocketSample(
            "/admin/traffic-watch",
            seconds,
            cancellationToken,
            ("database", databaseName));

        return new DiagnosticTextSampleResult(
            "traffic_watch",
            Math.Clamp(seconds, 1, 30),
            SummariseTrafficWatch(sample.Text),
            sample.Truncated,
            sample.Limit);
    }

    // The raw feed repeats the client IP, certificate thumbprint, absolute URI and a QueryTimings
    // tree on every event. Fold by request shape and keep counts plus one example each.
    // Falls back to the raw text if the feed is not the shape we expect.
    internal static string SummariseTrafficWatch(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || !raw.Contains("\"TrafficWatchType\""))
            return raw;

        // A stream of top-level objects, not one document, so scan for balanced braces and parse
        // each alone. A truncated final event is normal, so skip a bad chunk rather than the sample.
        var events = new List<JsonNode>();
        var depth = 0;
        var inString = false;
        var escaped = false;
        var start = -1;
        for (var i = 0; i < raw.Length; i++)
        {
            var ch = raw[i];
            if (inString)
            {
                if (escaped) escaped = false;
                else if (ch == '\\') escaped = true;
                else if (ch == '"') inString = false;
                continue;
            }

            if (ch == '"') { inString = true; continue; }
            if (ch == '{')
            {
                if (depth == 0) start = i;
                depth++;
            }
            else if (ch == '}' && depth > 0)
            {
                depth--;
                if (depth != 0 || start < 0)
                    continue;
                try
                {
                    if (JsonNode.Parse(raw[start..(i + 1)]) is { } node && node["TrafficWatchType"] is not null)
                        events.Add(node);
                }
                catch (JsonException)
                {
                    // skip this chunk, keep the rest
                }
                start = -1;
            }
        }

        if (events.Count == 0)
            return raw;

        static string? Str(JsonNode? n) => n?.GetValue<object>()?.ToString();
        static double Num(JsonNode? n) => double.TryParse(Str(n), out var d) ? d : 0;

        var groups = events
            .GroupBy(e => new
            {
                Type = Str(e["Type"]) ?? Str(e["TrafficWatchType"]),
                Method = Str(e["HttpMethod"]),
                Database = Str(e["DatabaseName"]),
                // The parameters differ per request; the shape does not.
                Shape = (Str(e["CustomInfo"]) ?? Str(e["RequestUri"]) ?? "").Split('\n')[0].Trim(),
            })
            .Select(g => new
            {
                type = g.Key.Type,
                method = g.Key.Method,
                database = g.Key.Database,
                shape = g.Key.Shape,
                count = g.Count(),
                minMs = g.Min(e => Num(e["ElapsedMilliseconds"])),
                avgMs = Math.Round(g.Average(e => Num(e["ElapsedMilliseconds"])), 1),
                maxMs = g.Max(e => Num(e["ElapsedMilliseconds"])),
                responseBytes = g.Sum(e => Num(e["ResponseSizeInBytes"])),
                statusCodes = g.Select(e => Str(e["ResponseStatusCode"]))
                    .GroupBy(c => c).ToDictionary(c => c.Key ?? "?", c => c.Count()),
            })
            .OrderByDescending(g => g.count)
            .ToArray();

        return JsonSerializer.Serialize(new
        {
            summarised = true,
            events = events.Count,
            distinctShapes = groups.Length,
            note = "Folded by request shape. Counts and timings are over the whole sample.",
            requests = groups,
        }, RavenDbJsonOptions);
    }

    public async Task<GetNotificationsResult> GetNotifications(
        string? databaseName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(databaseName))
            return new GetNotificationsResult(null, await GetServerJson("/admin/server/notifications", cancellationToken));

        return new GetNotificationsResult(
            databaseName,
            await GetDatabaseJson(databaseName, "/notifications", cancellationToken));
    }

    public async Task<DiagnosticTextSampleResult> SampleAdminLogs(int seconds, CancellationToken cancellationToken)
    {
        // The admin logs watch feed is a WebSocket endpoint.
        var sample = await GetServerWebSocketSample("/admin/logs/watch", seconds, cancellationToken);
        return new DiagnosticTextSampleResult(
            "admin_logs",
            Math.Clamp(seconds, 1, 30),
            sample.Text,
            sample.Truncated,
            sample.Limit);
    }
}
