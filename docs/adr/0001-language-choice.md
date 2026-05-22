# ADR-0001: Use C# For The RavenDB MCP Server

## Status

Accepted

## Context And Problem Statement

The v1 RavenDB MCP diagnostics server is a local, external MCP server that users run and connect to from their AI agent.

The v1 implementation does not live inside RavenDB Server. However, if the MCP surface proves useful and becomes stable enough, a later release may move or expose the implementation from `Raven.Server` under a server endpoint. In that mode, users would not need to set up a separate local MCP process.

The language choice should support both the v1 external server and a possible later RavenDB-integrated implementation.

## Decision Drivers

- The MCP ecosystem has a Tier 1 C# SDK.
- RavenDB Server is implemented in C#.
- The implementation should be able to use the official RavenDB .NET client and RavenDB-native types.
- RavenDB contributors should be able to review and maintain the MCP code without switching stacks.
- A later `Raven.Server` endpoint should not require rewriting the MCP implementation in another language.

## Considered Options

- C#
- TypeScript
- Python

## Decision Outcome

Chosen option: C#.

The v1 implementation will still run as an external MCP server. Choosing C# uses a Tier 1 MCP SDK, keeps the code close to RavenDB's existing server and client ecosystem, and preserves the option to integrate the mature MCP implementation into `Raven.Server` later.

## Pros And Cons Of The Options

### C#

- Good, because MCP has a Tier 1 C# SDK.
- Good, because RavenDB Server and the official .NET client are C#/.NET-native.
- Good, because the same implementation can plausibly move into `Raven.Server`.
- Good, because RavenDB engineers can maintain it in the same stack.
- Bad, because the v1 project may carry more .NET setup than a small script-based prototype.

### TypeScript

- Good, because TypeScript is common in MCP examples and agent tooling.
- Good, because it can be quick for prototypes.
- Bad, because a later `Raven.Server` integration would likely require a rewrite.
- Bad, because it is farther from RavenDB's server and .NET client ecosystem.

### Python

- Good, because Python is quick for prototypes and diagnostics scripts.
- Bad, because it is not aligned with RavenDB Server implementation.
- Bad, because a later `Raven.Server` integration would likely require a rewrite.
- Bad, because it creates a separate runtime and maintenance stack for official RavenDB tooling.

## More Information

- Product scope: [PRD: RavenDB MCP Diagnostics Server](../prd-ravendb-mcp-diagnostics-server.md)
