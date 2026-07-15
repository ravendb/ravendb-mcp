# RavenDB MCP

A local, read-only [MCP](https://modelcontextprotocol.io) diagnostics server for RavenDB. Point it at a cluster and your AI agent can inspect cluster, database, index, task, storage, and performance state, plus logs, support packages, and your data, across **21 read-only tools**.

## Quick start

Launch it with **npx** (Node.js), **dnx** (.NET SDK), or a prebuilt binary from [Releases](https://github.com/ravendb/ravendb-mcp/releases), then add it to your client. Set `RAVENDB_URLS` to your cluster; the examples use `http://localhost:8080`.

**Claude Code**

```powershell
claude mcp add ravendb --scope user --env RAVENDB_URLS=http://localhost:8080 -- npx -y @ravendb/mcp
```

Or install the plugin, which prompts for your cluster URL:

```
/plugin marketplace add ravendb/ravendb-mcp
/plugin install ravendb-mcp@ravendb
```

**Codex**

Add to `~/.codex/config.toml`:

```toml
[mcp_servers.ravendb]
command = "npx"
args = ["-y", "@ravendb/mcp"]
env = { RAVENDB_URLS = "http://localhost:8080" }
```

**Cursor**

Click the button (installs with `http://localhost:8080`; edit for another cluster), or add `.cursor/mcp.json`:

[![Add to Cursor](https://cursor.com/deeplink/mcp-install-dark.svg)](https://cursor.com/en/install-mcp?name=ravendb&config=eyJjb21tYW5kIjoibnB4IiwiYXJncyI6WyIteSIsIkByYXZlbmRiL21jcCJdLCJlbnYiOnsiUkFWRU5EQl9VUkxTIjoiaHR0cDovL2xvY2FsaG9zdDo4MDgwIn19)

```json
{
  "mcpServers": {
    "ravendb": {
      "command": "npx",
      "args": ["-y", "@ravendb/mcp"],
      "env": { "RAVENDB_URLS": "http://localhost:8080" }
    }
  }
}
```

Then ask the agent *“list my RavenDB databases.”* Any of these can use `dnx RavenDB.Mcp --yes` or a binary path in place of `npx -y @ravendb/mcp`. Secured (HTTPS) clusters, other OSes, and the full configuration reference are in **[INSTALL.md](INSTALL.md)**.

## Features

**Developer experience**
- One-line install with `npx`, `dnx`, or a prebuilt binary.
- 21 tools, one per subject area, each with parameters to select what you need (reduced from 74).
- Version-aware RQL docs are served as `rql://` resources; a failed query returns the parser error and where to look.

**Safety**
- Read-only: no write, patch, or delete tools, and mutating queries are rejected.
- Connection-string secrets (passwords, cloud keys, SAS tokens, AI-provider keys, certificates) are masked in the returned JSON.
- Access is scoped to the client certificate's RavenDB permissions.

**Context-window friendly**
- Progressive disclosure: tools return an overview by default and drill into detail on request.
- Results are paged: `run_query` takes `start` and `pageSize` (up to 128 rows), and the list tools take `pageSize`.
- Log exports and debug packages are written to an artifact file on disk; the tool returns its path and byte size, not the contents.
- Query results omit per-document metadata unless you request it.

**Runs anywhere**
- Distributed via npm, NuGet, GitHub Releases, and the MCP Registry.
- Self-contained binaries for Windows, macOS, and Linux (x64 and arm64), with no runtime to install.
- Run a separate instance per cluster for multi-cluster setups.

## Configuration

One server instance targets **one cluster**. The only required setting is the URL; pass it (and the optional certificate / artifacts settings) as environment variables or a `--config` JSON file:

| Variable | Required | Purpose |
| --- | --- | --- |
| `RAVENDB_URLS` | yes | Cluster node URL(s), comma-separated, the client fails over across them. |
| `RAVENDB_CERTIFICATE_PATH` / `_PASSWORD` | no | Client `.pfx` for secured (HTTPS) clusters. Omit for unsecured. |
| `RAVENDB_ARTIFACTS_PATH` | no | Where exported files are written (defaults to a temp folder). |

Full configuration reference, including secured clusters and multi-cluster setups, is in [INSTALL.md](INSTALL.md#configuration-reference).

## Tools

**21 read-only tools** (`snake_case`). Most are *facet* tools: one per subject that takes selectors and returns only the sections you ask for.

- **Facets:** `get_cluster_overview`, `get_server_config`, `get_server_resources`, `get_network_details`, `get_database_stats`, `get_database_config`, `get_index`, `get_tasks`, `get_live_workload`, `inspect_storage`, `get_document_data`, `sample_live_feed`, `wait_for_completion`, `collect_debug_package`, `get_ai_agents`
- **Singletons:** `list_databases`, `get_database_record`, `get_notifications`, `run_query`, `list_compare_exchange`, `export_server_logs`

Large or binary output (logs, debug packages) is written to a file and returned as a reference.

## License

MIT. See [LICENSE](LICENSE). Building from source is covered in [INSTALL.md](INSTALL.md).

---

mcp-name: io.github.ravendb/ravendb-mcp
