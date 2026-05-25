using ModelContextProtocol.Server;
using RavenDB.Mcp.RavenDB;
using System.Text.Json;

namespace RavenDB.Mcp.Tools;

[McpServerToolType]
public static class ServerTools
{
    [McpServerTool(Name = "get_server_info", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetServerInfoResult> GetServerInfo(
        RavenDbAdminClient client,
        CancellationToken cancellationToken)
    {
        return client.GetServerInfo(cancellationToken);
    }

    [McpServerTool(Name = "get_cluster_topology", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetClusterTopologyResult> GetClusterTopology(
        RavenDbAdminClient client,
        CancellationToken cancellationToken)
    {
        return client.GetClusterTopology(cancellationToken);
    }

    [McpServerTool(Name = "get_node_status", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetNodeStatusResult> GetNodeStatus(
        RavenDbAdminClient client,
        CancellationToken cancellationToken)
    {
        return client.GetNodeStatus(cancellationToken);
    }

    [McpServerTool(Name = "get_logs_configuration", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetLogsConfigurationToolResult> GetLogsConfiguration(
        RavenDbAdminClient client,
        CancellationToken cancellationToken)
    {
        return client.GetLogsConfiguration(cancellationToken);
    }

    [McpServerTool(Name = "get_server_wide_client_configuration", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetServerWideClientConfigurationResult> GetServerWideClientConfiguration(
        RavenDbAdminClient client,
        CancellationToken cancellationToken)
    {
        return client.GetServerWideClientConfiguration(cancellationToken);
    }
}

public sealed record GetServerInfoResult(
    string ProductVersion,
    int BuildVersion,
    string CommitHash,
    string FullVersion,
    JsonElement NodeInfo);

public sealed record GetClusterTopologyResult(JsonElement Topology);

public sealed record GetNodeStatusResult(JsonElement Status);

public sealed record GetLogsConfigurationToolResult(JsonElement Configuration);

public sealed record GetServerWideClientConfigurationResult(JsonElement Configuration);
