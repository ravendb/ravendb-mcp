# Technical Design: Alpha

Status: Draft

## Goal

Build the smallest useful RavenDB MCP diagnostics server:

- run as a local external MCP server;
- connect to a configured RavenDB instance or cluster;
- expose read-only tools over MCP `stdio`;
- support RavenDB 7.2 first;
- support unsecured RavenDB on demand and PFX certificate auth for secured RavenDB;
- return structured outputs with clear errors.

## Project Shape

| Area | Decision |
|---|---|
| Solution name | `RavenDB.Mcp` |
| Server project | `src/RavenDB.Mcp` |
| Tests directory | `tests/` |
| Target framework | `.NET 10` |
| Hosting model | .NET Generic Host for config, DI, logging, and process lifetime |
| Transport | MCP `stdio` |
| Tests | Not part of the first coding pass |

## Runtime Configuration

Alpha uses a flat JSON config file.

Required:

- RavenDB URL(s)

Optional:

- PFX certificate path
- PFX certificate password

If no certificate is configured, the server connects without a client certificate. If a certificate is configured, Alpha supports PFX only.

## RavenDB Client Lifetime

Use a singleton `DocumentStore` initialized from configuration.

The `DocumentStore` is created once during host startup, reused by tools, and disposed when the host shuts down.

## MCP Tool Registration

Use attribute-based MCP tool registration.

Define tool classes by category, inject RavenDB services through DI, and expose tools with snake_case names.

## Alpha Tools

| Tool | Purpose | Input | Output |
|---|---|---|---|
| `list_databases` | Fetch database names visible to the configured RavenDB connection | none | record with database names |
| `get_database_record` | Fetch one database record | database name | structured database-record result |
| `get_server_info` | Fetch server version/build info | none | structured server info |

## Tool Shape

- Tool names use snake_case.
- Tool inputs are explicit records or simple parameters.
- Tool outputs are records.
- Avoid anonymous objects and loosely shaped dictionaries unless RavenDB returns data that is intentionally open-ended.
- Async tools accept `CancellationToken` and pass it through to RavenDB calls where supported.

## Errors

Use clear errors for:

- invalid config;
- missing certificate file;
- invalid certificate password;
- RavenDB connection failure;
- authentication failure;
- authorization failure;
- database not found.

Unexpected failures can flow through exceptions. Expected setup and permission failures should be readable to the user.

## Logging

Do not write human logs to stdout because stdout carries the MCP protocol in `stdio` mode.

Use stderr or configured logging sinks.

## Security

Alpha is read-only.

Write/delete tools are not registered.

For secured RavenDB, Alpha supports PFX certificates only. RavenDB permissions decide what the certificate can access.

## Prototype Review

The referenced `gregolsky/ravendb-doctor-mcp` prototype may be reviewed for tool category organization and naming inspiration. It is not a source of architecture decisions for Alpha.
