using System.Text.Json;
using ModelContextProtocol.Server;
using RavenDB.Mcp.RavenDB;

namespace RavenDB.Mcp.Tools;

[McpServerToolType]
public static class ReplicationTools
{
    [McpServerTool(Name = "get_replication_tasks", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetReplicationTasksResult> GetReplicationTasks(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetReplicationTasks(databaseName, cancellationToken);
    }

    [McpServerTool(Name = "get_replication_performance", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetReplicationPerformanceResult> GetReplicationPerformance(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetReplicationPerformance(databaseName, cancellationToken);
    }
}

public sealed record GetReplicationTasksResult(string DatabaseName, JsonElement Tasks);

public sealed record GetReplicationPerformanceResult(string DatabaseName, JsonElement Performance);
