# Privacy Policy

RavenDB MCP is a local, read-only diagnostics server. It runs on your machine, connects only to the
RavenDB cluster you configure, and returns results to the AI agent you run it in.

## What we collect

Nothing. The server has no telemetry, analytics, tracking, or "phone home" of any kind. RavenDB and
Anthropic receive no data from it.

## Network activity

The only connections it makes are to the RavenDB cluster URL(s) you set in `RAVENDB_URLS` (plus, if
you install via `npx` or `dnx`, a one-time download of the server itself from npm or NuGet).

## Your data

- Diagnostics, and on request documents and query results, flow only between your cluster, this local
  process, and your agent. `get_document_data` and `run_query` return real cluster data into the
  agent's model context — choosing what cluster to expose, and how that data is handled once it
  reaches the agent, is up to you.
- Connection-string secrets (passwords, API keys, cloud credentials, SAS tokens) are masked as
  `***redacted***` before leaving the server.
- Exported files (logs, debug packages) are written only to a local folder you control
  (`RAVENDB_ARTIFACTS_PATH`, or your system temp folder). The default folder is locked to your user
  and its exports expire after 24 hours; a folder you set is left for you to manage.

## Storage, sharing, retention

The server stores nothing beyond those local export files, shares nothing with third parties, and
retains nothing after the process exits.

## Contact

Privacy questions: support@ravendb.net

RavenDB's company-wide privacy policy: https://ravendb.net/legal/website/privacy-policy
