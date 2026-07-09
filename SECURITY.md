# Security Policy

## Reporting a vulnerability

Please report security vulnerabilities privately to **support@ravendb.net** (subject line
`SECURITY: ravendb-mcp`). Do not open a public GitHub issue for security reports.

Include enough detail to reproduce the issue. We will acknowledge your report and keep you
informed of the resolution.

## Security model

RavenDB MCP is a **read-only** diagnostics server:

- No write or delete tools are exposed.
- Connection-string secrets (passwords, API keys, cloud credentials, SAS tokens) are masked as
  `***redacted***` before any database record is returned.
- What the agent can access is ultimately bounded by the RavenDB permissions of the client
  certificate the server is configured with; an Operator-clearance certificate is recommended.
