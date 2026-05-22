# Beta Implementation Checklist

Source of truth: [PRD](prd-ravendb-mcp-diagnostics-server.md) and [ADRs](adr/). Older planning notes may be stale.

Beta should expand the read-only diagnostics surface without changing the basic shape of the project: C#, local external MCP server, stdio transport, RavenDB Client API where reasonable, certificate auth, and no registered write/delete tools.

## Current Wire-State Findings

- [ ] Decide whether SDK-advertised `logging`, `tools.listChanged`, and task-optional execution are acceptable defaults or should be suppressed/configured before Beta.
- [ ] Fix `get_database_record` output shape. Current direct serialization exposes only `etag` and `isSharded` inside `record`, which is not enough to be a useful database-record tool.
- [ ] Keep `tools/list` metadata lean: snake_case names, read-only annotations, structured output schemas, and descriptions only when a name is genuinely unclear.

## Beta Foundations

- [ ] Keep all Beta tools read-only by construction.
- [ ] Keep tool outputs domain-shaped. Prefer RavenDB Client objects or small records over generic JSON wrappers.
- [ ] Establish a simple output convention for common fields: database name, node tag, etag, status, errors, timestamps, and durations.
- [ ] Establish clear permission/configuration failure behavior for missing database, inaccessible database, invalid certificate, and insufficient certificate clearance.
- [ ] Add MCP tool-call logging through normal server logs, not stdout.
- [ ] Log cancellation reasons when a client cancels an in-flight request.
- [ ] Pass `CancellationToken` through every RavenDB call that supports it.
- [ ] Add timeouts only where there is a concrete long-running read path; do not add global timeout machinery prematurely.

## Data And Artifact Policies

- [ ] Write the Beta data exposure policy: document fields, query results, metadata, redaction, and limits.
- [ ] Write the Beta artifact handling policy: logs, debug packages, dumps, traces, and other large outputs.
- [ ] Mark large or sensitive diagnostics as Stable unless the policy is already clear enough to implement safely.

## Beta Diagnostics Surface

- [ ] Get cluster topology.
- [ ] Get node info and node status.
- [ ] Get server-wide metrics.
- [ ] Get database stats.
- [ ] Get detailed database stats.
- [ ] Get collection stats.
- [ ] Get database configuration.
- [ ] Get server configuration.
- [ ] Get client configuration.
- [ ] Get Studio configuration.
- [ ] Get logs configuration.
- [ ] List indexes.
- [ ] Get index stats.
- [ ] Get index errors.
- [ ] Get index performance.
- [ ] Get index progress.
- [ ] Get index staleness.
- [ ] Get suggested index merges.
- [ ] List running operations.
- [ ] Get operation state.
- [ ] List running queries.
- [ ] Get query cache info.
- [ ] Get replication active connections.
- [ ] Get replication conflicts.
- [ ] Get replication performance.
- [ ] Get outgoing replication failures.
- [ ] Get incoming replication rejection info.
- [ ] Get backup status.
- [ ] Get next backup occurrence.
- [ ] List ongoing tasks.
- [ ] Get ETL stats.
- [ ] Get ETL performance.
- [ ] Get ETL debug stats.
- [ ] Get subscriptions.
- [ ] Get subscription connection details.
- [ ] Get notification center alerts.
- [ ] Get database TCP info.

## Tests

- [ ] Add integration coverage for each new RavenDB diagnostic category, not every tiny field.
- [ ] Keep E2E coverage small: tool discovery plus representative tool calls from the main categories.
- [ ] Keep GitHub Actions running RavenDB and the MCP test suite.
- [ ] Add secured RavenDB coverage only when certificate setup is part of the implementation, not before.

## Out Of Beta

- [ ] No writes/deletes.
- [ ] No broad admin surface.
- [ ] No Streamable HTTP unless ADR-0003 is updated.
- [ ] No Aspire, TestDriver, or TestContainers integration.
- [ ] No MCP Tasks extension.
- [ ] No Stable-sensitive diagnostics such as logs, traffic watch, dumps, thread data, memory stats, or info packages until their handling policy is ready.
