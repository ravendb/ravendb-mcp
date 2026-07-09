# Releasing

RavenDB MCP ships through four **manually-dispatched** GitHub Actions workflows, run in order from
the Actions tab. Each builds only what it publishes, so they are independent and safe to re-run.

## One-time prerequisites

- Repository secrets:
  - `NUGET_API_KEY` — a NuGet.org API key (push scope, allowed to push new packages) for the
    account that owns `RavenDB.Mcp`.
  - `NPM_TOKEN` — an npm automation/granular token with publish rights to the `@ravendb` scope.
- The repository lives under the `ravendb` GitHub organization. The MCP Registry publish and npm
  provenance both use GitHub OIDC for that owner, so they need no extra secret.

## Cutting a release

1. **Bump the version** from the repo root:

   ```
   node scripts/set-version.mjs 1.0.0
   ```

   This sets the version consistently across the csproj, `.mcp/server.json`, `npm/package.json`
   (including the platform `optionalDependencies`), and the `INSTALL.md` example. Workflow&nbsp;1
   fails the release if any of them disagree.

2. **Open a PR** with the bump, get it reviewed, and merge to the default branch.

3. **Dispatch the workflows in order** (Actions → *Run workflow*):

   1. **Build & GitHub Release** — verifies the version is in sync, builds the self-contained
      binaries for all platforms, and creates the `vX.Y.Z` tag and GitHub release.
   2. **Publish NuGet**
   3. **Publish npm**
   4. **Publish MCP Registry** — run **last**; it validates that the NuGet and npm packages already
      exist.

Re-running is safe: NuGet uses `--skip-duplicate`, and the npm workflow skips any version already
published.
