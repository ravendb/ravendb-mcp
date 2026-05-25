using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using Raven.Client.Documents.Operations.Indexes;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;
using RavenDB.Mcp.Configuration;
using RavenDB.Mcp.RavenDB;

namespace RavenDB.Mcp.Tests;

public sealed class RavenDbTestFixture : IAsyncLifetime
{
    public string DatabaseName { get; } = $"RavenDB_Mcp_Tests_{Guid.NewGuid():N}";

    public string IndexName { get; } = "TestUsers/ByName";

    public string IndexFieldName { get; } = "Name";

    public string Url { get; } = Environment.GetEnvironmentVariable("RAVENDB_TEST_URL")
        ?? throw new InvalidOperationException("RAVENDB_TEST_URL must be set for RavenDB-backed tests.");

    public IDocumentStore Store { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Store = DocumentStoreFactory.Create(new RavenDbOptions { Urls = [Url] });

        await Store.Maintenance.Server.SendAsync(
            new CreateDatabaseOperation(new DatabaseRecord(DatabaseName)));

        await SeedDatabase();
    }

    public async Task DisposeAsync()
    {
        if (Store is null)
            return;

        try
        {
            await Store.Maintenance.Server.SendAsync(
                new DeleteDatabasesOperation(DatabaseName, hardDelete: true));
        }
        finally
        {
            Store.Dispose();
        }
    }

    private async Task SeedDatabase()
    {
        using (var session = Store.OpenAsyncSession(DatabaseName))
        {
            await session.StoreAsync(new TestUser("Ada"));
            await session.SaveChangesAsync();
        }

        var index = new IndexDefinition { Name = IndexName };
        index.Maps.Add("from user in docs.TestUsers select new { user.Name }");

        await Store.Maintenance.ForDatabase(DatabaseName).SendAsync(new PutIndexesOperation(index));
    }

    private sealed record TestUser(string Name);
}
