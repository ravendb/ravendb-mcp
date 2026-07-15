# Security Policy

## Reporting a vulnerability

Please report security vulnerabilities privately to **support@ravendb.net** (subject line
`SECURITY: ravendb-mcp`). Do not open a public GitHub issue for security reports.

Include enough detail to reproduce the issue. We will acknowledge your report and keep you
informed of the resolution.

## Security model

RavenDB MCP is a **read-only** diagnostics server:

- No write or delete tools are exposed.
- **Redacted.** Connection-string secrets (passwords, cloud access keys, SAS tokens, AI-provider API
  keys, certificates) are masked as `***redacted***` in the structured JSON the server returns: the
  database record and everything derived from it (backup / ETL / replication task configs), database
  and server settings, ongoing-task and AI-agent configs, and the server-wide backup configuration.
- **Not scrubbed.** Free-text log exports and live feeds (`export_server_logs`, and the `AdminLogs`
  and `TrafficWatch` sample feeds) and the binary debug packages (`collect_debug_package`) are
  returned as-is and may contain secrets that structural redaction cannot mask. Treat them as sensitive.
- Access is bounded by the client certificate's RavenDB permissions. Prefer a least-privilege
  certificate (per-database Read for the database and query tools); Operator clearance is only needed
  for the server-wide and cluster-wide diagnostics.
