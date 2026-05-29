using System.Text.Json;
using ModelContextProtocol.Server;
using RavenDB.Mcp.RavenDB;

namespace RavenDB.Mcp.Tools;

[McpServerToolType]
public static class StoragePerformanceTools
{
    [McpServerTool(Name = "get_storage_overview", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetStorageOverviewResult> GetStorageOverview(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetStorageOverview(databaseName, cancellationToken);
    }

    [McpServerTool(Name = "get_storage_trees", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetStorageTreesResult> GetStorageTrees(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetStorageTrees(databaseName, cancellationToken);
    }

    [McpServerTool(Name = "get_storage_environment_details", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetStorageEnvironmentDetailsResult> GetStorageEnvironmentDetails(
        RavenDbAdminClient client,
        string databaseName,
        string? environmentName,
        string? environmentType,
        CancellationToken cancellationToken)
    {
        return client.GetStorageEnvironmentDetails(databaseName, environmentName, environmentType, cancellationToken);
    }

    [McpServerTool(Name = "get_storage_tree_structure", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetStorageTreeStructureResult> GetStorageTreeStructure(
        RavenDbAdminClient client,
        string databaseName,
        string treeName,
        string? treeKind,
        CancellationToken cancellationToken)
    {
        return client.GetStorageTreeStructure(databaseName, treeName, treeKind, cancellationToken);
    }

    [McpServerTool(Name = "get_storage_compression_dictionaries", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetStorageCompressionDictionariesResult> GetStorageCompressionDictionaries(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetStorageCompressionDictionaries(databaseName, cancellationToken);
    }

    [McpServerTool(Name = "get_server_resources", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetServerResourcesResult> GetServerResources(
        RavenDbAdminClient client,
        CancellationToken cancellationToken)
    {
        return client.GetServerResources(cancellationToken);
    }

    [McpServerTool(Name = "get_io_stats", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetIoStatsResult> GetIoStats(
        RavenDbAdminClient client,
        string? databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetIoStats(databaseName, cancellationToken);
    }

    [McpServerTool(Name = "get_low_memory_log", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetLowMemoryLogResult> GetLowMemoryLog(
        RavenDbAdminClient client,
        CancellationToken cancellationToken)
    {
        return client.GetLowMemoryLog(cancellationToken);
    }

    [McpServerTool(Name = "get_encryption_buffer_pool_stats", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetEncryptionBufferPoolStatsResult> GetEncryptionBufferPoolStats(
        RavenDbAdminClient client,
        CancellationToken cancellationToken)
    {
        return client.GetEncryptionBufferPoolStats(cancellationToken);
    }

    [McpServerTool(Name = "sample_runtime_events", ReadOnly = true, UseStructuredContent = true)]
    public static Task<SampleRuntimeEventsResult> SampleRuntimeEvents(
        RavenDbAdminClient client,
        string kind,
        int seconds,
        CancellationToken cancellationToken)
    {
        return client.SampleRuntimeEvents(kind, seconds, cancellationToken);
    }

    [McpServerTool(Name = "sample_thread_diagnostics", ReadOnly = true, UseStructuredContent = true)]
    public static Task<SampleThreadDiagnosticsResult> SampleThreadDiagnostics(
        RavenDbAdminClient client,
        string kind,
        int seconds,
        CancellationToken cancellationToken)
    {
        return client.SampleThreadDiagnostics(kind, seconds, cancellationToken);
    }

    [McpServerTool(Name = "get_stack_traces", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetStackTracesResult> GetStackTraces(
        RavenDbAdminClient client,
        CancellationToken cancellationToken)
    {
        return client.GetStackTraces(cancellationToken);
    }

    [McpServerTool(Name = "get_script_runners", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetScriptRunnersResult> GetScriptRunners(
        RavenDbAdminClient client,
        string? databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetScriptRunners(databaseName, cancellationToken);
    }

    [McpServerTool(Name = "get_tcp_stats", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetTcpStatsResult> GetTcpStats(
        RavenDbAdminClient client,
        CancellationToken cancellationToken)
    {
        return client.GetTcpStats(cancellationToken);
    }

    [McpServerTool(Name = "list_tcp_connections", ReadOnly = true, UseStructuredContent = true)]
    public static Task<ListTcpConnectionsResult> ListTcpConnections(
        RavenDbAdminClient client,
        string? databaseName,
        CancellationToken cancellationToken)
    {
        return client.ListTcpConnections(databaseName, cancellationToken);
    }
}

public sealed record GetStorageOverviewResult(string DatabaseName, JsonElement Report, JsonElement Environments);

public sealed record GetStorageTreesResult(string DatabaseName, JsonElement Trees);

public sealed record GetStorageEnvironmentReportResult(
    string DatabaseName,
    string EnvironmentName,
    string EnvironmentType,
    JsonElement Report);

public sealed record GetStorageEnvironmentDetailsResult(
    string DatabaseName,
    string EnvironmentName,
    string EnvironmentType,
    JsonElement Report,
    JsonElement ScratchBuffers,
    JsonElement FreeSpace);

public sealed record GetStorageTreeStructureResult(
    string DatabaseName,
    string TreeName,
    string TreeKind,
    string Structure);

public sealed record GetStorageCompressionDictionariesResult(string DatabaseName, JsonElement Dictionaries);

public sealed record GetStorageScratchBufferInfoResult(
    string DatabaseName,
    string EnvironmentName,
    string EnvironmentType,
    JsonElement ScratchBuffers);

public sealed record GetStorageFreeSpaceSnapshotResult(
    string DatabaseName,
    string EnvironmentName,
    string EnvironmentType,
    JsonElement FreeSpace);

public sealed record GetPerformanceOverviewResult(JsonElement Metrics);

public sealed record GetServerResourcesResult(
    JsonElement Metrics,
    JsonElement Cpu,
    JsonElement Io,
    JsonElement Gc,
    JsonElement Memory,
    JsonElement Process,
    JsonElement Threads);

public sealed record GetCpuStatsResult(JsonElement Cpu);

public sealed record GetIoStatsResult(string? DatabaseName, JsonElement Io);

public sealed record GetGcMemoryStatsResult(JsonElement Gc);

public sealed record GetOsMemoryStatsResult(JsonElement Memory);

public sealed record GetProcessStatsResult(JsonElement Process);

public sealed record GetLowMemoryLogResult(JsonElement Log);

public sealed record GetEncryptionBufferPoolStatsResult(JsonElement BufferPool);

public sealed record SampleRuntimeEventsResult(string Kind, int Seconds, string Sample);

public sealed record GetThreadStatsResult(JsonElement Threads);

public sealed record SampleThreadDiagnosticsResult(string Kind, int Seconds, string Sample);

public sealed record GetStackTracesResult(JsonElement StackTraces);

public sealed record GetScriptRunnersResult(string? DatabaseName, JsonElement ScriptRunners);

public sealed record GetTcpStatsResult(JsonElement Tcp);

public sealed record ListTcpConnectionsResult(string? DatabaseName, JsonElement Connections);
