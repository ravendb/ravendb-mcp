using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using Raven.Client.Documents.Conventions;
using Raven.Client.Documents.Operations;
using Raven.Client.Documents.Operations.Backups;
using Raven.Client.Documents.Operations.Configuration;
using Raven.Client.Documents.Operations.Identities;
using Raven.Client.Documents.Operations.Indexes;
using Raven.Client.Documents.Operations.OngoingTasks;
using Raven.Client.Documents.Operations.Replication;
using Raven.Client.Http;
using Raven.Client.ServerWide.Commands;
using Raven.Client.ServerWide.Operations;
using Raven.Client.ServerWide.Operations.Configuration;
using Raven.Client.ServerWide.Operations.Logs;
using RavenDB.Mcp.Configuration;
using RavenDB.Mcp.Tools;
using Sparrow.Json;

namespace RavenDB.Mcp.RavenDB;

public sealed class RavenDbAdminClient(
    IDocumentStore store,
    IOptions<RavenDbOptions>? options = null)
{
    private static readonly JsonSerializerOptions RavenDbJsonOptions = new()
    {
        IncludeFields = true
    };

    private readonly HttpClient http = CreateHttpClient(options?.Value);
    private readonly string serverUrl = (options?.Value.Urls.FirstOrDefault() ?? store.Urls.First()).TrimEnd('/');

    public async Task<ListDatabasesResult> ListDatabases(CancellationToken cancellationToken)
    {
        var databaseNames = await store.Maintenance.Server.SendAsync(
            new GetDatabaseNamesOperation(0, int.MaxValue),
            cancellationToken);

        return new ListDatabasesResult(databaseNames);
    }

    public async Task<GetDatabaseRecordResult> GetDatabaseRecord(
        string databaseName,
        CancellationToken cancellationToken)
    {
        return new GetDatabaseRecordResult(
            databaseName,
            await GetDatabaseRecordJson(databaseName, cancellationToken));
    }

    public async Task<GetServerInfoResult> GetServerInfo(CancellationToken cancellationToken)
    {
        var buildNumber = await store.Maintenance.Server.SendAsync(
            new GetBuildNumberOperation(),
            cancellationToken);
        var nodeInfo = await ExecuteServerCommand(new GetNodeInfoCommand(), cancellationToken);

        return new GetServerInfoResult(
            buildNumber.ProductVersion,
            buildNumber.BuildVersion,
            buildNumber.CommitHash,
            buildNumber.FullVersion,
            ToJson(nodeInfo));
    }

    public async Task<GetClusterTopologyResult> GetClusterTopology(CancellationToken cancellationToken)
    {
        var topology = await ExecuteServerCommand(new GetClusterTopologyCommand(), cancellationToken);
        return new GetClusterTopologyResult(ToJson(topology));
    }

    public async Task<GetNodeStatusResult> GetNodeStatus(CancellationToken cancellationToken)
    {
        var topology = await ExecuteServerCommand(new GetClusterTopologyCommand(), cancellationToken);
        return new GetNodeStatusResult(ToJson(topology));
    }

    public async Task<GetLogsConfigurationToolResult> GetLogsConfiguration(CancellationToken cancellationToken)
    {
        var configuration = await store.Maintenance.Server.SendAsync(
            new GetLogsConfigurationOperation(),
            cancellationToken);

        return new GetLogsConfigurationToolResult(ToJson(configuration));
    }

    public async Task<GetServerWideClientConfigurationResult> GetServerWideClientConfiguration(CancellationToken cancellationToken)
    {
        var configuration = await store.Maintenance.Server.SendAsync(
            new GetServerWideClientConfigurationOperation(),
            cancellationToken);

        return new GetServerWideClientConfigurationResult(ToJson(configuration));
    }

    public async Task<GetDatabaseStatsResult> GetDatabaseStats(string databaseName, CancellationToken cancellationToken)
    {
        var stats = await ForDatabase(databaseName).SendAsync(
            new GetStatisticsOperation(),
            token: cancellationToken);

        return new GetDatabaseStatsResult(databaseName, ToJson(stats));
    }

    public async Task<GetDetailedDatabaseStatsResult> GetDetailedDatabaseStats(string databaseName, CancellationToken cancellationToken)
    {
        var stats = await ForDatabase(databaseName).SendAsync(
            new GetDetailedStatisticsOperation(),
            token: cancellationToken);

        return new GetDetailedDatabaseStatsResult(databaseName, ToJson(stats));
    }

    public async Task<GetCollectionStatsResult> GetCollectionStats(string databaseName, CancellationToken cancellationToken)
    {
        var stats = await ForDatabase(databaseName).SendAsync(
            new GetCollectionStatisticsOperation(),
            token: cancellationToken);

        return new GetCollectionStatsResult(databaseName, ToJson(stats));
    }

    public async Task<GetDetailedCollectionStatsResult> GetDetailedCollectionStats(string databaseName, CancellationToken cancellationToken)
    {
        var stats = await ForDatabase(databaseName).SendAsync(
            new GetDetailedCollectionStatisticsOperation(),
            token: cancellationToken);

        return new GetDetailedCollectionStatsResult(databaseName, ToJson(stats));
    }

    public async Task<GetDatabaseHealthSummaryResult> GetDatabaseHealthSummary(
        string databaseName,
        CancellationToken cancellationToken)
    {
        var stats = await GetDatabaseStats(databaseName, cancellationToken);
        var indexingStatus = await GetIndexingStatus(databaseName, cancellationToken);
        var indexStats = await GetIndexStats(databaseName, cancellationToken);
        var indexErrors = await GetIndexErrors(databaseName, cancellationToken);
        var tasks = await ListOngoingTasks(databaseName, cancellationToken);

        return new GetDatabaseHealthSummaryResult(
            databaseName,
            stats.Stats,
            indexingStatus.Status,
            indexStats.Stats,
            indexErrors.Errors,
            tasks.Tasks);
    }

    public async Task<GetDatabaseConfigurationResult> GetDatabaseConfiguration(string databaseName, CancellationToken cancellationToken)
    {
        var configuration = await ForDatabase(databaseName).SendAsync(
            new GetDatabaseSettingsOperation(databaseName),
            token: cancellationToken);

        return new GetDatabaseConfigurationResult(databaseName, ToJson(configuration));
    }

    public async Task<GetClientConfigurationResult> GetClientConfiguration(string databaseName, CancellationToken cancellationToken)
    {
        var configuration = await ForDatabase(databaseName).SendAsync(
            new GetClientConfigurationOperation(),
            token: cancellationToken);

        return new GetClientConfigurationResult(databaseName, ToJson(configuration));
    }

    public async Task<ListIndexesResult> ListIndexes(string databaseName, CancellationToken cancellationToken)
    {
        var indexes = await ForDatabase(databaseName).SendAsync(
            new GetIndexesOperation(0, int.MaxValue),
            token: cancellationToken);

        return new ListIndexesResult(databaseName, ToJson(indexes));
    }

    public async Task<GetIndexStatsResult> GetIndexStats(string databaseName, CancellationToken cancellationToken)
    {
        var stats = await ForDatabase(databaseName).SendAsync(
            new GetIndexesStatisticsOperation(),
            token: cancellationToken);

        return new GetIndexStatsResult(databaseName, ToJson(stats));
    }

    public async Task<GetIndexErrorsResult> GetIndexErrors(string databaseName, CancellationToken cancellationToken)
    {
        var errors = await ForDatabase(databaseName).SendAsync(
            new GetIndexErrorsOperation(),
            token: cancellationToken);

        return new GetIndexErrorsResult(databaseName, ToJson(errors));
    }

    public async Task<GetIndexPerformanceResult> GetIndexPerformance(string databaseName, CancellationToken cancellationToken)
    {
        var performance = await ForDatabase(databaseName).SendAsync(
            new GetIndexPerformanceStatisticsOperation(),
            token: cancellationToken);

        return new GetIndexPerformanceResult(databaseName, ToJson(performance));
    }

    public async Task<GetIndexingStatusResult> GetIndexingStatus(string databaseName, CancellationToken cancellationToken)
    {
        var status = await ForDatabase(databaseName).SendAsync(
            new GetIndexingStatusOperation(),
            token: cancellationToken);

        return new GetIndexingStatusResult(databaseName, ToJson(status));
    }

    public async Task<GetIndexResult> GetIndex(
        string databaseName,
        string indexName,
        CancellationToken cancellationToken)
    {
        ValidateName(indexName, "Index name", nameof(indexName));

        var index = await ForDatabase(databaseName).SendAsync(
            new GetIndexOperation(indexName),
            token: cancellationToken);

        return new GetIndexResult(databaseName, indexName, ToJson(index));
    }

    public async Task<GetIndexTermsResult> GetIndexTerms(
        string databaseName,
        string indexName,
        string fieldName,
        string? fromValue,
        int? pageSize,
        CancellationToken cancellationToken)
    {
        ValidateName(indexName, "Index name", nameof(indexName));
        ValidateName(fieldName, "Field name", nameof(fieldName));

        var terms = await ForDatabase(databaseName).SendAsync(
            new GetTermsOperation(indexName, fieldName, fromValue, pageSize),
            token: cancellationToken);

        return new GetIndexTermsResult(databaseName, indexName, fieldName, ToJson(terms));
    }

    public async Task<GetOperationStateResult> GetOperationState(
        string databaseName,
        long operationId,
        CancellationToken cancellationToken)
    {
        var state = await ForDatabase(databaseName).SendAsync(
            new GetOperationStateOperation(operationId),
            token: cancellationToken);

        return new GetOperationStateResult(
            databaseName,
            operationId,
            ToJson(state));
    }

    public async Task<ListOngoingTasksResult> ListOngoingTasks(
        string databaseName,
        CancellationToken cancellationToken)
    {
        var record = await GetDatabaseRecordJson(databaseName, cancellationToken);

        return new ListOngoingTasksResult(
            databaseName,
            SelectRecordProperties(record, "backup", "replication", "etl", "subscription", "sink", "expiration", "refresh", "archival"));
    }

    public async Task<GetBackupTasksResult> GetBackupTasks(
        string databaseName,
        CancellationToken cancellationToken)
    {
        var record = await GetDatabaseRecordJson(databaseName, cancellationToken);
        return new GetBackupTasksResult(databaseName, SelectRecordProperties(record, "backup"));
    }

    public async Task<GetReplicationTasksResult> GetReplicationTasks(
        string databaseName,
        CancellationToken cancellationToken)
    {
        var record = await GetDatabaseRecordJson(databaseName, cancellationToken);
        return new GetReplicationTasksResult(databaseName, SelectRecordProperties(record, "replication"));
    }

    public async Task<GetReplicationPerformanceResult> GetReplicationPerformance(
        string databaseName,
        CancellationToken cancellationToken)
    {
        var performance = await ForDatabase(databaseName).SendAsync(
            new GetReplicationPerformanceStatisticsOperation(),
            token: cancellationToken);

        return new GetReplicationPerformanceResult(
            databaseName,
            ToJson(performance));
    }

    public async Task<GetEtlTasksResult> GetEtlTasks(
        string databaseName,
        CancellationToken cancellationToken)
    {
        var record = await GetDatabaseRecordJson(databaseName, cancellationToken);
        return new GetEtlTasksResult(databaseName, SelectRecordProperties(record, "etl"));
    }

    public async Task<GetBackupStatusResult> GetBackupStatus(
        string databaseName,
        long taskId,
        CancellationToken cancellationToken)
    {
        var status = await ForDatabase(databaseName).SendAsync(
            new GetPeriodicBackupStatusOperation(taskId),
            token: cancellationToken);

        return new GetBackupStatusResult(
            databaseName,
            taskId,
            ToJson(status));
    }

    public async Task<GetOngoingTaskInfoResult> GetOngoingTaskInfo(
        string databaseName,
        long taskId,
        OngoingTaskType taskType,
        CancellationToken cancellationToken)
    {
        var task = await ForDatabase(databaseName).SendAsync(
            new GetOngoingTaskInfoOperation(taskId, taskType),
            token: cancellationToken);

        return new GetOngoingTaskInfoResult(
            databaseName,
            taskId,
            taskType.ToString(),
            ToJson(task));
    }

    public async Task<GetEtlTaskInfoResult> GetEtlTaskInfo(
        string databaseName,
        long taskId,
        OngoingTaskType taskType,
        CancellationToken cancellationToken)
    {
        var task = await GetOngoingTaskInfo(databaseName, taskId, taskType, cancellationToken);

        return new GetEtlTaskInfoResult(
            task.DatabaseName,
            task.TaskId,
            task.TaskType,
            task.Task);
    }

    public async Task<GetSubscriptionsResult> GetSubscriptions(
        string databaseName,
        CancellationToken cancellationToken)
    {
        ValidateDatabaseName(databaseName);

        var subscriptions = await store.Subscriptions.GetSubscriptionsAsync(
            0,
            int.MaxValue,
            databaseName,
            cancellationToken);

        return new GetSubscriptionsResult(databaseName, ToJson(subscriptions));
    }

    public async Task<GetSubscriptionStateResult> GetSubscriptionState(
        string databaseName,
        string subscriptionName,
        CancellationToken cancellationToken)
    {
        ValidateDatabaseName(databaseName);

        if (string.IsNullOrWhiteSpace(subscriptionName))
            throw new ArgumentException("Subscription name is required.", nameof(subscriptionName));

        var state = await store.Subscriptions.GetSubscriptionStateAsync(
            subscriptionName,
            databaseName,
            cancellationToken);

        return new GetSubscriptionStateResult(databaseName, subscriptionName, ToJson(state));
    }

    public async Task<GetDatabaseTcpInfoResult> GetDatabaseTcpInfo(
        string databaseName,
        string nodeTag,
        CancellationToken cancellationToken)
    {
        ValidateDatabaseName(databaseName);

        if (string.IsNullOrWhiteSpace(nodeTag))
            throw new ArgumentException("Node tag is required.", nameof(nodeTag));

        var tcp = await ExecuteServerCommand(new GetTcpInfoCommand(nodeTag, databaseName), cancellationToken);
        return new GetDatabaseTcpInfoResult(databaseName, nodeTag, ToJson(tcp));
    }

    public async Task<GetIdentitiesResult> GetIdentities(
        string databaseName,
        CancellationToken cancellationToken)
    {
        var identities = await ForDatabase(databaseName).SendAsync(
            new GetIdentitiesOperation(),
            token: cancellationToken);

        return new GetIdentitiesResult(databaseName, ToJson(identities));
    }

    public async Task<GetStorageOverviewResult> GetStorageOverview(
        string databaseName,
        CancellationToken cancellationToken)
    {
        return new GetStorageOverviewResult(
            databaseName,
            await GetDatabaseJson(databaseName, "/debug/storage/report", cancellationToken),
            await GetDatabaseJson(databaseName, "/debug/storage/all-environments/report", cancellationToken));
    }

    public async Task<GetStorageTreesResult> GetStorageTrees(
        string databaseName,
        CancellationToken cancellationToken)
    {
        return new GetStorageTreesResult(
            databaseName,
            await GetDatabaseJson(databaseName, "/debug/storage/trees", cancellationToken));
    }

    public async Task<GetStorageEnvironmentReportResult> GetStorageEnvironmentReport(
        string databaseName,
        string? environmentName,
        string? environmentType,
        CancellationToken cancellationToken)
    {
        var name = string.IsNullOrWhiteSpace(environmentName) ? databaseName : environmentName;
        var type = string.IsNullOrWhiteSpace(environmentType) ? "Documents" : environmentType;

        return new GetStorageEnvironmentReportResult(
            databaseName,
            name,
            type,
            await GetDatabaseJson(
                databaseName,
                "/debug/storage/environment/report",
                cancellationToken,
                ("name", name),
                ("type", type)));
    }

    public async Task<GetStorageTreeStructureResult> GetStorageTreeStructure(
        string databaseName,
        string treeName,
        string? treeKind,
        CancellationToken cancellationToken)
    {
        ValidateName(treeName, "Tree name", nameof(treeName));

        var kind = string.IsNullOrWhiteSpace(treeKind) ? "btree" : treeKind;
        var path = kind.Equals("fixed_size", StringComparison.OrdinalIgnoreCase) ||
                   kind.Equals("fst", StringComparison.OrdinalIgnoreCase)
            ? "/debug/storage/fst-structure"
            : "/debug/storage/btree-structure";

        return new GetStorageTreeStructureResult(
            databaseName,
            treeName,
            kind,
            await GetDatabaseText(databaseName, path, cancellationToken, ("name", treeName)));
    }

    public async Task<GetStorageCompressionDictionariesResult> GetStorageCompressionDictionaries(
        string databaseName,
        CancellationToken cancellationToken)
    {
        return new GetStorageCompressionDictionariesResult(
            databaseName,
            await GetDatabaseJson(databaseName, "/debug/storage/compression-dictionaries", cancellationToken));
    }

    public async Task<GetStorageScratchBufferInfoResult> GetStorageScratchBufferInfo(
        string databaseName,
        string? environmentName,
        string? environmentType,
        CancellationToken cancellationToken)
    {
        var name = string.IsNullOrWhiteSpace(environmentName) ? databaseName : environmentName;
        var type = string.IsNullOrWhiteSpace(environmentType) ? "Documents" : environmentType;

        return new GetStorageScratchBufferInfoResult(
            databaseName,
            name,
            type,
            await GetDatabaseJson(
                databaseName,
                "/debug/storage/environment/scratch-buffer-info",
                cancellationToken,
                ("name", name),
                ("type", type)));
    }

    public async Task<GetStorageFreeSpaceSnapshotResult> GetStorageFreeSpaceSnapshot(
        string databaseName,
        string? environmentName,
        string? environmentType,
        CancellationToken cancellationToken)
    {
        var name = string.IsNullOrWhiteSpace(environmentName) ? databaseName : environmentName;
        var type = string.IsNullOrWhiteSpace(environmentType) ? "Documents" : environmentType;

        return new GetStorageFreeSpaceSnapshotResult(
            databaseName,
            name,
            type,
            await GetDatabaseJson(
                databaseName,
                "/debug/storage/environment/free-space-snapshot",
                cancellationToken,
                ("name", name),
                ("type", type)));
    }

    public async Task<GetPerformanceOverviewResult> GetPerformanceOverview(CancellationToken cancellationToken)
    {
        return new GetPerformanceOverviewResult(await GetServerJson("/admin/metrics", cancellationToken));
    }

    public async Task<GetCpuStatsResult> GetCpuStats(CancellationToken cancellationToken)
    {
        return new GetCpuStatsResult(await GetServerJson("/admin/debug/cpu/stats", cancellationToken));
    }

    public async Task<GetIoStatsResult> GetIoStats(string? databaseName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(databaseName))
            return new GetIoStatsResult(null, await GetServerJson("/admin/debug/io-metrics", cancellationToken));

        return new GetIoStatsResult(
            databaseName,
            await GetDatabaseJson(databaseName, "/debug/io-metrics", cancellationToken));
    }

    public async Task<GetGcMemoryStatsResult> GetGcMemoryStats(CancellationToken cancellationToken)
    {
        return new GetGcMemoryStatsResult(await GetServerJson("/admin/debug/memory/gc", cancellationToken));
    }

    public async Task<GetOsMemoryStatsResult> GetOsMemoryStats(CancellationToken cancellationToken)
    {
        return new GetOsMemoryStatsResult(await GetServerJson("/admin/debug/memory/stats", cancellationToken));
    }

    public async Task<GetProcessStatsResult> GetProcessStats(CancellationToken cancellationToken)
    {
        return new GetProcessStatsResult(await GetServerJson("/admin/debug/proc/stats", cancellationToken));
    }

    public async Task<GetLowMemoryLogResult> GetLowMemoryLog(CancellationToken cancellationToken)
    {
        return new GetLowMemoryLogResult(await GetServerJson("/admin/debug/memory/low-mem-log", cancellationToken));
    }

    public async Task<GetEncryptionBufferPoolStatsResult> GetEncryptionBufferPoolStats(CancellationToken cancellationToken)
    {
        return new GetEncryptionBufferPoolStatsResult(await GetServerJson("/admin/debug/memory/encryption-buffer-pool", cancellationToken));
    }

    public async Task<SampleRuntimeEventsResult> SampleRuntimeEvents(
        string kind,
        int seconds,
        CancellationToken cancellationToken)
    {
        var path = kind.Equals("gc", StringComparison.OrdinalIgnoreCase)
            ? "/admin/debug/memory/gc-events"
            : "/admin/debug/memory/allocations";

        return new SampleRuntimeEventsResult(
            kind,
            Math.Clamp(seconds, 1, 30),
            await GetServerTextSample(path, seconds, cancellationToken));
    }

    public async Task<GetThreadStatsResult> GetThreadStats(CancellationToken cancellationToken)
    {
        var stats = await GetServerJson("/admin/debug/memory/stats", cancellationToken);
        return new GetThreadStatsResult(stats.GetProperty("Threads").Clone());
    }

    public async Task<SampleThreadDiagnosticsResult> SampleThreadDiagnostics(
        string kind,
        int seconds,
        CancellationToken cancellationToken)
    {
        var path = kind.Equals("contention", StringComparison.OrdinalIgnoreCase)
            ? "/admin/debug/threads/contention"
            : "/admin/debug/threads/runaway";

        if (path.EndsWith("/runaway", StringComparison.Ordinal))
            return new SampleThreadDiagnosticsResult(kind, 0, await GetServerText(path, cancellationToken));

        return new SampleThreadDiagnosticsResult(
            kind,
            Math.Clamp(seconds, 1, 30),
            await GetServerTextSample(path, seconds, cancellationToken));
    }

    public async Task<GetStackTracesResult> GetStackTraces(CancellationToken cancellationToken)
    {
        return new GetStackTracesResult(await GetServerJson("/admin/debug/threads/stack-trace", cancellationToken));
    }

    public async Task<GetScriptRunnersResult> GetScriptRunners(
        string? databaseName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(databaseName))
            return new GetScriptRunnersResult(null, await GetServerJson("/admin/debug/script-runners", cancellationToken));

        return new GetScriptRunnersResult(
            databaseName,
            await GetDatabaseJson(databaseName, "/debug/script-runners", cancellationToken));
    }

    public async Task<GetTcpStatsResult> GetTcpStats(CancellationToken cancellationToken)
    {
        return new GetTcpStatsResult(await GetServerJson("/admin/debug/info/tcp/stats", cancellationToken));
    }

    public async Task<ListTcpConnectionsResult> ListTcpConnections(
        string? databaseName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(databaseName))
            return new ListTcpConnectionsResult(null, await GetServerJson("/admin/debug/info/tcp/active-connections", cancellationToken));

        return new ListTcpConnectionsResult(
            databaseName,
            await GetDatabaseJson(databaseName, "/info/tcp", cancellationToken));
    }

    private async Task<JsonElement> GetDatabaseRecordJson(string databaseName, CancellationToken cancellationToken)
    {
        ValidateDatabaseName(databaseName);

        var record = await store.Maintenance.Server.SendAsync(
            new GetDatabaseRecordOperation(databaseName),
            cancellationToken);

        if (record is null)
            throw new InvalidOperationException($"Database '{databaseName}' was not found.");

        // DatabaseRecord keeps most payload data in fields.
        return ToJson(record);
    }

    private MaintenanceOperationExecutor ForDatabase(string databaseName)
    {
        ValidateDatabaseName(databaseName);
        return store.Maintenance.ForDatabase(databaseName);
    }

    private Task<T> ExecuteServerCommand<T>(RavenCommand<T> command, CancellationToken cancellationToken)
    {
        return store.Maintenance.Server.SendAsync(
            new ServerCommandOperation<T>(command),
            cancellationToken);
    }

    private Task<JsonElement> GetServerJson(
        string path,
        CancellationToken cancellationToken,
        params (string Name, string? Value)[] query)
    {
        return GetJson(BuildServerUrl(path, query), cancellationToken);
    }

    private Task<JsonElement> GetDatabaseJson(
        string databaseName,
        string path,
        CancellationToken cancellationToken,
        params (string Name, string? Value)[] query)
    {
        ValidateDatabaseName(databaseName);
        return GetJson(BuildDatabaseUrl(databaseName, path, query), cancellationToken);
    }

    private Task<string> GetServerText(string path, CancellationToken cancellationToken)
    {
        return GetText(BuildServerUrl(path), cancellationToken);
    }

    private Task<string> GetDatabaseText(
        string databaseName,
        string path,
        CancellationToken cancellationToken,
        params (string Name, string? Value)[] query)
    {
        ValidateDatabaseName(databaseName);
        return GetText(BuildDatabaseUrl(databaseName, path, query), cancellationToken);
    }

    private async Task<JsonElement> GetJson(string url, CancellationToken cancellationToken)
    {
        var content = await GetText(url, cancellationToken);
        return JsonSerializer.Deserialize<JsonElement>(content);
    }

    private async Task<string> GetText(string url, CancellationToken cancellationToken)
    {
        using var response = await http.GetAsync(url, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"GET {url} failed with {(int)response.StatusCode}: {content}");

        return content;
    }

    private async Task<string> GetServerTextSample(
        string path,
        int seconds,
        CancellationToken cancellationToken)
    {
        var sampleSeconds = Math.Clamp(seconds, 1, 30);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(sampleSeconds));

        var result = new StringBuilder();

        try
        {
            using var response = await http.GetAsync(
                BuildServerUrl(path),
                HttpCompletionOption.ResponseHeadersRead,
                timeout.Token);

            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
            using var reader = new StreamReader(stream);
            var buffer = new char[4096];

            while (!timeout.Token.IsCancellationRequested && result.Length < 131_072)
            {
                var read = await reader.ReadAsync(buffer, timeout.Token);
                if (read == 0)
                    break;

                result.Append(buffer, 0, read);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
        }

        return result.ToString();
    }

    private static JsonElement ToJson<T>(T value)
    {
        return JsonSerializer.SerializeToElement(value, RavenDbJsonOptions);
    }

    private static HttpClient CreateHttpClient(RavenDbOptions? options)
    {
        if (options is null || string.IsNullOrWhiteSpace(options.CertificatePath))
            return new HttpClient { Timeout = TimeSpan.FromSeconds(60) };

        var handler = new HttpClientHandler();
        handler.ClientCertificates.Add(DocumentStoreFactory.LoadCertificate(options)!);
        return new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(60) };
    }

    private string BuildServerUrl(
        string path,
        params (string Name, string? Value)[] query)
    {
        return WithQuery($"{serverUrl}{path}", query);
    }

    private string BuildDatabaseUrl(
        string databaseName,
        string path,
        params (string Name, string? Value)[] query)
    {
        return WithQuery($"{serverUrl}/databases/{Uri.EscapeDataString(databaseName)}{path}", query);
    }

    private static string WithQuery(string url, params (string Name, string? Value)[] query)
    {
        var values = query
            .Where(item => !string.IsNullOrWhiteSpace(item.Value))
            .Select(item => $"{Uri.EscapeDataString(item.Name)}={Uri.EscapeDataString(item.Value!)}")
            .ToArray();

        return values.Length == 0 ? url : $"{url}?{string.Join('&', values)}";
    }

    private static JsonElement SelectRecordProperties(JsonElement record, params string[] nameFragments)
    {
        var values = new Dictionary<string, JsonElement>();

        foreach (var property in record.EnumerateObject())
        {
            if (nameFragments.Any(fragment => property.Name.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
                values[property.Name] = property.Value.Clone();
        }

        return ToJson(values);
    }

    private static void ValidateDatabaseName(string databaseName)
    {
        ValidateName(databaseName, "Database name", nameof(databaseName));
    }

    private static void ValidateName(string value, string label, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{label} is required.", parameterName);
    }

    private sealed class ServerCommandOperation<T>(RavenCommand<T> command) : IServerOperation<T>
    {
        public RavenCommand<T> GetCommand(DocumentConventions conventions, JsonOperationContext context)
        {
            return command;
        }
    }
}
