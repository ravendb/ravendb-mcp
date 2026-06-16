namespace RavenDB.Mcp.Tools;

// Selector enums for parameterized facet tools (ADR-0010). The MCP SDK maps each enum to a
// JSON-Schema `enum`, so the agent sees the exact allowed values. One enum per facet tool;
// added alongside the tool that uses it (no unused selectors).

/// <summary>What <c>wait_for_completion</c> blocks on.</summary>
public enum WaitCondition
{
    /// <summary>Wait for a server operation (by operationId) to reach a terminal state.</summary>
    Operation,

    /// <summary>Wait until the database has no stale indexes.</summary>
    Indexing
}
