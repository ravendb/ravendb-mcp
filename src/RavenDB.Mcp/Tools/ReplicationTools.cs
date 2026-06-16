using System.Text.Json;

namespace RavenDB.Mcp.Tools;

// Replication result records. The replication reads are surfaced through get_tasks
// (taskType=Replication, includeDiagnostics) and the GetReplicationTasksDetails client method.

public sealed record GetReplicationTasksResult(string DatabaseName, JsonElement Tasks);

public sealed record GetReplicationPerformanceResult(string DatabaseName, JsonElement Performance);

public sealed record GetReplicationTasksDetailsResult(
    string DatabaseName,
    JsonElement Tasks,
    JsonElement Performance,
    JsonElement ActiveConnections,
    JsonElement Conflicts,
    JsonElement OutgoingFailures,
    JsonElement IncomingLastActivity,
    JsonElement IncomingRejections,
    JsonElement OutgoingReconnectQueue,
    JsonElement Progress,
    JsonElement InternalOutgoingProgress);
