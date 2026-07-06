using System.Text.Json;
using System.Text.Json.Nodes;
using Raven.Client.Documents.Operations;
using Raven.Client.Documents.Operations.Configuration;
using Raven.Client.ServerWide.Operations;
using Raven.Client.ServerWide.Operations.Configuration;
using RavenDB.Mcp.Tools;

namespace RavenDB.Mcp.RavenDB;

public sealed partial class RavenDbAdminClient
{
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
        var record = await GetDatabaseRecordJson(databaseName, cancellationToken);
        return new GetDatabaseRecordResult(databaseName, SummarizeRecordIndexes(record));
    }

    private static readonly string[] IndexCollections = ["Indexes", "AutoIndexes", "IndexesHistory"];

    // Full index definitions/history dominate the record (~90%+) and duplicate get_index; reduce them to
    // name+count so the record stays readable. Use get_index for one index's definition, staleness, or history.
    private static JsonElement SummarizeRecordIndexes(JsonElement record)
    {
        var root = JsonNode.Parse(record.GetRawText())!.AsObject();
        var summarized = false;

        foreach (var key in IndexCollections)
        {
            if (root[key] is not JsonObject entries || entries.Count == 0)
                continue;
            var names = new JsonArray();
            foreach (var entry in entries)
                names.Add(entry.Key);
            root[key] = new JsonObject { ["Count"] = entries.Count, ["Names"] = names };
            summarized = true;
        }

        if (summarized)
            root["IndexesHint"] = "Index definitions/history reduced to names; use get_index for one index's full definition, staleness, errors, or history.";

        return JsonSerializer.SerializeToElement(root);
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

    public async Task<GetCollectionOverviewResult> GetCollectionOverview(
        string databaseName,
        CancellationToken cancellationToken)
    {
        var statsTask = GetCollectionStats(databaseName, cancellationToken);
        var detailedStatsTask = GetDetailedCollectionStats(databaseName, cancellationToken);
        await Task.WhenAll(statsTask, detailedStatsTask);

        return new GetCollectionOverviewResult(
            databaseName,
            (await statsTask).Stats,
            (await detailedStatsTask).Stats);
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

    // The studio-config route 404s until set; availability-wrapped.
    public Task<JsonElement> GetDatabaseStudioConfiguration(string databaseName, CancellationToken cancellationToken)
    {
        ValidateDatabaseName(databaseName);
        return TryGetDatabaseJson(databaseName, "/configuration/studio", cancellationToken);
    }

    // No typed op; availability-wrapped (e.g. sharding state only exists on sharded databases).
    public Task<JsonElement> GetTombstonesState(string databaseName, CancellationToken cancellationToken)
    {
        ValidateDatabaseName(databaseName);
        return TryGetDatabaseJson(databaseName, "/admin/tombstones/state", cancellationToken);
    }

    public Task<JsonElement> GetDatabaseMetrics(string databaseName, CancellationToken cancellationToken)
    {
        ValidateDatabaseName(databaseName);
        return TryGetDatabaseJson(databaseName, "/metrics", cancellationToken);
    }

    public Task<JsonElement> GetShardingState(string databaseName, CancellationToken cancellationToken)
    {
        ValidateDatabaseName(databaseName);
        return TryGetDatabaseJson(databaseName, "/debug/sharding/buckets", cancellationToken);
    }
}
