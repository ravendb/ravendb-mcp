using System.Text.Json;
using ModelContextProtocol.Server;
using RavenDB.Mcp.RavenDB;

namespace RavenDB.Mcp.Tools;

[McpServerToolType]
public static class TaskTools
{
    [McpServerTool(Name = "get_backup_status", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetBackupStatusResult> GetBackupStatus(
        RavenDbAdminClient client,
        string databaseName,
        long taskId,
        CancellationToken cancellationToken)
    {
        return client.GetBackupStatus(databaseName, taskId, cancellationToken);
    }

    [McpServerTool(Name = "get_next_backup_occurrences", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetNextBackupOccurrencesResult> GetNextBackupOccurrences(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetNextBackupOccurrences(databaseName, cancellationToken);
    }

    [McpServerTool(Name = "list_ongoing_tasks", ReadOnly = true, UseStructuredContent = true)]
    public static Task<ListOngoingTasksResult> ListOngoingTasks(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.ListOngoingTasks(databaseName, cancellationToken);
    }

    [McpServerTool(Name = "get_etl_stats", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetEtlStatsResult> GetEtlStats(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetEtlStats(databaseName, cancellationToken);
    }

    [McpServerTool(Name = "get_etl_performance", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetEtlPerformanceResult> GetEtlPerformance(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetEtlPerformance(databaseName, cancellationToken);
    }

    [McpServerTool(Name = "get_etl_debug_stats", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetEtlDebugStatsResult> GetEtlDebugStats(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetEtlDebugStats(databaseName, cancellationToken);
    }
}

public sealed record GetBackupStatusResult(string DatabaseName, long TaskId, JsonElement Status);

public sealed record GetNextBackupOccurrencesResult(string DatabaseName, JsonElement Occurrences);

public sealed record ListOngoingTasksResult(string DatabaseName, JsonElement Tasks);

public sealed record GetEtlStatsResult(string DatabaseName, JsonElement Stats);

public sealed record GetEtlPerformanceResult(string DatabaseName, JsonElement Performance);

public sealed record GetEtlDebugStatsResult(string DatabaseName, JsonElement Stats);
