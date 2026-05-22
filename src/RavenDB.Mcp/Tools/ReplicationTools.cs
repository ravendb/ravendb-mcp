using System.Text.Json;
using ModelContextProtocol.Server;
using RavenDB.Mcp.RavenDB;

namespace RavenDB.Mcp.Tools;

[McpServerToolType]
public static class ReplicationTools
{
    [McpServerTool(Name = "get_replication_active_connections", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetReplicationActiveConnectionsResult> GetReplicationActiveConnections(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetReplicationActiveConnections(databaseName, cancellationToken);
    }

    [McpServerTool(Name = "get_replication_conflicts", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetReplicationConflictsResult> GetReplicationConflicts(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetReplicationConflicts(databaseName, cancellationToken);
    }

    [McpServerTool(Name = "get_replication_performance", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetReplicationPerformanceResult> GetReplicationPerformance(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetReplicationPerformance(databaseName, cancellationToken);
    }

    [McpServerTool(Name = "get_outgoing_replication_failures", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetOutgoingReplicationFailuresResult> GetOutgoingReplicationFailures(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetOutgoingReplicationFailures(databaseName, cancellationToken);
    }

    [McpServerTool(Name = "get_incoming_replication_rejection_info", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetIncomingReplicationRejectionInfoResult> GetIncomingReplicationRejectionInfo(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetIncomingReplicationRejectionInfo(databaseName, cancellationToken);
    }
}

public sealed record GetReplicationActiveConnectionsResult(string DatabaseName, JsonElement Connections);

public sealed record GetReplicationConflictsResult(string DatabaseName, JsonElement Conflicts);

public sealed record GetReplicationPerformanceResult(string DatabaseName, JsonElement Performance);

public sealed record GetOutgoingReplicationFailuresResult(string DatabaseName, JsonElement Failures);

public sealed record GetIncomingReplicationRejectionInfoResult(string DatabaseName, JsonElement Rejections);
