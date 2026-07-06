using System.Text.Json;
using System.Text.Json.Nodes;
using RavenDB.Mcp.Tools;

namespace RavenDB.Mcp.RavenDB;

public sealed partial class RavenDbAdminClient
{
    // High-level per-environment sizes for every environment. The per-table/per-tree deep report is
    // an order of magnitude larger; get it one environment at a time via inspect_storage.
    public async Task<GetStorageOverviewResult> GetStorageOverview(
        string databaseName,
        CancellationToken cancellationToken)
    {
        return new GetStorageOverviewResult(
            databaseName,
            await GetDatabaseJson(databaseName, "/debug/storage/report", cancellationToken));
    }

    public async Task<GetStorageTreesResult> GetStorageTrees(
        string databaseName,
        CancellationToken cancellationToken)
    {
        return new GetStorageTreesResult(
            databaseName,
            await GetDatabaseJson(databaseName, "/debug/storage/trees", cancellationToken));
    }

    public async Task<GetStorageEnvironmentReportResult> GetStorageEnvironmentReport(
        string databaseName,
        string? environmentName,
        string? environmentType,
        CancellationToken cancellationToken)
    {
        var name = string.IsNullOrWhiteSpace(environmentName) ? databaseName : environmentName;
        var type = string.IsNullOrWhiteSpace(environmentType) ? "Documents" : environmentType;

        return new GetStorageEnvironmentReportResult(
            databaseName,
            name,
            type,
            await GetDatabaseJson(
                databaseName,
                "/debug/storage/environment/report",
                cancellationToken,
                ("name", name),
                ("type", type)));
    }

    public async Task<GetStorageCompressionDictionariesResult> GetStorageCompressionDictionaries(
        string databaseName,
        CancellationToken cancellationToken)
    {
        return new GetStorageCompressionDictionariesResult(
            databaseName,
            await GetDatabaseJson(databaseName, "/debug/storage/compression-dictionaries", cancellationToken));
    }

    public async Task<GetStorageScratchBufferInfoResult> GetStorageScratchBufferInfo(
        string databaseName,
        string? environmentName,
        string? environmentType,
        CancellationToken cancellationToken)
    {
        var name = string.IsNullOrWhiteSpace(environmentName) ? databaseName : environmentName;
        var type = string.IsNullOrWhiteSpace(environmentType) ? "Documents" : environmentType;

        return new GetStorageScratchBufferInfoResult(
            databaseName,
            name,
            type,
            await GetDatabaseJson(
                databaseName,
                "/debug/storage/environment/scratch-buffer-info",
                cancellationToken,
                ("name", name),
                ("type", type)));
    }

    public async Task<GetStorageEnvironmentDetailsResult> GetStorageEnvironmentDetails(
        string databaseName,
        string? environmentName,
        string? environmentType,
        CancellationToken cancellationToken)
    {
        var reportTask = GetStorageEnvironmentReport(databaseName, environmentName, environmentType, cancellationToken);
        var scratchBuffersTask = GetStorageScratchBufferInfo(databaseName, environmentName, environmentType, cancellationToken);
        var freeSpaceTask = GetStorageFreeSpaceSnapshot(databaseName, environmentName, environmentType, cancellationToken);
        await Task.WhenAll(reportTask, scratchBuffersTask, freeSpaceTask);

        var report = await reportTask;
        return new GetStorageEnvironmentDetailsResult(
            databaseName,
            report.EnvironmentName,
            report.EnvironmentType,
            report.Report,
            (await scratchBuffersTask).ScratchBuffers,
            (await freeSpaceTask).FreeSpace);
    }

    public async Task<GetStorageFreeSpaceSnapshotResult> GetStorageFreeSpaceSnapshot(
        string databaseName,
        string? environmentName,
        string? environmentType,
        CancellationToken cancellationToken)
    {
        var name = string.IsNullOrWhiteSpace(environmentName) ? databaseName : environmentName;
        var type = string.IsNullOrWhiteSpace(environmentType) ? "Documents" : environmentType;

        return new GetStorageFreeSpaceSnapshotResult(
            databaseName,
            name,
            type,
            await GetDatabaseJson(
                databaseName,
                "/debug/storage/environment/free-space-snapshot",
                cancellationToken,
                ("name", name),
                ("type", type)));
    }

    public async Task<GetPerformanceOverviewResult> GetPerformanceOverview(CancellationToken cancellationToken)
    {
        return new GetPerformanceOverviewResult(await GetServerJson("/admin/metrics", cancellationToken));
    }

    public async Task<GetCpuStatsResult> GetCpuStats(CancellationToken cancellationToken)
    {
        return new GetCpuStatsResult(await GetServerJson("/admin/debug/cpu/stats", cancellationToken));
    }

    public async Task<GetIoStatsResult> GetIoStats(string? databaseName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(databaseName))
            return new GetIoStatsResult(null, await GetServerJson("/admin/debug/io-metrics", cancellationToken));

