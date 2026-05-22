# RavenDB MCP Diagnostics Server Planning Notes

## Research Areas

| Area | Core Question | Why It Matters | Output |
|---|---|---|---|
| Deployment model | External local MCP, RavenDB-hosted MCP, or both? | Slack leans external for cert simplicity; GitHub issue asks for RavenDB/Aspire-hosted discovery. | Architecture decision with supported modes. |
| Auth model | What cert level is enough for diagnostics? | `Cluster Admin` is scary; `Operator` can already gather debug info, logs, traffic watch, etc. | Proposed "diagnostics certificate" or minimum supported clearance. |
| Safety model | How do we classify read/write/dangerous/sensitive tools? | MCP annotations are hints, not enforcement. App-level gates are required. | Tool risk taxonomy and config flags. |
| API coverage | Which features use `RavenDB.Client` vs raw REST/debug endpoints? | Official implementation should prefer client APIs, but debug endpoints expose unique diagnostics. | Endpoint/API mapping matrix. |
| Dev/test integration | How should Aspire, TestDriver, and TestContainers expose MCP? | GitHub issue specifically wants trivial test debugging with real instance data. | Integration design and sample configs. |
| Tool granularity | Many tiny tools or bundled diagnostic workflows? | Too many calls increase model cost and make diagnostics noisy. | Tool catalog with "summary first, expand later" pattern. |
| Data exposure | How much real document data can the agent inspect? | The issue asks AI to query real data; this has privacy/security implications. | Redaction, limits, allowlists, and query policy. |
| Artifact handling | How should debug packages, dumps, logs, and traces be returned? | Large/sensitive outputs should not be dumped into context. | Resource-link/file-output strategy. |
| Compatibility | RavenDB 7.2 only, or version-adaptive? | Tool availability may vary by server version and license. | Capability detection plan. |
| Prior prototype review | What can be learned from the unofficial attempt? | Avoid repeating mistakes; reuse naming/workflow ideas if useful. | Notes on useful patterns and rejected choices. |

## Implementation Plan

| Phase | Goal | Scope | Output |
|---|---|---|---|
| 0. Docs baseline | Capture decisions before code drifts | PRD + ADR | `PRD: RavenDB MCP Diagnostics Server`, `ADR: Local external C# MCP server` |
| 1. Hello World MCP | Prove MCP server runs locally beside the agent | C# console app, stdio MCP transport, one test tool | `ping` or `get_server_version` tool |
| 2. RavenDB connection | Prove RavenDB auth/config path | Configure RavenDB URL(s), cert path/password, default database | initialized `DocumentStore` |
| 3. First real read tool | Prove end-to-end RavenDB access | `list_databases` using RavenDB Client API | agent can fetch database names |
| 4. Read-only diagnostics skeleton | Establish patterns | tool naming, structured outputs, errors, logging, config validation | stable pattern for adding tools |
| 5. Prior prototype review | Mine useful ideas without inheriting architecture | inspect unofficial implementation, note useful tool names/workflows/mistakes | short review note |
| 6. Feature/API coverage planning | Decide capability list | tiny tools vs bundled workflows, diagnostics scope, data exposure | prioritized feature catalog |
| 7. Read-only diagnostics expansion | Build first real MVP | stats, topology, index errors/stats, running queries/operations, basic health | usable diagnostics MCP |
| 8. Aspire/TestDriver/TestContainers | Improve dev/test ergonomics after MCP works | Aspire discovery, Docker examples, test fixture helpers | integration stories |
| 9. Writes/deletes later | Separate safety discussion | write/delete modes, dangerous tool gates, new cert tier | future ADR + implementation |

## Updated Priority Map

| Priority | Requirement | Status/Timing |
|---|---|---|
| P0 | External local MCP server running where the user's AI agent runs | Decided |
| P0 | C# implementation | Decided, capture in ADR |
| P0 | RavenDB 7.2 first | Decided |
| P0 | Docker-first/local usage against a RavenDB URL | Decided |
| P0 | RavenDB-generated certificate auth | Decided |
| P0 | Use `Operator` certificate for now | Decided |
| P0 | Leave room for future MCP/diagnostics cert tier | ADR note, later product work |
| P0 | Read-only tools only for first version | Decided |
| P0 | First real tool: fetch database names | MVP milestone |
| P0 | Use RavenDB Client API where reasonable | Implementation default |
| P0 | Structured tool outputs and clean errors | Build from phase 3 onward |
| P1 | Prior prototype review | I can do this independently once I have the branch/repo/source |
| P1 | Feature/API coverage list | Later, after hello-world works |
| P1 | Tool granularity decision: tiny tools plus bundled workflows | Discuss with feature list |
| P1 | Data exposure policy | Later, during feature list |
| P1 | Artifact handling policy | Later, when diagnostics artifacts enter scope |
| P2 | Aspire integration | Later, after working MCP |
| P2 | TestDriver/TestContainers integration | Later, after working MCP |
| P2 | Writes/deletes | Later |
| P3 | Multi-version support beyond 7.2 | Later, only if APIs differ materially |

## Suggested First ADR

Title: `ADR-0001: Build RavenDB MCP as a local external C# server`

Decisions:

- The MCP server runs locally beside the AI agent.
- It connects to RavenDB via configured URL(s), usually Docker/local.
- It is implemented in C#.
- It targets RavenDB 7.2 first.
- It authenticates using RavenDB-generated client certificates.
- Initial certificate level is `Operator`.
- Initial tool surface is read-only.
- Writes/deletes are explicitly out of scope for the first implementation.

## Suggested First PRD

Title: `PRD: RavenDB MCP Diagnostics Server`

Keep it to:

- Problem
- Goals
- Non-goals
- User workflow
- MVP scope
- Configuration
- Safety model
- Milestones
- Open questions
