using System.Text.Json;
using ModelContextProtocol.Server;
using RavenDB.Mcp.RavenDB;

namespace RavenDB.Mcp.Tools;

[McpServerToolType]
public static class OperationsTools
{
    [McpServerTool(Name = "list_running_operations", ReadOnly = true, UseStructuredContent = true)]
    public static Task<ListRunningOperationsResult> ListRunningOperations(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.ListRunningOperations(databaseName, cancellationToken);
    }

    [McpServerTool(Name = "get_operation_state", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetOperationStateResult> GetOperationState(
        RavenDbAdminClient client,
        string databaseName,
        long operationId,
        CancellationToken cancellationToken)
    {
        return client.GetOperationState(databaseName, operationId, cancellationToken);
    }

    [McpServerTool(Name = "list_running_queries", ReadOnly = true, UseStructuredContent = true)]
    public static Task<ListRunningQueriesResult> ListRunningQueries(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.ListRunningQueries(databaseName, cancellationToken);
    }

    [McpServerTool(Name = "get_query_cache_info", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetQueryCacheInfoResult> GetQueryCacheInfo(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetQueryCacheInfo(databaseName, cancellationToken);
    }
}

public sealed record ListRunningOperationsResult(string DatabaseName, JsonElement Operations);

public sealed record GetOperationStateResult(string DatabaseName, long OperationId, JsonElement State);

public sealed record ListRunningQueriesResult(string DatabaseName, JsonElement Queries);

public sealed record GetQueryCacheInfoResult(string DatabaseName, JsonElement Cache);
