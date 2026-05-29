using System.Text.Json;
using ModelContextProtocol.Server;
using RavenDB.Mcp.RavenDB;

namespace RavenDB.Mcp.Tools;

[McpServerToolType]
public static class DatabaseTools
{
    [McpServerTool(Name = "list_databases", ReadOnly = true, UseStructuredContent = true)]
    public static Task<ListDatabasesResult> ListDatabases(
        RavenDbAdminClient client,
        CancellationToken cancellationToken)
    {
        return client.ListDatabases(cancellationToken);
    }

    [McpServerTool(Name = "get_database_record", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetDatabaseRecordResult> GetDatabaseRecord(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetDatabaseRecord(databaseName, cancellationToken);
    }

    [McpServerTool(Name = "get_collection_overview", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetCollectionOverviewResult> GetCollectionOverview(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetCollectionOverview(databaseName, cancellationToken);
    }

    [McpServerTool(Name = "get_database_overview", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetDatabaseOverviewResult> GetDatabaseOverview(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetDatabaseOverview(databaseName, cancellationToken);
    }

    [McpServerTool(Name = "get_database_configuration", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetDatabaseConfigurationResult> GetDatabaseConfiguration(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetDatabaseConfiguration(databaseName, cancellationToken);
    }

    [McpServerTool(Name = "get_client_configuration", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetClientConfigurationResult> GetClientConfiguration(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetClientConfiguration(databaseName, cancellationToken);
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

public sealed record GetDatabaseHealthSummaryResult(
    string DatabaseName,
    JsonElement Stats,
    JsonElement IndexingStatus,
    JsonElement IndexStats,
    JsonElement IndexErrors,
    JsonElement Tasks);

public sealed record GetDatabaseConfigurationResult(string DatabaseName, JsonElement Configuration);

public sealed record GetClientConfigurationResult(string DatabaseName, JsonElement Configuration);
