# @ravendb/mcp

A local, read-only [MCP](https://modelcontextprotocol.io) diagnostics server for RavenDB.
No .NET required — run it with `npx` (Node.js 18+).

## Usage

```jsonc
{
  "command": "npx",
  "args": ["-y", "@ravendb/mcp"],
  "env": { "RAVENDB_URLS": "http://localhost:8080" }
}
```

Or with Claude Code:

```bash
claude mcp add ravendb --scope user --env RAVENDB_URLS=http://localhost:8080 -- npx -y @ravendb/mcp
```

`RAVENDB_URLS` is required — comma-separated node URLs of a single cluster. For secured (HTTPS)
clusters, client certificates, the `--config` file, and the full configuration reference, see
[INSTALL.md](https://github.com/ravendb/ravendb-mcp/blob/main/INSTALL.md).

## Supported platforms

Windows (x64, arm64), macOS (x64, arm64), Linux (x64).
