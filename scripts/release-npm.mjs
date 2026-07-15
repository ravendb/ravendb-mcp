// Builds the self-contained binaries, stages the @ravendb/mcp npm packages, and publishes them.
//
//   node scripts/release-npm.mjs               build, stage, and publish
//   node scripts/release-npm.mjs --dry-run     build and stage, then publish with --dry-run (no upload)
//   node scripts/release-npm.mjs --skip-build  reuse an existing artifacts/publish/ build
//
// Requires the .NET SDK, Node.js, and an npm login with publish access to the @ravendb scope.

import { execSync } from 'node:child_process';
import { readFileSync } from 'node:fs';
import { resolve, dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const repoRoot = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const flags = new Set(process.argv.slice(2));
const dryRun = flags.has('--dry-run');
const skipBuild = flags.has('--skip-build');

// One entry per platform: the .NET runtime id and the npm package that carries its binary.
const PLATFORMS = [
  { rid: 'win-x64', pkg: 'mcp-win32-x64' },
  { rid: 'win-arm64', pkg: 'mcp-win32-arm64' },
  { rid: 'linux-x64', pkg: 'mcp-linux-x64' },
  { rid: 'osx-x64', pkg: 'mcp-darwin-x64' },
  { rid: 'osx-arm64', pkg: 'mcp-darwin-arm64' },
];

const version = JSON.parse(readFileSync(join(repoRoot, 'npm/package.json'), 'utf8')).version;
const run = (cmd) => execSync(cmd, { cwd: repoRoot, stdio: 'inherit' });

function alreadyPublished(pkg) {
  try {
    execSync(`npm view @ravendb/${pkg}@${version} version`, { cwd: repoRoot, stdio: 'ignore' });
    return true;
  } catch {
    return false;
  }
}

function publish(pkg) {
  if (alreadyPublished(pkg)) {
    console.log(`skip     @ravendb/${pkg}@${version} (already on npm)`);
    return;
  }
  console.log(`publish  @ravendb/${pkg}@${version}`);
  run(`npm publish npm/dist/@ravendb/${pkg} --access public${dryRun ? ' --dry-run' : ''}`);
}

console.log(`\n@ravendb/mcp ${version}${dryRun ? '  (dry run)' : ''}\n`);

if (!skipBuild) {
  for (const { rid } of PLATFORMS) {
    console.log(`build    ${rid}`);
    run(`dotnet publish src/RavenDB.Mcp/RavenDB.Mcp.csproj -c Release -r ${rid} --self-contained true -p:PublishSingleFile=true -o artifacts/publish/${rid}`);
  }
}

console.log('stage    npm packages');
run(`node npm/scripts/build-platform-packages.mjs --version ${version} --publish-dir artifacts/publish --out npm/dist`);

// Platform packages first, so the launcher's optionalDependencies resolve when it is installed.
for (const { pkg } of PLATFORMS) publish(pkg);
publish('mcp');

console.log('\nDone.');
