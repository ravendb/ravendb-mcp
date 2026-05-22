# ADR-0003: Use Stdio Transport For v1

## Status

Accepted

## Context And Problem Statement

The v1 MCP diagnostics server is a local, external process started by an AI-agent host.

MCP supports `stdio` for local process integration and Streamable HTTP for networked deployments.

## Decision Drivers

- v1 targets local agent-host integration.
- `stdio` is the standard MCP transport for local process integration.
- `stdio` avoids opening an HTTP port for v1.
- Streamable HTTP is needed for networked MCP deployments.

## Considered Options

- `stdio` first, Streamable HTTP later
- Streamable HTTP first
- Support both transports in v1

## Decision Outcome

Chosen option: `stdio` first, Streamable HTTP later.

In v1, the AI-agent host starts the MCP server as a local child process and communicates with it over stdin/stdout. The MCP server connects to RavenDB using configured RavenDB URL(s) and, when required, certificate details.

After v1 is ready, add Streamable HTTP for networked scenarios such as shared endpoints, browser/web-hosted agents, Aspire, containerized service deployment, and `Raven.Server`-hosted MCP.

## Consequences

- Good, because v1 follows the local MCP deployment model.
- Good, because v1 avoids transport hosting and MCP transport-auth decisions.
- Good, because the RavenDB diagnostics surface can be proven before adding networked transport.
- Bad, because clients that require a URL-based MCP server need Streamable HTTP.
- Bad, because Streamable HTTP must still be implemented before hosted or service-discovered MCP scenarios work.

## More Information

- Product scope: [PRD: RavenDB MCP Diagnostics Server](../prd-ravendb-mcp-diagnostics-server.md)
