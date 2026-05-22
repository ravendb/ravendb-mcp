using RavenDB.Mcp.RavenDB;

namespace RavenDB.Mcp.Tests;

public sealed class RavenDbAdminClientTests(RavenDbTestFixture fixture)
    : IClassFixture<RavenDbTestFixture>
{
    [Fact]
    public async Task ReadsServerInfoAndDatabaseRecord()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var client = new RavenDbAdminClient(fixture.Store);

        var serverInfo = await client.GetServerInfo(timeout.Token);
        Assert.NotEmpty(serverInfo.ProductVersion);
        Assert.True(serverInfo.BuildVersion > 0);

        var databases = await client.ListDatabases(timeout.Token);
        Assert.Contains(fixture.DatabaseName, databases.Databases);

        var databaseRecord = await client.GetDatabaseRecord(
            fixture.DatabaseName,
            timeout.Token);

        Assert.Equal(fixture.DatabaseName, databaseRecord.DatabaseName);
        Assert.Equal(fixture.DatabaseName, databaseRecord.Record.GetProperty("DatabaseName").GetString());
    }
}