        return new GetIoStatsResult(
            databaseName,
            await GetDatabaseJson(databaseName, "/debug/io-metrics", cancellationToken));
    }

    public async Task<GetGcMemoryStatsResult> GetGcMemoryStats(CancellationToken cancellationToken)
    {
        return new GetGcMemoryStatsResult(await GetServerJson("/admin/debug/memory/gc", cancellationToken));
    }

    public async Task<GetOsMemoryStatsResult> GetOsMemoryStats(CancellationToken cancellationToken)
    {
        return new GetOsMemoryStatsResult(await GetServerJson("/admin/debug/memory/stats", cancellationToken));
    }

    public async Task<GetProcessStatsResult> GetProcessStats(CancellationToken cancellationToken)
    {
        return new GetProcessStatsResult(await GetServerJson("/admin/debug/proc/stats", cancellationToken));
    }

    public async Task<GetLowMemoryLogResult> GetLowMemoryLog(CancellationToken cancellationToken)
    {
        return new GetLowMemoryLogResult(await GetServerJson("/admin/debug/memory/low-mem-log", cancellationToken));
    }

    public async Task<GetEncryptionBufferPoolStatsResult> GetEncryptionBufferPoolStats(CancellationToken cancellationToken)
    {
        return new GetEncryptionBufferPoolStatsResult(await GetServerJson("/admin/debug/memory/encryption-buffer-pool", cancellationToken));
    }

    private async Task<DiagnosticTextSampleResult> SampleServerTextFeed(
        string kind,
        string path,
        int seconds,
        CancellationToken cancellationToken)
    {
        var sample = await GetServerTextSample(path, seconds, cancellationToken);
        return new DiagnosticTextSampleResult(
            kind,
            Math.Clamp(seconds, 1, 30),
            sample.Text,
            sample.Truncated,
            sample.Limit);
    }

    public Task<DiagnosticTextSampleResult> SampleGcEvents(int seconds, CancellationToken cancellationToken)
        => SampleServerTextFeed("gc_events", "/admin/debug/memory/gc-events", seconds, cancellationToken);

    public Task<DiagnosticTextSampleResult> SampleAllocations(int seconds, CancellationToken cancellationToken)
        => SampleServerTextFeed("allocations", "/admin/debug/memory/allocations", seconds, cancellationToken);

    public Task<DiagnosticTextSampleResult> SampleThreadContention(int seconds, CancellationToken cancellationToken)
        => SampleServerTextFeed("thread_contention", "/admin/debug/threads/contention", seconds, cancellationToken);

    private const int RunawayTopThreads = 5;

    public async Task<DiagnosticTextSampleResult> SampleThreadRunaway(string? namePrefix, CancellationToken cancellationToken)
    {
        var text = SummarizeRunawayThreads(await GetServerText("/admin/debug/threads/runaway", cancellationToken), namePrefix);
        var sample = TruncateSample(text);
        return new("thread_runaway", 0, sample.Text, sample.Truncated, sample.Limit);
    }

    // The raw snapshot lists every thread with a fat per-thread IoStats block. Default: the hottest few in full
    // plus a compact index of all threads (Id/Name/CpuUsage) so the caller can pick a name prefix; with a prefix:
    // full detail for matching threads. Threads sorted by CPU descending.
    private static string SummarizeRunawayThreads(string json, string? namePrefix)
    {
        JsonNode? root;
        try { root = JsonNode.Parse(json); }
        catch (JsonException) { return json; }

        if (root?["Runaway Threads"] is not JsonObject runaway || runaway["List"] is not JsonArray list)
            return json;

        var sorted = list.OfType<JsonObject>()
            .OrderByDescending(t => t["CpuUsage"]?.GetValue<double>() ?? 0)
            .ToArray();

        var result = new JsonObject();
        if (root["@metadata"] is { } meta) result["@metadata"] = meta.DeepClone();
        foreach (var field in runaway)
            if (field.Key != "List")
                result[field.Key] = field.Value?.DeepClone();

        if (string.IsNullOrWhiteSpace(namePrefix))
        {
            var top = new JsonArray();
            foreach (var t in sorted.Take(RunawayTopThreads))
                top.Add(t.DeepClone());
            var all = new JsonArray();
            foreach (var t in sorted)
                all.Add(new JsonObject
                {
                    ["Id"] = t["Id"]?.DeepClone(),
                    ["Name"] = t["Name"]?.DeepClone(),
                    ["CpuUsage"] = t["CpuUsage"]?.DeepClone(),
                });
            result["TopByCpu"] = top;
            result["AllThreads"] = all;
            result["Hint"] = $"Top {RunawayTopThreads} threads by CPU in full; AllThreads lists every thread compactly. Pass threadNamePrefix for full detail on threads whose name starts with the prefix.";
        }
        else
        {
            var matching = new JsonArray();
            foreach (var t in sorted)
                if ((t["Name"]?.GetValue<string>() ?? "").StartsWith(namePrefix, StringComparison.OrdinalIgnoreCase))
                    matching.Add(t.DeepClone());
            result["MatchingThreads"] = matching;
            result["MatchCount"] = matching.Count;
        }

        return result.ToJsonString();
    }

    public async Task<GetStackTracesResult> GetStackTraces(CancellationToken cancellationToken)
    {
        return new GetStackTracesResult(await GetServerJson("/admin/debug/threads/stack-trace", cancellationToken));
    }

    public async Task<GetScriptRunnersResult> GetScriptRunners(
        string? databaseName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(databaseName))
            return new GetScriptRunnersResult(null, await GetServerJson("/admin/debug/script-runners", cancellationToken));

        return new GetScriptRunnersResult(
            databaseName,
            await GetDatabaseJson(databaseName, "/debug/script-runners", cancellationToken));
    }

    public async Task<GetTcpStatsResult> GetTcpStats(CancellationToken cancellationToken)
    {
        return new GetTcpStatsResult(await GetServerJson("/admin/debug/info/tcp/stats", cancellationToken));
    }

    public async Task<ListTcpConnectionsResult> ListTcpConnections(
        string? databaseName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(databaseName))
            return new ListTcpConnectionsResult(null, await GetServerJson("/admin/debug/info/tcp/active-connections", cancellationToken));

        return new ListTcpConnectionsResult(
            databaseName,
            await GetDatabaseJson(databaseName, "/info/tcp", cancellationToken));
    }
}
