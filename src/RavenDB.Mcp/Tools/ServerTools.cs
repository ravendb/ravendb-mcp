using ModelContextProtocol.Server;
using RavenDB.Mcp.RavenDB;

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
}

public sealed record GetServerInfoResult(
    string ProductVersion,
    int BuildVersion,
    string CommitHash,
    string FullVersion);
