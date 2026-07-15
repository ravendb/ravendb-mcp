# Releasing

1. Bump the version with `node scripts/set-version.mjs <version>`, then open a PR and merge it.
2. Run the release steps in order:
   1. Build & GitHub Release
   2. Publish NuGet
   3. Publish npm
   4. Publish MCP Registry
