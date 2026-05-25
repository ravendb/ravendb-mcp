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
        Assert.Equal(JsonValueKind.Object, serverInfo.NodeInfo.ValueKind);

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

        var topology = await client.GetClusterTopology(timeout.Token);
        var nodeTag = topology.Topology.GetProperty("NodeTag").GetString()!;

        Assert.Equal(JsonValueKind.Object, topology.Topology.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.GetNodeStatus(timeout.Token)).Status.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.GetLogsConfiguration(timeout.Token)).Configuration.ValueKind);

        var stats = await client.GetDatabaseStats(fixture.DatabaseName, timeout.Token);
        Assert.Equal(fixture.DatabaseName, stats.DatabaseName);
        Assert.Equal(JsonValueKind.Object, stats.Stats.ValueKind);

        Assert.Equal(JsonValueKind.Object, (await client.GetDetailedDatabaseStats(fixture.DatabaseName, timeout.Token)).Stats.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.GetCollectionStats(fixture.DatabaseName, timeout.Token)).Stats.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.GetDetailedCollectionStats(fixture.DatabaseName, timeout.Token)).Stats.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.GetDatabaseConfiguration(fixture.DatabaseName, timeout.Token)).Configuration.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.GetClientConfiguration(fixture.DatabaseName, timeout.Token)).Configuration.ValueKind);
        var serverWideClientConfiguration = await client.GetServerWideClientConfiguration(timeout.Token);
        Assert.True(serverWideClientConfiguration.Configuration.ValueKind is JsonValueKind.Object or JsonValueKind.Null);
        Assert.Equal(JsonValueKind.Object, (await client.GetDatabaseHealthSummary(fixture.DatabaseName, timeout.Token)).Stats.ValueKind);

        Assert.Equal(JsonValueKind.Array, (await client.ListIndexes(fixture.DatabaseName, timeout.Token)).Indexes.ValueKind);
        Assert.Equal(JsonValueKind.Array, (await client.GetIndexStats(fixture.DatabaseName, timeout.Token)).Stats.ValueKind);
        Assert.Equal(JsonValueKind.Array, (await client.GetIndexErrors(fixture.DatabaseName, timeout.Token)).Errors.ValueKind);
        Assert.Equal(JsonValueKind.Array, (await client.GetIndexPerformance(fixture.DatabaseName, timeout.Token)).Performance.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.GetIndexingStatus(fixture.DatabaseName, timeout.Token)).Status.ValueKind);

        var index = await client.GetIndex(fixture.DatabaseName, fixture.IndexName, timeout.Token);
        Assert.Equal(fixture.IndexName, index.IndexName);
        Assert.Equal(JsonValueKind.Object, index.Index.ValueKind);

        var terms = await client.GetIndexTerms(
            fixture.DatabaseName,
            fixture.IndexName,
            fixture.IndexFieldName,
            null,
            16,
            timeout.Token);
        Assert.Equal(JsonValueKind.Array, terms.Terms.ValueKind);

        Assert.Equal(JsonValueKind.Object, (await client.GetReplicationPerformance(fixture.DatabaseName, timeout.Token)).Performance.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.GetReplicationTasks(fixture.DatabaseName, timeout.Token)).Tasks.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.GetBackupTasks(fixture.DatabaseName, timeout.Token)).Tasks.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.GetEtlTasks(fixture.DatabaseName, timeout.Token)).Tasks.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.ListOngoingTasks(fixture.DatabaseName, timeout.Token)).Tasks.ValueKind);

        Assert.Equal(JsonValueKind.Array, (await client.GetSubscriptions(fixture.DatabaseName, timeout.Token)).Subscriptions.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.GetDatabaseTcpInfo(fixture.DatabaseName, nodeTag, timeout.Token)).Tcp.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.GetIdentities(fixture.DatabaseName, timeout.Token)).Identities.ValueKind);
    }

    [Fact]
    public async Task ReadsStorageDiagnostics()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var client = new RavenDbAdminClient(fixture.Store);

        var overview = await client.GetStorageOverview(fixture.DatabaseName, timeout.Token);
        Assert.Equal(fixture.DatabaseName, overview.DatabaseName);
        Assert.Equal(JsonValueKind.Object, overview.Report.ValueKind);
        Assert.Equal(JsonValueKind.Object, overview.Environments.ValueKind);

        Assert.Equal(JsonValueKind.Object, (await client.GetStorageTrees(fixture.DatabaseName, timeout.Token)).Trees.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.GetStorageEnvironmentReport(fixture.DatabaseName, null, null, timeout.Token)).Report.ValueKind);

        var tree = await client.GetStorageTreeStructure(fixture.DatabaseName, "Docs", null, timeout.Token);
        Assert.Equal("Docs", tree.TreeName);
        Assert.NotEmpty(tree.Structure);

        Assert.Equal(JsonValueKind.Object, (await client.GetStorageCompressionDictionaries(fixture.DatabaseName, timeout.Token)).Dictionaries.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.GetStorageScratchBufferInfo(fixture.DatabaseName, null, null, timeout.Token)).ScratchBuffers.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.GetStorageFreeSpaceSnapshot(fixture.DatabaseName, null, null, timeout.Token)).FreeSpace.ValueKind);
    }

    [Fact]
    public async Task ReadsPerformanceDiagnostics()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var client = new RavenDbAdminClient(fixture.Store);

        Assert.Equal(JsonValueKind.Object, (await client.GetPerformanceOverview(timeout.Token)).Metrics.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.GetCpuStats(timeout.Token)).Cpu.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.GetIoStats(null, timeout.Token)).Io.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.GetIoStats(fixture.DatabaseName, timeout.Token)).Io.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.GetGcMemoryStats(timeout.Token)).Gc.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.GetOsMemoryStats(timeout.Token)).Memory.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.GetProcessStats(timeout.Token)).Process.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.GetLowMemoryLog(timeout.Token)).Log.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.GetEncryptionBufferPoolStats(timeout.Token)).BufferPool.ValueKind);
        Assert.Equal(JsonValueKind.Array, (await client.GetThreadStats(timeout.Token)).Threads.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.GetStackTraces(timeout.Token)).StackTraces.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.GetScriptRunners(null, timeout.Token)).ScriptRunners.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.GetScriptRunners(fixture.DatabaseName, timeout.Token)).ScriptRunners.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.GetTcpStats(timeout.Token)).Tcp.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.ListTcpConnections(null, timeout.Token)).Connections.ValueKind);
        Assert.Equal(JsonValueKind.Object, (await client.ListTcpConnections(fixture.DatabaseName, timeout.Token)).Connections.ValueKind);

        var runtimeEvents = await client.SampleRuntimeEvents("gc", 1, timeout.Token);
        Assert.Equal("gc", runtimeEvents.Kind);
        Assert.Equal(1, runtimeEvents.Seconds);

        var threadDiagnostics = await client.SampleThreadDiagnostics("runaway", 1, timeout.Token);
        Assert.Equal("runaway", threadDiagnostics.Kind);
        Assert.NotEmpty(threadDiagnostics.Sample);
    }
}
