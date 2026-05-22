# PRD: RavenDB MCP Diagnostics Server

Status: Steady

## Summary

Build a local, external MCP server that lets an AI agent perform controlled read-only diagnostics against an accessible RavenDB instance or cluster.

The first milestone is intentionally small: prove the MCP server can connect to RavenDB and expose real read-only data. After that works, expand into diagnostic workflows, production hardening, and broader version/integration support.

## Scope

| Version | In | Out |
|---|---|---|
| Alpha: Hello-world diagnostics | Local external MCP server; RavenDB 7.2; read-only tools; `list_databases`; get database record; structured outputs and clean errors | Hosted RavenDB MCP; writes/deletes; full diagnostics coverage; Aspire/TestDriver/TestContainers; multi-version support |
| Beta: Diagnostics MVP | Read-only diagnostics expansion; tool naming and output conventions; data exposure policy; artifact handling policy; logs for MCP tool calls; timeouts, pagination, limits, cancellation; clear permission errors | Writes/deletes by default; broad admin surface |
| Stable: Release-ready diagnostics | Documentation, packaging, configuration polish, safety validation, all security modes working, controlled sensitive diagnostics | Broad admin surface; destructive operations without explicit safety design |
| V2: Broader support | Aspire integration; TestDriver/TestContainers integration; 6.2+ support; RavenDB MCP/diagnostics certificate tier; possible controlled write/delete modes; bundled workflow tools to reduce roundtrips and token usage | Destructive operations without explicit safety design |

## Primary Workflow

1. User has access to a RavenDB instance or cluster.
2. User runs the MCP server.
3. User configures the MCP server with RavenDB URL(s) and, when required, certificate details.
4. User connects their AI agent to the MCP server.
5. AI agent calls read-only diagnostic tools against RavenDB.
6. First milestone: agent fetches database names.

## Requirements

| Target | Requirement |
|---|---|
| Alpha | External local MCP server running where the user's AI agent can connect to it |
| Alpha | RavenDB 7.2 first |
| Alpha | Connects to configured RavenDB URL(s) |
| Alpha | Read-only tools only |
| Alpha | Structured tool outputs and clean errors |
| Alpha | Clear auth/config/permission errors |
| Beta | Data exposure policy (document fields, query results, metadata, redaction, limits) |
| Beta | Artifact handling policy (logs, debug packages, dumps, traces, large outputs) |
| Beta | Read-only diagnostics expansion: stats, topology, index errors/stats, running queries/operations, basic health |
| Beta | Logs for MCP tool calls |
| Beta | Logs cancellation reasons when clients cancel in-flight requests |
| Beta | Timeouts and cancellation-token propagation |
| Stable | Documentation, packaging, configuration polish, and safety validation |
| Stable | Works against Unsecured, Secured, and Let's Encrypt RavenDB modes |
| Stable | Tool-result limits and pagination/cursors for tools that can return variable-size collections or large payloads |
| Stable | Controlled sensitive diagnostics, such as logs, traffic watch, memory stats, thread data, and info packages |
| V2 | Aspire integration |
| V2 | TestDriver/TestContainers integration |
| V2 | Writes/deletes |
| V2 | Bundled workflows via single tool to optimize token usage by reducing roundtrips, such as diagnosing cluster, database, indexes, replication, ETL, and collecting a support snapshot |
| V2 | RavenDB 6.2+ support, not only 7.2 |
| V2 | RavenDB MCP/diagnostics certificate tier |
| V2 | MCP `logging` capability for progress and setup diagnostics |
| V2 | Evaluate MCP Tasks extension for long-running workflows such as waiting for indexing, waiting for RavenDB operations, and collecting support snapshots |

## Safety

Alpha is read-only. Write/delete tools are out of scope for Alpha. Destructive operations such as database deletion must not be exposed in Alpha.

MCP tool annotations may be used, but app-level behavior is the source of truth. The server should not rely on the AI agent or MCP client to enforce RavenDB safety rules.

## Done When

- MCP server can be launched and connected to by an AI-agent host.
- It can connect to RavenDB 7.2 using configured URL(s) and, when required, certificate details.
- It exposes `list_databases`.
- It exposes a database-record read tool.
- Tools return structured output.
- Auth, configuration, and permission errors are understandable.
- ADR-0001 captures the foundation decisions.

## Feature/API Coverage Appendix

| Target | Capability |
|---|---|
| Alpha | List databases |
| Alpha | Get database record |
| Alpha | Get server version/build info |
| Alpha | Get current certificate/user identity, where supported |
| Beta | Get cluster topology |
| Beta | Get node info and node status |
| Beta | Get server-wide metrics |
| Beta | Get database stats |
| Beta | Get detailed database stats |
| Beta | Get collection stats |
| Beta | Get database configuration |
| Beta | Get server configuration |
| Beta | Get client configuration |
| Beta | Get Studio configuration |
| Beta | Get logs configuration |
| Beta | List indexes |
| Beta | Get index stats |
| Beta | Get index errors |
| Beta | Get index performance |
| Beta | Get index progress |
| Beta | Get index staleness |
| Beta | Get suggested index merges |
| Beta | List running operations |
| Beta | Get operation state |
| Beta | List running queries |
| Beta | Get query cache info |
| Beta | Get replication active connections |
| Beta | Get replication conflicts |
| Beta | Get replication performance |
| Beta | Get outgoing replication failures |
| Beta | Get incoming replication rejection info |
| Beta | Get backup status |
| Beta | Get next backup occurrence |
| Beta | List ongoing tasks |
| Beta | Get ETL stats |
| Beta | Get ETL performance |
| Beta | Get ETL debug stats |
| Beta | Get subscriptions |
| Beta | Get subscription connection details |
| Beta | Get notification center alerts |
| Beta | Get database TCP info |
| Stable | Get logs |
| Stable | Search logs |
| Stable | Get admin logs for a given period |
| Stable | Get audit logs |
| Stable | Get traffic watch for a given period |
| Stable | Get memory stats |
| Stable | Get low-memory log |
| Stable | Get CPU stats |
| Stable | Get runaway threads |
| Stable | Get thread/stack trace diagnostics |
| Stable | Get script runners |
| Stable | Get storage report |
| Stable | Get all storage environments report |
| Stable | Get IO metrics |
| Stable | Get performance metrics |
| Stable | Get huge documents report |
| Stable | Get identities |
| Stable | Get server info package |
| Stable | Get cluster info package |
| Stable | Get database info package |
| V2 | Wait for indexing |
| V2 | Wait for RavenDB operation |
| V2 | Controlled document writes |
| V2 | Controlled document deletes |
| V2 | Controlled operation cancellation |
| V2 | Controlled index enable/disable |

## Open Questions

- What exact diagnostics belong in the first production-ready read-only feature set?
- What data exposure limits are acceptable when agents inspect real documents?
- How should large diagnostics artifacts be represented outside the model context?
- What should a later RavenDB MCP/diagnostics certificate tier allow?
