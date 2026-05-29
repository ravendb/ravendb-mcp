---
name: tool-squash
description: Consolidate RavenDB MCP diagnostic tools without losing troubleshooting intent. Use when reviewing the MCP tool catalog, reducing overlapping tools, grouping leaf diagnostic reads into compound tools, or checking whether a proposed tool split/squash is honest for agents.
---

# Tool Squash

Use this skill to reduce MCP tool count only when consolidation improves agent choice without hiding needed diagnostics.

## Workflow

1. Inventory exposed tools with `rg -n "McpServerTool\\(Name =" src/RavenDB.Mcp/Tools`.
2. Group tools by the diagnostic question an operator would ask, not by endpoint shape.
3. Mark a squash candidate only when one compound name can honestly describe all collected data.
4. Keep drill-down tools separate when they need extra parameters, can return large payloads, are sampled over time, or are useful after a summary points at a specific object.
5. Define the compound response fields before editing code.
6. Keep leaf methods single-purpose in `RavenDbAdminClient`; remove only the eaten MCP registrations.
7. Make the compound method body show exactly what it collects through direct leaf method calls.
8. Update tool-name tests, call tests, and any local planning notes.
9. Run build/tests that fit the change, then report tool-count delta and remaining candidates.

## Squash Rules

- Prefer full catalog until a concrete overlap is clear.
- Squash for one diagnostic question, not for an arbitrary smaller count.
- Do not merge a compact overview with a parameterized detail lookup.
- Do not merge long-running, streaming, or artifact-like diagnostics into ordinary overview tools.
- Do not invent policy or reshape RavenDB data unless the tool contract needs a stable summary.
- Keep public tool names `snake_case`, read-only annotations, and structured outputs.

## Repo Pattern

Compound tools should look like this:

```csharp
var first = await GetFirstThing(databaseName, cancellationToken);
var second = await GetSecondThing(databaseName, cancellationToken);

return new GetOverviewResult(databaseName, first.Value, second.Value);
```

This keeps the public MCP catalog navigable while preserving a readable implementation shape for future split/squash decisions.
