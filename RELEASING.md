# Releasing

1. Bump the version with `node scripts/set-version.mjs <version>`, then open a PR and merge it.
2. Run the release steps in order:
   1. Build & GitHub Release
   2. Publish NuGet
   3. Publish npm: `node scripts/release-npm.mjs`
   4. Publish MCP Registry

Step 3 has to run on Linux or macOS, from WSL or a container with the .NET SDK and Node. npm records
each file's mode from disk and `chmod` is a no-op on Windows, so a Windows-staged package ships
`bin/ravendb-mcp` without the executable bit and `npx -y @ravendb/mcp` fails with `EACCES`. Staging
refuses to run on such a host.
