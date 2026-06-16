using ModelContextProtocol.Server;
using RavenDB.Mcp.RavenDB;
using System.ComponentModel;
using System.Text.Json;

namespace RavenDB.Mcp.Tools;

[McpServerToolType]
public static class ServerTools
{
    [McpServerTool(Name = "get_cluster_overview", ReadOnly = true)]
    [Description("Cluster and server overview. Sections: Nodes (topology, leader, per-node tag/type/url/health), ServerInfo (build/version + contacted node), ServerDiagnostics (routes/settings/metrics/license/idle DBs), ClusterDiagnostics (observer decisions, cluster log, engine logs), Notifications (server-wide alerts). Choose with include; default is Nodes + ServerInfo.")]
    public static async Task<Dictionary<string, object?>> GetClusterOverview(
        RavenDbAdminClient client,
        [Description("Sections to return; omit for Nodes + ServerInfo.")] ClusterInclude[]? include,
        CancellationToken cancellationToken)
    {
        var sections = Facet.Resolve(include, ClusterInclude.Nodes, ClusterInclude.ServerInfo);
        var result = new Dictionary<string, object?>();

        if (sections.Contains(ClusterInclude.Nodes)) result["nodes"] = await client.GetClusterNodes(cancellationToken);
        if (sections.Contains(ClusterInclude.ServerInfo)) result["serverInfo"] = await client.GetServerInfo(cancellationToken);
        if (sections.Contains(ClusterInclude.ServerDiagnostics)) result["serverDiagnostics"] = await client.GetServerDiagnosticsOverview(cancellationToken);
        if (sections.Contains(ClusterInclude.ClusterDiagnostics)) result["clusterDiagnostics"] = await client.GetClusterDiagnosticsOverview(cancellationToken);
        if (sections.Contains(ClusterInclude.Notifications)) result["notifications"] = await client.GetNotifications(null, cancellationToken);

        return result;
    }

    [McpServerTool(Name = "get_logs_configuration", ReadOnly = true)]
    [Description("Current server logging configuration: log mode/levels, paths, and retention settings.")]
    public static Task<GetLogsConfigurationToolResult> GetLogsConfiguration(
        RavenDbAdminClient client,
        CancellationToken cancellationToken)
    {
        return client.GetLogsConfiguration(cancellationToken);
    }

    [McpServerTool(Name = "get_server_wide_client_configuration", ReadOnly = true)]
    [Description("Server-wide client configuration RavenDB pushes to all clients: read balance, load-balancing, and max requests per session.")]
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
