using System.Text.Json;
using ModelContextProtocol.Server;
using RavenDB.Mcp.RavenDB;

namespace RavenDB.Mcp.Tools;

[McpServerToolType]
public static class IndexTools
{
    [McpServerTool(Name = "list_indexes", ReadOnly = true, UseStructuredContent = true)]
    public static Task<ListIndexesResult> ListIndexes(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.ListIndexes(databaseName, cancellationToken);
    }

    [McpServerTool(Name = "get_index_stats", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetIndexStatsResult> GetIndexStats(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetIndexStats(databaseName, cancellationToken);
    }

    [McpServerTool(Name = "get_index_errors", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetIndexErrorsResult> GetIndexErrors(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetIndexErrors(databaseName, cancellationToken);
    }

    [McpServerTool(Name = "get_index_performance", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetIndexPerformanceResult> GetIndexPerformance(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetIndexPerformance(databaseName, cancellationToken);
    }

    [McpServerTool(Name = "get_indexing_status", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetIndexingStatusResult> GetIndexingStatus(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetIndexingStatus(databaseName, cancellationToken);
    }

    [McpServerTool(Name = "get_index_progress", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetIndexProgressResult> GetIndexProgress(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetIndexProgress(databaseName, cancellationToken);
    }

    [McpServerTool(Name = "get_index_staleness", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetIndexStalenessResult> GetIndexStaleness(
        RavenDbAdminClient client,
        string databaseName,
        string indexName,
        CancellationToken cancellationToken)
    {
        return client.GetIndexStaleness(databaseName, indexName, cancellationToken);
    }

    [McpServerTool(Name = "get_suggested_index_merges", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetSuggestedIndexMergesResult> GetSuggestedIndexMerges(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetSuggestedIndexMerges(databaseName, cancellationToken);
    }
}

public sealed record ListIndexesResult(string DatabaseName, JsonElement Indexes);

public sealed record GetIndexStatsResult(string DatabaseName, JsonElement Stats);

public sealed record GetIndexErrorsResult(string DatabaseName, JsonElement Errors);

public sealed record GetIndexPerformanceResult(string DatabaseName, JsonElement Performance);

public sealed record GetIndexingStatusResult(string DatabaseName, JsonElement Status);

public sealed record GetIndexProgressResult(string DatabaseName, JsonElement Progress);

public sealed record GetIndexStalenessResult(string DatabaseName, string IndexName, JsonElement Staleness);

public sealed record GetSuggestedIndexMergesResult(string DatabaseName, JsonElement Merges);
