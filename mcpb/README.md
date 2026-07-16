# MCPB desktop-extension bundle

Packages RavenDB MCP as a `.mcpb` for the Claude Desktop extension directory, which a local stdio
server cannot reach as a remote connector.

Decisions made here:

- **Server type `node`, not `binary`.** The `binary` type selects a binary by OS only, not by CPU
  architecture, and we ship both arm64 and x64 for Windows and macOS. So `server/cli.js` detects the
  OS and architecture and runs the matching bundled binary; one `.mcpb` covers all five targets.
- **All five binaries are bundled**, with no runtime download. There is no published `.mcpb` size
  limit, and a launcher that fetched a binary on first run would read as downloading and running
  remote code during review, and would fail on restricted networks. Bundling stays offline and
  auditable (about 175 MB).
- **Config is prompted via `user_config`** (cluster URL, certificate, masked password) and passed as
  the environment variables the server already reads.
- **Privacy policy** is in [../PRIVACY.md](../PRIVACY.md); `manifest.json` links it and RavenDB's
  policy, which directory submission requires.

`build.mjs` builds, stages, and packs the `.mcpb` on demand; `ci.yml` runs a Linux smoke test of the launcher.
