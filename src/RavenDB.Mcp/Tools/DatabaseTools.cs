using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using RavenDB.Mcp.RavenDB;

namespace RavenDB.Mcp.Tools;

[McpServerToolType]
public static class DatabaseTools
{
    [McpServerTool(Name = "list_databases", ReadOnly = true, UseStructuredContent = true)]
    [Description("List all database names in the cluster. Call first to discover targets.")]
    public static Task<ListDatabasesResult> ListDatabases(
        RavenDbAdminClient client,
        CancellationToken cancellationToken)
    {
        return client.ListDatabases(cancellationToken);
    }

    [McpServerTool(Name = "get_database_record", ReadOnly = true)]
    [Description("Full database record: topology, ongoing tasks (backup/replication/ETL/subscriptions), settings, and feature configuration.")]
    public static Task<GetDatabaseRecordResult> GetDatabaseRecord(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetDatabaseRecord(databaseName, cancellationToken);
    }

    [McpServerTool(Name = "get_database_stats", ReadOnly = true)]
    [Description("Per-database statistics and state. Sections: Summary, Detailed, Collections, Indexing, IndexErrors, IndexPerformance, Storage, Tombstones, Metrics, Identities, Revisions, Sharding, HugeDocuments, Io. Choose with include; default is Summary + Collections + Indexing. Tombstones/Metrics/Sharding are availability-wrapped (e.g. Sharding only on sharded databases).")]
    public static async Task<Dictionary<string, object?>> GetDatabaseStats(
        RavenDbAdminClient client,
        [Description("Database to read.")] string databaseName,
        [Description("Sections to return; omit for Summary + Collections + Indexing.")] DatabaseStatsInclude[]? include = null,
        CancellationToken cancellationToken = default)
    {
        var sections = Facet.Resolve(include,
            DatabaseStatsInclude.Summary, DatabaseStatsInclude.Collections, DatabaseStatsInclude.Indexing);
        var result = new Dictionary<string, object?>();

        if (sections.Contains(DatabaseStatsInclude.Summary)) result["summary"] = await client.GetDatabaseStats(databaseName, cancellationToken);
        if (sections.Contains(DatabaseStatsInclude.Detailed)) result["detailed"] = await client.GetDetailedDatabaseStats(databaseName, cancellationToken);
        if (sections.Contains(DatabaseStatsInclude.Collections)) result["collections"] = await client.GetCollectionOverview(databaseName, cancellationToken);
        if (sections.Contains(DatabaseStatsInclude.Indexing)) result["indexing"] = await client.GetIndexingOverview(databaseName, cancellationToken);
        if (sections.Contains(DatabaseStatsInclude.IndexErrors)) result["indexErrors"] = await client.GetIndexErrors(databaseName, cancellationToken);
        if (sections.Contains(DatabaseStatsInclude.IndexPerformance)) result["indexPerformance"] = await client.GetIndexPerformance(databaseName, cancellationToken);
        if (sections.Contains(DatabaseStatsInclude.Storage)) result["storage"] = await client.GetStorageOverview(databaseName, cancellationToken);
        if (sections.Contains(DatabaseStatsInclude.Tombstones)) result["tombstones"] = await client.GetTombstonesState(databaseName, cancellationToken);
        if (sections.Contains(DatabaseStatsInclude.Metrics)) result["metrics"] = await client.GetDatabaseMetrics(databaseName, cancellationToken);
        if (sections.Contains(DatabaseStatsInclude.Identities)) result["identities"] = await client.GetIdentities(databaseName, cancellationToken);
        if (sections.Contains(DatabaseStatsInclude.Revisions)) result["revisions"] = await client.GetRevisionsCollectionStats(databaseName, cancellationToken);
        if (sections.Contains(DatabaseStatsInclude.Sharding)) result["sharding"] = await client.GetShardingState(databaseName, cancellationToken);
        if (sections.Contains(DatabaseStatsInclude.HugeDocuments)) result["hugeDocuments"] = await client.GetHugeDocumentsReport(databaseName, cancellationToken);
        if (sections.Contains(DatabaseStatsInclude.Io)) result["io"] = await client.GetIoStats(databaseName, cancellationToken);

        return result;
    }

