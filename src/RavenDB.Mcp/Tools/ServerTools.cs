using ModelContextProtocol.Server;
using RavenDB.Mcp.RavenDB;
using System.Text.Json;

namespace RavenDB.Mcp.Tools;

[McpServerToolType]
public static class ServerTools
{
    [McpServerTool(Name = "get_cluster_nodes", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetClusterNodesResult> GetClusterNodes(
        RavenDbAdminClient client,
        CancellationToken cancellationToken)
    {
        return client.GetClusterNodes(cancellationToken);
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

public sealed record ServerBuildResult(
    string ProductVersion,
    int BuildVersion,
    string? AssemblyVersion,
    string CommitHash,
    string FullVersion);

public sealed record GetServerInfoResult(
    string ProductVersion,
    int BuildVersion,
    string CommitHash,
    string FullVersion,
    JsonElement NodeInfo);

public sealed record GetClusterTopologyResult(JsonElement Topology);

public sealed record CurrentNodeResult(
    string? NodeTag,
    Guid ServerId,
    string? TopologyId,
    string? ClusterStatus,
    string? CurrentState,
    string? ServerRole,
    int ServerSchemaVersion,
    bool HasFixedPort,
    int NumberOfCores,
    double InstalledMemoryInGb,
    double UsableMemoryInGb,
    bool CertificatePresent,
    OsInfoResult? Os);

public sealed record OsInfoResult(
    string? Type,
    string? FullName,
    string? Version,
    string? BuildVersion,
    bool Is64Bit);

public sealed record ClusterNodeStatusResult(
    string? Name,
    bool Connected,
    DateTime LastSent,
    DateTime LastReply,
    string? LastSentMessage,
    long LastMatchingIndex,
    string? ErrorDetails);

public sealed record ClusterNodeResult(
    string Tag,
    string Type,
    string? Url,
    ClusterNodeStatusResult? Status,
    ServerBuildResult? Server,
    CurrentNodeResult? Self,
    string? Error);

public sealed record ClusterResult(
    string? TopologyId,
    long Etag,
    string? Leader,
    string? RespondingNodeTag,
    string? RespondingNodeRole,
    string? LastNodeId,
    ClusterNodeResult[] Nodes);

public sealed record GetClusterNodesResult(
    ServerBuildResult Server,
    CurrentNodeResult CurrentNode,
    ClusterResult Cluster);

public sealed record GetLogsConfigurationToolResult(JsonElement Configuration);

public sealed record GetServerWideClientConfigurationResult(JsonElement Configuration);
