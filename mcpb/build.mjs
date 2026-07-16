// Builds the .mcpb desktop-extension bundle: publishes the five self-contained binaries, stages
// them next to the launcher and manifest, and packs with the mcpb CLI.
//
//   node mcpb/build.mjs               build all RIDs, stage, and pack -> dist/ravendb-mcp-<version>.mcpb
//   node mcpb/build.mjs --skip-build  reuse an existing artifacts/publish/ build
//
// Requires the .NET SDK and the mcpb CLI (from anthropics/mcpb).

import { execSync } from 'node:child_process';
import { readFileSync, mkdirSync, copyFileSync, rmSync, existsSync } from 'node:fs';
import { resolve, dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const repoRoot = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const skipBuild = process.argv.includes('--skip-build');

const RIDS = ['win-x64', 'win-arm64', 'linux-x64', 'osx-x64', 'osx-arm64'];
const binName = (rid) => (rid.startsWith('win-') ? 'ravendb-mcp.exe' : 'ravendb-mcp');

const version = JSON.parse(readFileSync(join(repoRoot, 'mcpb/manifest.json'), 'utf8')).version;
const run = (cmd) => execSync(cmd, { cwd: repoRoot, stdio: 'inherit' });

if (!skipBuild) {
  for (const rid of RIDS) {
    console.log(`build ${rid}`);
    run(`dotnet publish src/RavenDB.Mcp/RavenDB.Mcp.csproj -c Release -r ${rid} --self-contained true -p:PublishSingleFile=true -o artifacts/publish/${rid}`);
  }
}

console.log('stage bundle');
const stage = join(repoRoot, 'artifacts/mcpb');
rmSync(stage, { recursive: true, force: true });
mkdirSync(join(stage, 'server'), { recursive: true });
copyFileSync(join(repoRoot, 'mcpb/manifest.json'), join(stage, 'manifest.json'));
copyFileSync(join(repoRoot, 'mcpb/icon.png'), join(stage, 'icon.png'));
copyFileSync(join(repoRoot, 'mcpb/server/cli.js'), join(stage, 'server/cli.js'));
for (const rid of RIDS) {
  const src = join(repoRoot, 'artifacts/publish', rid, binName(rid));
  if (!existsSync(src)) throw new Error(`missing binary: ${src} (run without --skip-build)`);
  const dst = join(stage, 'server/bin', rid);
  mkdirSync(dst, { recursive: true });
  copyFileSync(src, join(dst, binName(rid)));
}

mkdirSync(join(repoRoot, 'dist'), { recursive: true });
const out = join(repoRoot, 'dist', `ravendb-mcp-${version}.mcpb`);
console.log('pack');
run(`mcpb pack "${stage}" "${out}"`);
console.log(`\nDone: ${out}`);
