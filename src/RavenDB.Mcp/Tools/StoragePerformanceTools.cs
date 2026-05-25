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

    [McpServerTool(Name = "get_storage_environment_report", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetStorageEnvironmentReportResult> GetStorageEnvironmentReport(
        RavenDbAdminClient client,
        string databaseName,
        string? environmentName,
        string? environmentType,
        CancellationToken cancellationToken)
    {
        return client.GetStorageEnvironmentReport(databaseName, environmentName, environmentType, cancellationToken);
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

    [McpServerTool(Name = "get_storage_scratch_buffer_info", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetStorageScratchBufferInfoResult> GetStorageScratchBufferInfo(
        RavenDbAdminClient client,
        string databaseName,
        string? environmentName,
        string? environmentType,
        CancellationToken cancellationToken)
    {
        return client.GetStorageScratchBufferInfo(databaseName, environmentName, environmentType, cancellationToken);
    }

    [McpServerTool(Name = "get_storage_free_space_snapshot", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetStorageFreeSpaceSnapshotResult> GetStorageFreeSpaceSnapshot(
        RavenDbAdminClient client,
        string databaseName,
        string? environmentName,
        string? environmentType,
        CancellationToken cancellationToken)
    {
        return client.GetStorageFreeSpaceSnapshot(databaseName, environmentName, environmentType, cancellationToken);
    }

    [McpServerTool(Name = "get_performance_overview", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetPerformanceOverviewResult> GetPerformanceOverview(
        RavenDbAdminClient client,
        CancellationToken cancellationToken)
    {
        return client.GetPerformanceOverview(cancellationToken);
    }

    [McpServerTool(Name = "get_cpu_stats", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetCpuStatsResult> GetCpuStats(
        RavenDbAdminClient client,
        CancellationToken cancellationToken)
    {
        return client.GetCpuStats(cancellationToken);
    }

    [McpServerTool(Name = "get_io_stats", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetIoStatsResult> GetIoStats(
        RavenDbAdminClient client,
        string? databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetIoStats(databaseName, cancellationToken);
    }

    [McpServerTool(Name = "get_gc_memory_stats", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetGcMemoryStatsResult> GetGcMemoryStats(
        RavenDbAdminClient client,
        CancellationToken cancellationToken)
    {
        return client.GetGcMemoryStats(cancellationToken);
    }

    [McpServerTool(Name = "get_os_memory_stats", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetOsMemoryStatsResult> GetOsMemoryStats(
        RavenDbAdminClient client,
        CancellationToken cancellationToken)
    {
        return client.GetOsMemoryStats(cancellationToken);
    }

    [McpServerTool(Name = "get_process_stats", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetProcessStatsResult> GetProcessStats(
        RavenDbAdminClient client,
        CancellationToken cancellationToken)
    {
        return client.GetProcessStats(cancellationToken);
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

    [McpServerTool(Name = "get_thread_stats", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetThreadStatsResult> GetThreadStats(
        RavenDbAdminClient client,
        CancellationToken cancellationToken)
    {
        return client.GetThreadStats(cancellationToken);
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
