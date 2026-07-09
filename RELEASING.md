# Releasing

1. Bump the version: `node scripts/set-version.mjs <version>` (e.g. `1.0.0`).
2. Open a PR with the bump and merge it.
3. From the Actions tab, run the release workflows in order: **Build & GitHub Release → Publish NuGet → Publish npm → Publish MCP Registry**.

Requires the `NUGET_API_KEY` and `NPM_TOKEN` repository secrets.
