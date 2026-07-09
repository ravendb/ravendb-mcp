# @ravendb/mcp

A local, **read-only** [MCP](https://modelcontextprotocol.io) diagnostics server for RavenDB,
distributed for `npx`.

This package is a thin launcher: it resolves the self-contained native binary for your platform
(shipped as a platform-specific optional dependency) and runs it over stdio. **No .NET runtime is
required** — the binary bundles it. Node.js 18+ is the only prerequisite.

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
[INSTALL.md](https://github.com/poissoncorp/ravendb-mcp/blob/main/INSTALL.md).

## Supported platforms

Windows (x64, arm64), macOS (x64, arm64), Linux (x64).
