# Project Scope Defaults

This project is a RavenDB MCP diagnostics server. The v1 goal is deliberately small: a local external C# MCP server that connects to RavenDB through configured URL/certificate settings and exposes read-only diagnostic tools over stdio.

The project source of truth is the PRD and ADRs under `docs/`. Other planning notes or older docs may be stale; check them only as context, not as binding requirements.

The code should be demo-shallow and professional: direct enough to show the mechanism clearly, but not throwaway. Prefer the RavenDB Client API and MCP SDK surface as they are. Do not add architecture, policy, or future-proofing unless the current feature needs it.

## Coding Defaults

- No fake generality. If v1 does not genuinely need config sections, password environment indirection, paging abstractions, custom sorting, counts, wrappers, or fallback shapes, do not add them.
- Do not invent product policy in plumbing code. A tool should report what RavenDB returns unless there is an explicit product reason to reshape it.
- Prefer real domain objects unless there is a concrete reason not to. Do not serialize RavenDB objects into generic JSON just because it feels safer.
- Names should carry the meaning. Add descriptions or comments only when the name is ambiguous, risky, or non-obvious.
- Test-specific things should look test-specific. Use clear test-runner variables such as `RAVENDB_TEST_URL` instead of leaking framework binding syntax into CI.
- Avoid ceremony that exists only to make code look architectural. Constants, validators, result fields, helper classes, and abstractions must earn their place.
- Do not silently hide limits. If a tool has paging, truncation, redaction, or filtering, make that an explicit tool/API decision.
- Keep v1 honest. Prove the MCP/RavenDB path first; do not solve future production UX, fleet scale, config ergonomics, or advanced security in passing.
- Make failure explicit at the boundary. A clear setup error is better than a hidden fallback that changes behavior depending on the machine.
- Use framework mechanics where they protect the actual protocol or runtime. For example, stdio MCP must keep logs off stdout, so console logs should go to stderr.
- Every line should answer: what current problem does this solve? If the answer is only "maybe later", cut it or move the thought to docs.

## MCP Tool Contract Defaults

- Tool schemas are agent-facing API contracts. Keep them small, stable, and readable in `tools/list`.
- Treat schema size and result size as context-window costs. Large contracts and large payloads reduce how much useful diagnostic state the agent can keep.
- Use precise records for stable summaries and normal diagnostic results.
- Do not expose large RavenDB Client object graphs as fully expanded MCP schemas.
- For raw RavenDB payloads, prefer a permissive JSON field such as `JsonElement` over copying the whole RavenDB type into our own contract.
- If a RavenDB Client type does not serialize usefully through the MCP/System.Text.Json path, fix it at the narrow tool-result boundary. Do not change global serializer behavior for one tool.
- Split summary and raw detail when the raw shape is large or unstable.
- Large artifacts such as logs, debug packages, dumps, traces, and long query results should move through resource links or files when that feature exists, not giant tool schemas.
- Keep `tools/list` metadata lean: snake_case names, read-only annotations, structured output schemas, and descriptions only when a name is genuinely unclear.
