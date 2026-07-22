# RavenDB MCP: configuration and advanced setup

Install and one-click client setup are in the [README](README.md). This covers the rest:
configuration, secured clusters, GUI clients, building from source, and troubleshooting.

- [Prerequisites](#prerequisites)
- [Configuration reference](#configuration-reference)
- [GUI clients](#gui-clients)
- [Build from source](#build-from-source)
- [Verify the connection](#verify-the-connection)
- [Updating and removing](#updating-and-removing)
- [Troubleshooting](#troubleshooting)
- [FAQ](#faq)

## Prerequisites

Only what your chosen launch command needs:

- **npx**: Node.js 18+.
- **dnx** or a global tool: the .NET 10 SDK.
- **Prebuilt binary**: nothing; the runtime is bundled.
- **Building from source**: the .NET 10 SDK and git.

Plus network access to your RavenDB cluster, and a client `.pfx` certificate only for secured
(HTTPS) clusters. Docker is not required to run the server (only for the test fixtures).

## Configuration reference

The server targets one RavenDB cluster. The only required setting is the URL(s); everything else is
optional.

### Environment variables

| Variable | Required | Maps to | Notes |
| --- | --- | --- | --- |
| `RAVENDB_URLS` | yes | `Urls` | One or more node URLs of a single cluster, comma or semicolon separated. |
| `RAVENDB_CERTIFICATE_PATH` | no | `CertificatePath` | Path to a client `.pfx` for secured clusters. Omit for unsecured. |
| `RAVENDB_CERTIFICATE_PASSWORD` | no | `CertificatePassword` | Password for the `.pfx` (secret). Omit if none. |
| `RAVENDB_ARTIFACTS_PATH` | no | `ArtifactsPath` | Where exported files are written. See [Artifacts path](#artifacts-path). |

### Config file (`--config`)

Pass `--config <path>` as a server argument to load a flat JSON file instead of, or on top of, env
vars:

```json
{
  "Urls": ["https://node-a.example.development.run:443", "https://node-b.example.development.run:443"],
  "CertificatePath": "C:\\certs\\client.certificate.pfx",
  "CertificatePassword": "secret",
  "ArtifactsPath": "D:\\ravendb-mcp-artifacts"
}
```

Everything except `Urls` is optional. JSON requires escaping Windows backslashes as `\\`.

### Precedence

Environment variables apply first; a `--config` file overrides them. If no URL is set by either, the
server exits at startup with `At least one RavenDB URL must be configured.`

### Secured clusters and certificates

Secured RavenDB uses HTTPS and authenticates clients with a `.pfx` certificate:

```powershell
claude mcp add ravendb --scope user --env RAVENDB_URLS=https://node-a.example.development.run:443 --env RAVENDB_CERTIFICATE_PATH="C:\certs\client.certificate.pfx" --env RAVENDB_CERTIFICATE_PASSWORD=pfxPasswordIfAny -- npx -y @ravendb/mcp
```

- Prefer a least-privilege certificate. A per-database **Read** certificate covers the database,
  index, query, and document tools; **Operator** clearance is only needed for the server-wide and
  cluster-wide tools. The certificate's permissions bound what the agent can see.
- Drop the password variable if the `.pfx` has none. When set, `RAVENDB_CERTIFICATE_PASSWORD` is used
  only in-process to open the `.pfx` — never logged or written to disk. Mark it as a secret in your
  client config (in `server.json` it is declared `isSecret`).
- For unsecured (HTTP) clusters, leave both certificate settings unset. The server prints a startup
  warning on an `http://` URL, since traffic is then unencrypted.

Generate a client certificate in RavenDB Studio under **Manage Server > Certificates**.

### One cluster, multiple nodes

List all of the cluster's node URLs in `RAVENDB_URLS`. The typed client fails over across them; raw
diagnostic routes target the first URL. To inspect a different cluster, register a second server
entry with its own name and URLs.

### Artifacts path

Some tools (log exports, debug packages, large reports) write a file to disk and return a small
reference instead of flooding the context:

```json
{ "path": "...\\ravendb-mcp-artifacts\\...", "contentType": "application/octet-stream", "bytes": 12345 }
```

Without `RAVENDB_ARTIFACTS_PATH`, files go to a `ravendb-mcp-artifacts` folder in the system temp
directory (`%TEMP%` on Windows, `$TMPDIR` or `/tmp` on macOS/Linux). Because log exports and debug
packages are written unredacted and can contain secrets, this default folder is locked to your user
(mode `0700` on Linux/macOS) and the server expires its own exports there after 24 hours. Set
`RAVENDB_ARTIFACTS_PATH` for a persistent location you manage yourself — a folder you set is used
as-is, with no permission changes and no expiry, so its retention and cleanup are up to you.

## GUI clients

Editors that use a config file rather than a CLI. Set the launch command to `npx -y @ravendb/mcp`,
`dnx RavenDB.Mcp --yes`, or a binary path.

### Claude Desktop

Edit `%APPDATA%\Claude\claude_desktop_config.json` (Windows) or
`~/Library/Application Support/Claude/claude_desktop_config.json` (macOS), then restart:

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

### VS Code / Visual Studio

Create `.vscode/mcp.json`:

```json
{
  "servers": {
    "ravendb": {
      "type": "stdio",
      "command": "npx",
      "args": ["-y", "@ravendb/mcp"],
      "env": { "RAVENDB_URLS": "http://localhost:8080" }
    }
  }
}
```

## Build from source

For developing the server itself. Publish a self-contained binary:

```powershell
git clone https://github.com/ravendb/ravendb-mcp.git
cd ravendb-mcp
dotnet publish src/RavenDB.Mcp/RavenDB.Mcp.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o dist/win-x64
```

Pick the runtime identifier for the target: `win-x64`, `win-arm64`, `osx-arm64`, `osx-x64`, or
`linux-x64`. The output binary (`dist/<rid>/ravendb-mcp[.exe]`) is the only file needed at run time;
point your client at it.

To iterate during development, `dotnet run --project src/RavenDB.Mcp -- --config <path>`. Build first
(`dotnet build -c Release`) if you wire `dotnet run` into a client, since MSBuild output on stdout can
corrupt the JSON-RPC handshake; prefer the published binary there.

## Verify the connection

```powershell
claude mcp list          # servers with a connection status
claude mcp get ravendb   # the resolved command, args, and env
```

In a Claude Code session, run `/mcp`, then try *“list my RavenDB databases”* or
*“show cluster overview.”*

## Updating and removing

The `npx` and `dnx` paths fetch the latest published version on each launch, so there is nothing to
update. To change configuration, re-register:

```powershell
claude mcp remove ravendb
claude mcp add ravendb --scope user --env RAVENDB_URLS=... -- npx -y @ravendb/mcp
```

## Troubleshooting

| Symptom | Likely cause and fix |
| --- | --- |
| `At least one RavenDB URL must be configured` at startup | No `RAVENDB_URLS` and no `--config` URL. Set one. |
| Failed to connect | URL unreachable, wrong scheme (`http` vs `https`), or (secured) a bad or missing certificate path or clearance. |
| Tools never appear after adding | Start a new client session; servers load on session start. |
| Secured cluster rejects the agent | Certificate clearance too low or expired. Use Read on the target databases, or Operator for the server and cluster-wide tools. |
| Windows path errors in JSON config | Escape backslashes as `\\`. |

The server logs to stderr (stdout is reserved for JSON-RPC), so the client's server logs are where
startup and connection messages appear.

## FAQ

**Is it really read-only?**
Yes. There are no write or delete tools, only diagnostics plus read-only document and query access.
Connection-string secrets (passwords, API keys, cloud credentials, SAS tokens) are masked as
`***redacted***` at the database-record boundary.

**Do I need .NET or Node.js?**
The `npx` path needs Node.js 18+; `dnx` and the global tool need the .NET 10 SDK; a prebuilt binary
needs neither.

**What is a secured vs unsecured cluster?**
Secured = HTTPS plus a client certificate (set `RAVENDB_CERTIFICATE_PATH`, and the password if any).
Unsecured = HTTP, no certificate. The server works with both.

**Can one server connect to multiple clusters?**
No. One server targets one cluster; list that cluster's nodes in `RAVENDB_URLS`. Register a second
server for another cluster.

**Where do exported files go?**
A `ravendb-mcp-artifacts` folder in the system temp directory unless `RAVENDB_ARTIFACTS_PATH` is set.
See [Artifacts path](#artifacts-path).

**Where is it published?**
npm (`@ravendb/mcp`), NuGet (`RavenDB.Mcp`), prebuilt binaries on each
[GitHub release](https://github.com/ravendb/ravendb-mcp/releases), and the MCP Registry.
