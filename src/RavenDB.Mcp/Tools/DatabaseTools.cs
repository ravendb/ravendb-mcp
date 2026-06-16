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

    [McpServerTool(Name = "get_collection_overview", ReadOnly = true)]
    [Description("Collection statistics for a database: per-collection document counts plus detailed size/document totals.")]
    public static Task<GetCollectionOverviewResult> GetCollectionOverview(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetCollectionOverview(databaseName, cancellationToken);
    }

    [McpServerTool(Name = "get_database_overview", ReadOnly = true)]
    [Description("One-call health snapshot for a database: stats, detailed stats, indexing status, index stats, index errors, and ongoing tasks.")]
    public static Task<GetDatabaseOverviewResult> GetDatabaseOverview(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetDatabaseOverview(databaseName, cancellationToken);
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
        [Description("Sections to return; omit for all.")] DatabaseConfigSection[]? include,
        CancellationToken cancellationToken)
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

public sealed record GetDatabaseOverviewResult(
    string DatabaseName,
    JsonElement Stats,
    JsonElement DetailedStats,
    JsonElement IndexingStatus,
    JsonElement IndexStats,
    JsonElement IndexErrors,
    JsonElement Tasks);

public sealed record GetDatabaseConfigurationResult(string DatabaseName, JsonElement Configuration);

public sealed record GetClientConfigurationResult(string DatabaseName, JsonElement Configuration);
