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

    [McpServerTool(Name = "get_database_stats", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetDatabaseStatsResult> GetDatabaseStats(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetDatabaseStats(databaseName, cancellationToken);
    }

    [McpServerTool(Name = "get_detailed_database_stats", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetDetailedDatabaseStatsResult> GetDetailedDatabaseStats(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetDetailedDatabaseStats(databaseName, cancellationToken);
    }

    [McpServerTool(Name = "get_collection_stats", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetCollectionStatsResult> GetCollectionStats(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetCollectionStats(databaseName, cancellationToken);
    }

    [McpServerTool(Name = "get_detailed_collection_stats", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetDetailedCollectionStatsResult> GetDetailedCollectionStats(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetDetailedCollectionStats(databaseName, cancellationToken);
    }

    [McpServerTool(Name = "get_database_health_summary", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetDatabaseHealthSummaryResult> GetDatabaseHealthSummary(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetDatabaseHealthSummary(databaseName, cancellationToken);
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

public sealed record GetDatabaseHealthSummaryResult(
    string DatabaseName,
    JsonElement Stats,
    JsonElement IndexingStatus,
    JsonElement IndexStats,
    JsonElement IndexErrors,
    JsonElement Tasks);

public sealed record GetDatabaseConfigurationResult(string DatabaseName, JsonElement Configuration);

public sealed record GetClientConfigurationResult(string DatabaseName, JsonElement Configuration);
