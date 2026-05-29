using System.Text.Json;
using ModelContextProtocol.Server;
using RavenDB.Mcp.RavenDB;

namespace RavenDB.Mcp.Tools;

[McpServerToolType]
public static class OperationsTools
{
    [McpServerTool(Name = "get_operation_state", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetOperationStateResult> GetOperationState(
        RavenDbAdminClient client,
        string databaseName,
        long operationId,
        CancellationToken cancellationToken)
    {
        return client.GetOperationState(databaseName, operationId, cancellationToken);
    }

}

public sealed record GetOperationStateResult(string DatabaseName, long OperationId, JsonElement State);

public sealed record ListOngoingTasksResult(string DatabaseName, JsonElement Tasks);
