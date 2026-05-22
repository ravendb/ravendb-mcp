# ADR-0004: Separate Unit, Integration, And E2E Test Responsibilities

## Status

Accepted

## Context And Problem Statement

The RavenDB MCP diagnostics server is a protocol adapter around RavenDB operations. Its important failure modes are at boundaries: configuration, RavenDB connectivity, MCP transport, tool registration, and structured tool results.

Tests should cover those boundaries directly instead of relying on mocks for RavenDB or MCP protocol behavior.

## Decision Drivers

- Unit tests are useful for deterministic logic that does not require RavenDB or an MCP transport.
- RavenDB-facing behavior must be tested against a running RavenDB instance.
- MCP-facing behavior must be tested through the MCP protocol, not by calling tool methods directly.
- The E2E suite should stay small enough to run in normal CI.
- CI should fail when the project cannot build, start, connect to RavenDB, or expose callable MCP tools.

## Considered Options

- Mostly unit tests with mocked RavenDB and mocked MCP
- Integration-first tests against real RavenDB
- E2E-only tests

## Decision Outcome

Chosen option: limited unit tests, broader integration tests, and a small E2E MCP test suite.

Unit tests are reserved for logic that can be validated without process, network, or RavenDB state.

Integration tests cover RavenDB behavior against a running RavenDB instance: client setup, selected RavenDB read operations, and expected failure handling.

E2E tests launch the MCP server over `stdio` and use MCP JSON-RPC to verify handshake, tool discovery, and selected tool calls against a running RavenDB instance.

GitHub Actions should start RavenDB and run the integration and E2E tests as part of CI.

## Consequences

- Good, because RavenDB client behavior is verified against a real server.
- Good, because MCP behavior is verified through the same protocol path used by agent hosts.
- Good, because unit tests stay limited to logic that is cheap and deterministic.
- Good, because the E2E suite is intentionally small enough for regular CI.
- Bad, because CI needs a RavenDB service/container.
- Bad, because integration and E2E tests are slower and more operationally sensitive than unit tests.

## More Information

- Product scope: [PRD: RavenDB MCP Diagnostics Server](../prd-ravendb-mcp-diagnostics-server.md)
