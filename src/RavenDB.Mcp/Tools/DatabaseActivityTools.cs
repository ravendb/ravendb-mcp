using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using RavenDB.Mcp.RavenDB;

namespace RavenDB.Mcp.Tools;

[McpServerToolType]
public static class DatabaseActivityTools
{
    [McpServerTool(Name = "get_identities", ReadOnly = true)]
    [Description("Identity counters for a database (the server-side sequence values behind identity document ids).")]
    public static Task<GetIdentitiesResult> GetIdentities(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetIdentities(databaseName, cancellationToken);
    }
}

public sealed record GetSubscriptionsResult(string DatabaseName, JsonElement Subscriptions);

public sealed record GetSubscriptionStateResult(
    string DatabaseName,
    string SubscriptionName,
    JsonElement State);

public sealed record GetDatabaseTcpInfoResult(string DatabaseName, string NodeTag, JsonElement Tcp);

public sealed record GetIdentitiesResult(string DatabaseName, JsonElement Identities);
