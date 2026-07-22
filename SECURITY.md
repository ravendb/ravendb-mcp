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

## Data reaches the agent

`get_document_data` and `run_query` (and any tool you point at a document or query) return **real
cluster data** into the agent's model context, as designed. Diagnostics can likewise echo live
values. What cluster you connect to, and what the agent then does with that data, is your call — scope
the certificate to the data the agent should see, and prefer a non-production cluster when the agent
or its transcript is not trusted with production contents.

## Certificate password

`RAVENDB_CERTIFICATE_PASSWORD` is read from the environment (or the `--config` file) and used only
in-process to open the `.pfx`; it is never logged or written to disk. Mark it as a secret in your
client configuration, and drop it entirely when the `.pfx` has no password.

## Exported files

Log exports and debug packages are written unredacted to the artifacts folder and can contain
secrets. When the location is left to default, the server writes to a per-user temp folder (locked to
the current user on Linux/macOS) and expires its own exports after 24 hours; a folder you set with
`RAVENDB_ARTIFACTS_PATH` is used as-is, and its contents are yours to retain and clean up. Either way,
treat that folder as sensitive.
