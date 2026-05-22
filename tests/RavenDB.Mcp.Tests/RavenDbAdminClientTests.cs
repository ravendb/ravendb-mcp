using System.Text.Json;
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

    [Fact]
    public async Task ReadsBetaDiagnosticCategories()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var client = new RavenDbAdminClient(fixture.Store);

        Assert.Equal(JsonValueKind.Object, (await client.GetClusterTopology(timeout.Token)).Topology.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.GetNodeInfo(timeout.Token)).NodeInfo.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.GetNodeStatus(timeout.Token)).Status.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.GetServerMetrics(timeout.Token)).Metrics.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.GetServerConfiguration(timeout.Token)).Configuration.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.GetStudioConfiguration(timeout.Token)).Configuration.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.GetLogsConfiguration(timeout.Token)).Configuration.ValueKind);

        var stats = await client.GetDatabaseStats(fixture.DatabaseName, timeout.Token);
        Assert.Equal(fixture.DatabaseName, stats.DatabaseName);
        Assert.Equal(JsonValueKind.Object, stats.Stats.ValueKind);

        Assert.Equal(JsonValueKind.Object, (await client.GetDetailedDatabaseStats(fixture.DatabaseName, timeout.Token)).Stats.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.GetCollectionStats(fixture.DatabaseName, timeout.Token)).Stats.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.GetDetailedCollectionStats(fixture.DatabaseName, timeout.Token)).Stats.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.GetClientConfiguration(fixture.DatabaseName, timeout.Token)).Configuration.ValueKind);

        Assert.Equal(JsonValueKind.Array, (await client.ListIndexes(fixture.DatabaseName, timeout.Token)).Indexes.ValueKind);
        Assert.Equal(JsonValueKind.Array, (await client.GetIndexStats(fixture.DatabaseName, timeout.Token)).Stats.ValueKind);
        Assert.Equal(JsonValueKind.Array, (await client.GetIndexErrors(fixture.DatabaseName, timeout.Token)).Errors.ValueKind);
        Assert.Equal(JsonValueKind.Array, (await client.GetIndexPerformance(fixture.DatabaseName, timeout.Token)).Performance.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.GetIndexingStatus(fixture.DatabaseName, timeout.Token)).Status.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.GetIndexProgress(fixture.DatabaseName, timeout.Token)).Progress.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.GetSuggestedIndexMerges(fixture.DatabaseName, timeout.Token)).Merges.ValueKind);

        Assert.Equal(JsonValueKind.Object, (await client.ListRunningOperations(fixture.DatabaseName, timeout.Token)).Operations.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.ListRunningQueries(fixture.DatabaseName, timeout.Token)).Queries.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.GetQueryCacheInfo(fixture.DatabaseName, timeout.Token)).Cache.ValueKind);

        Assert.Equal(JsonValueKind.Object, (await client.GetReplicationActiveConnections(fixture.DatabaseName, timeout.Token)).Connections.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.GetReplicationConflicts(fixture.DatabaseName, timeout.Token)).Conflicts.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.GetReplicationPerformance(fixture.DatabaseName, timeout.Token)).Performance.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.GetOutgoingReplicationFailures(fixture.DatabaseName, timeout.Token)).Failures.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.GetIncomingReplicationRejectionInfo(fixture.DatabaseName, timeout.Token)).Rejections.ValueKind);

        Assert.Equal(JsonValueKind.Object, (await client.GetNextBackupOccurrences(fixture.DatabaseName, timeout.Token)).Occurrences.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.ListOngoingTasks(fixture.DatabaseName, timeout.Token)).Tasks.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.GetEtlStats(fixture.DatabaseName, timeout.Token)).Stats.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.GetEtlPerformance(fixture.DatabaseName, timeout.Token)).Performance.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.GetEtlDebugStats(fixture.DatabaseName, timeout.Token)).Stats.ValueKind);

        Assert.Equal(JsonValueKind.Object, (await client.GetSubscriptions(fixture.DatabaseName, timeout.Token)).Subscriptions.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.GetNotificationCenterAlerts(fixture.DatabaseName, timeout.Token)).Alerts.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.GetDatabaseTcpInfo(fixture.DatabaseName, timeout.Token)).Tcp.ValueKind);
    }
}
