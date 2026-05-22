using Raven.Client.Documents;
using Raven.Client.ServerWide.Operations;
using RavenDB.Mcp.Tools;

namespace RavenDB.Mcp.RavenDB;

public sealed class RavenDbAdminClient(IDocumentStore store)
{
    public async Task<ListDatabasesResult> ListDatabases(CancellationToken cancellationToken)
    {
        var databaseNames = await store.Maintenance.Server.SendAsync(
            new GetDatabaseNamesOperation(0, int.MaxValue),
            cancellationToken);

        return new ListDatabasesResult(databaseNames);
    }

    public async Task<GetDatabaseRecordResult> GetDatabaseRecord(
        string databaseName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(databaseName))
            throw new ArgumentException("Database name is required.", nameof(databaseName));

        var record = await store.Maintenance.Server.SendAsync(
            new GetDatabaseRecordOperation(databaseName),
            cancellationToken);

        if (record is null)
            throw new InvalidOperationException($"Database '{databaseName}' was not found.");

        return new GetDatabaseRecordResult(databaseName, record);
    }

    public async Task<GetServerInfoResult> GetServerInfo(CancellationToken cancellationToken)
    {
        var buildNumber = await store.Maintenance.Server.SendAsync(
            new GetBuildNumberOperation(),
            cancellationToken);

        return new GetServerInfoResult(
            buildNumber.ProductVersion,
            buildNumber.BuildVersion,
            buildNumber.CommitHash,
            buildNumber.FullVersion);
    }
}
