# RavenDB MCP

A local, read-only [MCP](https://modelcontextprotocol.io) diagnostics server for RavenDB. It runs beside an AI agent over stdio, connects to one RavenDB cluster, and exposes **21 read-only tools** so the agent can inspect cluster, database, index, task, storage, and performance state — plus logs, support packages, and read-only data — without hand-writing client code or hitting the REST API.

## Quick start

Run it with `npx` (needs only Node.js — the launcher fetches the self-contained binary, so no .NET is required) and register it with Claude Code:

```powershell
claude mcp add ravendb --scope user --env RAVENDB_URLS=http://localhost:8080 -- npx -y @ravendb/mcp
```

Then ask the agent *“list my RavenDB databases.”*

On the .NET stack, `dnx RavenDB.Mcp` works too; for no runtime at all, grab a prebuilt binary from the [Releases](https://github.com/ravendb/ravendb-mcp/releases) page. Other OSes, secured (HTTPS + certificate) clusters, VS Code / Claude Desktop wiring, the full configuration reference, and troubleshooting are in **[INSTALL.md](INSTALL.md)**.

Other OSes, secured (HTTPS + certificate) clusters, VS Code / Claude Desktop wiring, the full configuration reference, and troubleshooting are in **[INSTALL.md](INSTALL.md)**.

## Configuration

One server instance targets **one cluster**. The only required setting is the URL; pass it (and the optional certificate / artifacts settings) as environment variables or a `--config` JSON file:

| Variable | Required | Purpose |
| --- | --- | --- |
| `RAVENDB_URLS` | yes | Cluster node URL(s), comma-separated — the client fails over across them. |
| `RAVENDB_CERTIFICATE_PATH` / `_PASSWORD` | no | Client `.pfx` for secured (HTTPS) clusters. Omit for unsecured. |
| `RAVENDB_ARTIFACTS_PATH` | no | Where exported files are written (defaults to a temp folder). |

To work with several clusters, register the server more than once under different names — no reinstall. Details in [INSTALL.md](INSTALL.md#configuration-reference).

## Tools

**21 read-only tools**, `snake_case`. Most are *facet* tools — one tool per subject that takes enum/array selectors (the agent sees the allowed values as a JSON-Schema `enum`) and returns only the requested sections, so a whole area is covered without bloating `tools/list`.

- **Facets:** `get_cluster_overview`, `get_server_config`, `get_server_resources`, `get_network_details`, `get_database_stats`, `get_database_config`, `get_index`, `get_tasks`, `get_live_workload`, `inspect_storage`, `get_document_data`, `sample_live_feed`, `wait_for_completion`, `collect_debug_package`, `get_ai_agents`
- **Singletons:** `list_databases`, `get_database_record`, `get_notifications`, `run_query`, `list_compare_exchange`, `export_server_logs`

Tools that produce large or binary output (logs, debug packages) write a file and return a reference — `{ path, contentType, bytes }` — instead of flooding the context.

## Safety

Read-only by design: there are no write or delete tools, and tools carry MCP read-only annotations. Connection-string secrets (passwords, API keys, cloud credentials, SAS tokens) are masked as `***redacted***` at the database-record boundary, so they never leak through `get_database_record` or any tool projecting it. What the agent can see is ultimately bounded by the configured certificate's RavenDB permissions — an **Operator** certificate is the recommended clearance.

## Build & test

```powershell
dotnet build RavenDB.Mcp.slnx -c Release
./scripts/start-ravendb-test-container.ps1 -Port 8070 -Name ravendb-mcp-test   # local RavenDB for tests
$env:RAVENDB_TEST_URL = "http://127.0.0.1:8070/"
dotnet test RavenDB.Mcp.slnx -c Release
```

CI runs the suite against both unsecured and secured (certificate) RavenDB. Release artifacts — self-contained executables, the NuGet `McpServer` package, and the npm packages (`@ravendb/mcp`) — are produced by the manually-dispatched workflows in `.github/workflows/` (build & GitHub release, then NuGet, npm, and MCP Registry); the underlying build commands are in [INSTALL.md](INSTALL.md).
