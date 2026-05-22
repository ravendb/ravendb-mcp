# ADR-0002: Use RavenDB Certificates With MCP Tool Safety Labels

## Status

Accepted

## Context And Problem Statement

The v1 MCP diagnostics server is a local, external process that connects to RavenDB and exposes read-only diagnostic tools to AI-agent clients.

It needs to connect to RavenDB without inventing a new authentication model.

It also needs to make tool risk visible to AI-agent clients while keeping enforcement inside the MCP server and RavenDB permissions.

## Decision Drivers

- RavenDB already supports certificate-based authentication and permissions.
- `Operator` certificate clearance is the closest fit available today for diagnostics.
- v1 is read-only.
- MCP tool annotations help clients understand risk, but are not enforcement.
- A later RavenDB MCP/diagnostics permission tier should be more granular than `Operator`.

## Considered Options

- Reuse RavenDB certificate auth with `Operator` clearance for v1
- Add a new MCP-specific auth model for v1
- MCP-managed per-agent credential brokering

## Decision Outcome

Chosen option: reuse RavenDB certificate auth with `Operator` clearance for v1.

For secured RavenDB instances, the MCP server connects with a RavenDB-generated certificate. RavenDB permissions decide what the certificate can access.

v1 exposes read-only tools only. Write/delete tools are not registered.

Tools will be labeled with MCP safety annotations where applicable, such as read-only metadata for v1 tools and destructive metadata for later tools if they are added. These labels are client-facing hints. The server still decides which tools exist.

In a later release, RavenDB should support a more granular MCP/diagnostics permission tier so users do not need to rely on broad `Operator` clearance for diagnostics.

## Consequences

- Good, because v1 uses RavenDB's existing security model.
- Good, because users do not need a new credential type for v1.
- Good, because tool labels improve client UX without becoming the security boundary.
- Good, because v1 stays read-only by construction.
- Bad, because `Operator` is broader than an ideal diagnostics-only permission set.
- Bad, because a later MCP/diagnostics permission tier requires RavenDB product work.

## Rejected Option: MCP-Managed Per-Agent Credential Brokering

One proposed direction was to make the MCP server act as a credential broker: keep a privileged manager certificate, authenticate the human/session, and issue short-lived narrowly scoped credentials per agent, profile, database, or session.

We reject this for the v1 MCP diagnostics server.

- It adds substantial auth complexity before the diagnostics surface is stable.
- Multiple agents can share the same RavenDB certificate when they operate under the same trust and audit boundary.
- v1 is read-only and does not need short-lived per-agent credentials to meet its safety goals.
- RavenDB permissions plus MCP tool registration are enough for the v1 scope.

## More Information

- Product scope: [PRD: RavenDB MCP Diagnostics Server](../prd-ravendb-mcp-diagnostics-server.md)