    // Feature toggles/policies live in the database record; one fetch covers all of them.
    private static readonly (DatabaseConfigSection Section, string RecordKey, string Label)[] FeatureSections =
    [
        (DatabaseConfigSection.Expiration, "Expiration", "expiration"),
        (DatabaseConfigSection.Refresh, "Refresh", "refresh"),
        (DatabaseConfigSection.DataArchival, "DataArchival", "dataArchival"),
        (DatabaseConfigSection.Revisions, "Revisions", "revisions"),
        (DatabaseConfigSection.DocumentsCompression, "DocumentsCompression", "documentsCompression"),
        (DatabaseConfigSection.TimeSeries, "TimeSeries", "timeSeries"),
        (DatabaseConfigSection.SchemaValidation, "SchemaValidation", "schemaValidation")
    ];

    [McpServerTool(Name = "get_database_config", ReadOnly = true)]
    [Description("Configuration of one database. Sections: Settings (effective config keys), ClientConfig (client config pushed to clients), Studio, and feature toggles/policies Expiration, Refresh, DataArchival, Revisions, DocumentsCompression, TimeSeries, SchemaValidation. Choose with include; default is all. Feature sections are projected from the database record; null means not configured.")]
    public static async Task<Dictionary<string, object?>> GetDatabaseConfig(
        RavenDbAdminClient client,
        [Description("Database to read configuration for.")] string databaseName,
        [Description("Sections to return; omit for all.")] DatabaseConfigSection[]? include = null,
        CancellationToken cancellationToken = default)
    {
        var sections = Facet.Resolve(include,
            DatabaseConfigSection.Settings, DatabaseConfigSection.ClientConfig, DatabaseConfigSection.Studio,
            DatabaseConfigSection.Expiration, DatabaseConfigSection.Refresh, DatabaseConfigSection.DataArchival,
            DatabaseConfigSection.Revisions, DatabaseConfigSection.DocumentsCompression,
            DatabaseConfigSection.TimeSeries, DatabaseConfigSection.SchemaValidation);
        var result = new Dictionary<string, object?>();

        if (sections.Contains(DatabaseConfigSection.Settings))
            result["settings"] = await client.GetDatabaseConfiguration(databaseName, cancellationToken);
        if (sections.Contains(DatabaseConfigSection.ClientConfig))
            result["clientConfig"] = await client.GetClientConfiguration(databaseName, cancellationToken);
        if (sections.Contains(DatabaseConfigSection.Studio))
            result["studio"] = await client.GetDatabaseStudioConfiguration(databaseName, cancellationToken);

        var requestedFeatures = FeatureSections.Where(feature => sections.Contains(feature.Section)).ToArray();
        if (requestedFeatures.Length > 0)
        {
            var record = (await client.GetDatabaseRecord(databaseName, cancellationToken)).Record;
            foreach (var (_, recordKey, label) in requestedFeatures)
                result[label] = record.TryGetProperty(recordKey, out var value) ? (object?)value : null;
        }

        return result;
    }
}

public sealed record ListDatabasesResult(string[] Databases);

public sealed record GetDatabaseRecordResult(string DatabaseName, JsonElement Record);

public sealed record GetDatabaseStatsResult(string DatabaseName, JsonElement Stats);

public sealed record GetDetailedDatabaseStatsResult(string DatabaseName, JsonElement Stats);

public sealed record GetCollectionStatsResult(string DatabaseName, JsonElement Stats);

public sealed record GetDetailedCollectionStatsResult(string DatabaseName, JsonElement Stats);

public sealed record GetCollectionOverviewResult(
    string DatabaseName,
    JsonElement Stats,
    JsonElement DetailedStats);

public sealed record GetDatabaseConfigurationResult(string DatabaseName, JsonElement Configuration);

public sealed record GetClientConfigurationResult(string DatabaseName, JsonElement Configuration);
