# RavenDB MCP

A local, read-only [MCP](https://modelcontextprotocol.io) diagnostics server for RavenDB. Point it at a cluster and your AI agent can inspect cluster, database, index, task, storage, and performance state, plus logs, support packages, and your data, across **21 read-only tools**.

## Quick start

Run it with **npx**:

```powershell
claude mcp add ravendb --scope user --env RAVENDB_URLS=http://localhost:8080 -- npx -y @ravendb/mcp
```

Or with **dnx**:

```powershell
claude mcp add ravendb --scope user --env RAVENDB_URLS=http://localhost:8080 -- dnx RavenDB.Mcp --yes
```

Or download a prebuilt executable from the [Releases](https://github.com/ravendb/ravendb-mcp/releases) page.

Then ask the agent *“list my RavenDB databases.”* Other OSes, secured (HTTPS + certificate) clusters, VS Code / Claude Desktop wiring, and the full configuration reference are in **[INSTALL.md](INSTALL.md)**.

## Configuration

One server instance targets **one cluster**. The only required setting is the URL; pass it (and the optional certificate / artifacts settings) as environment variables or a `--config` JSON file:

| Variable | Required | Purpose |
| --- | --- | --- |
| `RAVENDB_URLS` | yes | Cluster node URL(s), comma-separated, the client fails over across them. |
| `RAVENDB_CERTIFICATE_PATH` / `_PASSWORD` | no | Client `.pfx` for secured (HTTPS) clusters. Omit for unsecured. |
| `RAVENDB_ARTIFACTS_PATH` | no | Where exported files are written (defaults to a temp folder). |

To work with several clusters, register the server more than once under different names; no reinstall. Details in [INSTALL.md](INSTALL.md#configuration-reference).

## Tools

**21 read-only tools** (`snake_case`). Most are *facet* tools: one per subject that takes selectors and returns only the sections you ask for.

- **Facets:** `get_cluster_overview`, `get_server_config`, `get_server_resources`, `get_network_details`, `get_database_stats`, `get_database_config`, `get_index`, `get_tasks`, `get_live_workload`, `inspect_storage`, `get_document_data`, `sample_live_feed`, `wait_for_completion`, `collect_debug_package`, `get_ai_agents`
- **Singletons:** `list_databases`, `get_database_record`, `get_notifications`, `run_query`, `list_compare_exchange`, `export_server_logs`

Large or binary output (logs, debug packages) is written to a file and returned as a reference. The server also publishes version-aware **`rql://` query-documentation resources**, so the agent writes correct RQL for your server version.

## Safety

Read-only by design: no write or delete tools. Connection-string secrets (passwords, API keys, cloud credentials, certificates) are masked as `***redacted***`, and what the agent can see is bounded by the certificate's RavenDB permissions. An **Operator** certificate is recommended.

## License

MIT. See [LICENSE](LICENSE). Building from source is covered in [INSTALL.md](INSTALL.md).
