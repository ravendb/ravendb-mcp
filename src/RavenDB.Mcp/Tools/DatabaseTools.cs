using System.Text.Json;
using ModelContextProtocol.Server;
using RavenDB.Mcp.RavenDB;

namespace RavenDB.Mcp.Tools;

[McpServerToolType]
public static class DatabaseTools
{
    [McpServerTool(Name = "list_databases", ReadOnly = true, UseStructuredContent = true)]
    public static Task<ListDatabasesResult> ListDatabases(
        RavenDbAdminClient client,
        CancellationToken cancellationToken)
    {
        return client.ListDatabases(cancellationToken);
    }

    [McpServerTool(Name = "get_database_record", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetDatabaseRecordResult> GetDatabaseRecord(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetDatabaseRecord(databaseName, cancellationToken);
    }
}

public sealed record ListDatabasesResult(string[] Databases);

public sealed record GetDatabaseRecordResult(string DatabaseName, JsonElement Record);
