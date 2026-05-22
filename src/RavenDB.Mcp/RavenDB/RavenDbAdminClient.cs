using System.Net.Http;
using System.Text.Json;
using Raven.Client.Documents;
using Raven.Client.Documents.Conventions;
using Raven.Client.Documents.Operations;
using Raven.Client.Documents.Operations.Indexes;
using Raven.Client.Http;
using Raven.Client.ServerWide.Commands;
using Raven.Client.ServerWide.Operations;
using Raven.Client.ServerWide.Operations.Configuration;
using Raven.Client.ServerWide.Operations.Logs;
using RavenDB.Mcp.Tools;
using Sparrow.Json;

namespace RavenDB.Mcp.RavenDB;

public sealed class RavenDbAdminClient(IDocumentStore store)
{
    private static readonly JsonSerializerOptions RavenDbJsonOptions = new()
    {
        IncludeFields = true
    };

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
        if (string.IsNullOrWhiteSpace(databaseName))
            throw new ArgumentException("Database name is required.", nameof(databaseName));

        var record = await store.Maintenance.Server.SendAsync(
            new GetDatabaseRecordOperation(databaseName),
            cancellationToken);

        if (record is null)
            throw new InvalidOperationException($"Database '{databaseName}' was not found.");

        return new GetDatabaseRecordResult(
            databaseName,
            // DatabaseRecord keeps most payload data in fields.
            ToJson(record));
    }

    public async Task<GetServerInfoResult> GetServerInfo(CancellationToken cancellationToken)
    {
        var buildNumber = await store.Maintenance.Server.SendAsync(
            new GetBuildNumberOperation(),
            cancellationToken);

        return new GetServerInfoResult(
            buildNumber.ProductVersion,
            buildNumber.BuildVersion,
            buildNumber.CommitHash,
            buildNumber.FullVersion);
    }

    public async Task<GetClusterTopologyResult> GetClusterTopology(CancellationToken cancellationToken)
    {
        var topology = await ExecuteServerCommand(new GetClusterTopologyCommand(), cancellationToken);
        return new GetClusterTopologyResult(ToJson(topology));
    }

    public async Task<GetNodeInfoResult> GetNodeInfo(CancellationToken cancellationToken)
    {
        var nodeInfo = await ExecuteServerCommand(new GetNodeInfoCommand(), cancellationToken);
        return new GetNodeInfoResult(ToJson(nodeInfo));
    }

    public async Task<GetNodeStatusResult> GetNodeStatus(CancellationToken cancellationToken)
    {
        return new GetNodeStatusResult(await GetServerJson("/admin/stats", cancellationToken));
    }

    public async Task<GetServerMetricsResult> GetServerMetrics(CancellationToken cancellationToken)
    {
        return new GetServerMetricsResult(await GetServerJson("/admin/metrics", cancellationToken));
    }

    public async Task<GetServerConfigurationResult> GetServerConfiguration(CancellationToken cancellationToken)
    {
        return new GetServerConfigurationResult(await GetServerJson("/admin/configuration/settings", cancellationToken));
    }

    public async Task<GetStudioConfigurationResult> GetStudioConfiguration(CancellationToken cancellationToken)
    {
        return new GetStudioConfigurationResult(await GetServerJson("/configuration/studio", cancellationToken));
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
            new Raven.Client.Documents.Operations.Configuration.GetClientConfigurationOperation(),
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

    public async Task<GetIndexProgressResult> GetIndexProgress(string databaseName, CancellationToken cancellationToken)
    {
        return new GetIndexProgressResult(
            databaseName,
            await GetDatabaseJson(databaseName, "/indexes/progress", cancellationToken));
    }

    public async Task<GetIndexStalenessResult> GetIndexStaleness(
        string databaseName,
        string indexName,
        CancellationToken cancellationToken)
    {
        return new GetIndexStalenessResult(
            databaseName,
            indexName,
            await GetDatabaseJson(databaseName, $"/indexes/staleness?name={Uri.EscapeDataString(indexName)}", cancellationToken));
    }

    public async Task<GetSuggestedIndexMergesResult> GetSuggestedIndexMerges(
        string databaseName,
        CancellationToken cancellationToken)
    {
        return new GetSuggestedIndexMergesResult(
            databaseName,
            await GetDatabaseJson(databaseName, "/indexes/suggest-index-merge", cancellationToken));
    }

    public async Task<ListRunningOperationsResult> ListRunningOperations(
        string databaseName,
        CancellationToken cancellationToken)
    {
        return new ListRunningOperationsResult(
            databaseName,
            await GetDatabaseJson(databaseName, "/operations", cancellationToken));
    }

    public async Task<GetOperationStateResult> GetOperationState(
        string databaseName,
        long operationId,
        CancellationToken cancellationToken)
    {
        return new GetOperationStateResult(
            databaseName,
            operationId,
            await GetDatabaseJson(databaseName, $"/operations/state?id={operationId}", cancellationToken));
    }

    public async Task<ListRunningQueriesResult> ListRunningQueries(
        string databaseName,
        CancellationToken cancellationToken)
    {
        return new ListRunningQueriesResult(
            databaseName,
            await GetDatabaseJson(databaseName, "/debug/queries/running", cancellationToken));
    }

    public async Task<GetQueryCacheInfoResult> GetQueryCacheInfo(
        string databaseName,
        CancellationToken cancellationToken)
    {
        return new GetQueryCacheInfoResult(
            databaseName,
            await GetDatabaseJson(databaseName, "/debug/queries/cache/list", cancellationToken));
    }

    public async Task<GetReplicationActiveConnectionsResult> GetReplicationActiveConnections(
        string databaseName,
        CancellationToken cancellationToken)
    {
        return new GetReplicationActiveConnectionsResult(
            databaseName,
            await GetDatabaseJson(databaseName, "/replication/active-connections", cancellationToken));
    }

    public async Task<GetReplicationConflictsResult> GetReplicationConflicts(
        string databaseName,
        CancellationToken cancellationToken)
    {
        return new GetReplicationConflictsResult(
            databaseName,
            await GetDatabaseJson(databaseName, "/replication/conflicts", cancellationToken));
    }

    public async Task<GetReplicationPerformanceResult> GetReplicationPerformance(
        string databaseName,
        CancellationToken cancellationToken)
    {
        return new GetReplicationPerformanceResult(
            databaseName,
            await GetDatabaseJson(databaseName, "/replication/performance", cancellationToken));
    }

    public async Task<GetOutgoingReplicationFailuresResult> GetOutgoingReplicationFailures(
        string databaseName,
        CancellationToken cancellationToken)
    {
        return new GetOutgoingReplicationFailuresResult(
            databaseName,
            await GetDatabaseJson(databaseName, "/replication/debug/outgoing-failures", cancellationToken));
    }

    public async Task<GetIncomingReplicationRejectionInfoResult> GetIncomingReplicationRejectionInfo(
        string databaseName,
        CancellationToken cancellationToken)
    {
        return new GetIncomingReplicationRejectionInfoResult(
            databaseName,
            await GetDatabaseJson(databaseName, "/replication/debug/incoming-rejection-info", cancellationToken));
    }

    public async Task<GetBackupStatusResult> GetBackupStatus(
        string databaseName,
        long taskId,
        CancellationToken cancellationToken)
    {
        return new GetBackupStatusResult(
            databaseName,
            taskId,
            await GetServerJson($"/periodic-backup/status?name={Uri.EscapeDataString(databaseName)}&taskId={taskId}", cancellationToken));
    }

    public async Task<GetNextBackupOccurrencesResult> GetNextBackupOccurrences(
        string databaseName,
        CancellationToken cancellationToken)
    {
        return new GetNextBackupOccurrencesResult(
            databaseName,
            await GetDatabaseJson(databaseName, "/admin/debug/periodic-backup/timers", cancellationToken));
    }

    public async Task<ListOngoingTasksResult> ListOngoingTasks(
        string databaseName,
        CancellationToken cancellationToken)
    {
        return new ListOngoingTasksResult(
            databaseName,
            await GetDatabaseJson(databaseName, "/tasks", cancellationToken));
    }

    public async Task<GetEtlStatsResult> GetEtlStats(
        string databaseName,
        CancellationToken cancellationToken)
    {
        return new GetEtlStatsResult(
            databaseName,
            await GetDatabaseJson(databaseName, "/etl/stats", cancellationToken));
    }

    public async Task<GetEtlPerformanceResult> GetEtlPerformance(
        string databaseName,
        CancellationToken cancellationToken)
    {
        return new GetEtlPerformanceResult(
            databaseName,
            await GetDatabaseJson(databaseName, "/etl/performance", cancellationToken));
    }

    public async Task<GetEtlDebugStatsResult> GetEtlDebugStats(
        string databaseName,
        CancellationToken cancellationToken)
    {
        return new GetEtlDebugStatsResult(
            databaseName,
            await GetDatabaseJson(databaseName, "/etl/debug/stats", cancellationToken));
    }

    public async Task<GetSubscriptionsResult> GetSubscriptions(
        string databaseName,
        CancellationToken cancellationToken)
    {
        return new GetSubscriptionsResult(
            databaseName,
            await GetDatabaseJson(databaseName, "/subscriptions", cancellationToken));
    }

    public async Task<GetSubscriptionConnectionDetailsResult> GetSubscriptionConnectionDetails(
        string databaseName,
        string subscriptionName,
        CancellationToken cancellationToken)
    {
        return new GetSubscriptionConnectionDetailsResult(
            databaseName,
            subscriptionName,
            await GetDatabaseJson(databaseName, $"/subscriptions/connection-details?name={Uri.EscapeDataString(subscriptionName)}", cancellationToken));
    }

    public async Task<GetNotificationCenterAlertsResult> GetNotificationCenterAlerts(
        string databaseName,
        CancellationToken cancellationToken)
    {
        return new GetNotificationCenterAlertsResult(
            databaseName,
            await GetDatabaseJson(databaseName, "/notifications", cancellationToken));
    }

    public async Task<GetDatabaseTcpInfoResult> GetDatabaseTcpInfo(
        string databaseName,
        CancellationToken cancellationToken)
    {
        return new GetDatabaseTcpInfoResult(
            databaseName,
            await GetDatabaseJson(databaseName, "/info/tcp", cancellationToken));
    }

    private MaintenanceOperationExecutor ForDatabase(string databaseName)
    {
        if (string.IsNullOrWhiteSpace(databaseName))
            throw new ArgumentException("Database name is required.", nameof(databaseName));

        return store.Maintenance.ForDatabase(databaseName);
    }

    private Task<T> ExecuteServerCommand<T>(RavenCommand<T> command, CancellationToken cancellationToken)
    {
        return store.Maintenance.Server.SendAsync(
            new ServerCommandOperation<T>(command),
            cancellationToken);
    }

    private Task<JsonElement> GetServerJson(string path, CancellationToken cancellationToken)
    {
        return store.Maintenance.Server.SendAsync(
            new RawJsonOperation(path, isDatabaseRequest: false),
            cancellationToken);
    }

    private Task<JsonElement> GetDatabaseJson(
        string databaseName,
        string path,
        CancellationToken cancellationToken)
    {
        return ForDatabase(databaseName).SendAsync(
            new RawJsonOperation(path, isDatabaseRequest: true),
            token: cancellationToken);
    }

    private static JsonElement ToJson<T>(T value)
    {
        return JsonSerializer.SerializeToElement(value, RavenDbJsonOptions);
    }

    private sealed class ServerCommandOperation<T>(RavenCommand<T> command) : IServerOperation<T>
    {
        public RavenCommand<T> GetCommand(DocumentConventions conventions, JsonOperationContext context)
        {
            return command;
        }
    }

    private sealed class RawJsonOperation(string path, bool isDatabaseRequest) :
        IServerOperation<JsonElement>,
        IMaintenanceOperation<JsonElement>
    {
        public RavenCommand<JsonElement> GetCommand(DocumentConventions conventions, JsonOperationContext context)
        {
            return new RawJsonCommand(path, isDatabaseRequest);
        }
    }

    private sealed class RawJsonCommand(string path, bool isDatabaseRequest) : RavenCommand<JsonElement>
    {
        public override bool IsReadRequest => true;

        public override HttpRequestMessage CreateRequest(JsonOperationContext context, ServerNode node, out string url)
        {
            url = isDatabaseRequest
                ? $"{node.Url}/databases/{node.Database}{path}"
                : node.Url + path;

            return new HttpRequestMessage
            {
                Method = HttpMethod.Get
            };
        }

        public override void SetResponse(JsonOperationContext context, BlittableJsonReaderObject response, bool fromCache)
        {
            using var document = JsonDocument.Parse(response.ToString());
            Result = document.RootElement.Clone();
        }
    }
}
