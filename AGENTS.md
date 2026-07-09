# RavenDB MCP — Agent Orientation

## What this is

A local, external **read-only MCP diagnostics server for RavenDB**, written in C# (.NET 10) and spoken over **stdio**. It connects to one RavenDB cluster via configured URL(s) and an optional client certificate, and exposes read-only tools so an AI agent can assess cluster / database / index / task / storage / performance state and (read-only) data without hand-crafting REST calls.

- Transport is stdio, so all logs go to **stderr** — never write to stdout.
- The server targets **one cluster**; list that cluster's node URLs in configuration.
- It is **read-only**: no write or delete tools. Read-only document and query access is allowed; everything else is metadata / diagnostics.

## Conventions

- Keep the tool surface and result schemas small: they are agent-facing API contracts and a context-window cost. Prefer permissive `JsonElement` payloads described in each tool's `[Description]` over fully-expanded schemas.
- Every tool is read-only and carries a dual-use description (when to use it + what it returns).
- Make failure explicit at the boundary; avoid hidden fallbacks or future-proofing the current feature does not need.
