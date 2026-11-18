// Sets the release version across every manifest the release check enforces.
// Usage: node scripts/set-version.mjs 1.0.0

import { readFileSync, writeFileSync, existsSync } from 'node:fs';
import { execFileSync } from 'node:child_process';

const next = process.argv[2];
if (!next || !/^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?$/.test(next)) {
  console.error('Usage: node scripts/set-version.mjs <version>   e.g. 1.0.0');
  process.exit(1);
}

const csprojPath = 'src/RavenDB.Mcp/RavenDB.Mcp.csproj';
const current = readFileSync(csprojPath, 'utf8').match(/<Version>([^<]+)<\/Version>/)?.[1];
if (!current) {
  console.error(`Could not read <Version> from ${csprojPath}`);
  process.exit(1);
}
if (current === next) {
  console.log(`Already at ${next}.`);
  process.exit(0);
}

const cur = current.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
const edits = [
  [csprojPath, new RegExp(`(<Version>)${cur}(</Version>)`, 'g'), `$1${next}$2`],
  ['.mcp/server.json', new RegExp(`"${cur}"`, 'g'), `"${next}"`],
  ['npm/package.json', new RegExp(`"${cur}"`, 'g'), `"${next}"`],
  ['INSTALL.md', new RegExp(`(RavenDB\\.Mcp@)${cur}`, 'g'), `$1${next}`],
  ['mcpb/manifest.json', new RegExp(`"${cur}"`, 'g'), `"${next}"`],
  // Drives the `/plugin install` path the README recommends, so a stale version here ships a
  // plugin pointing at the previous release.
  ['.claude-plugin/plugin.json', new RegExp(`"${cur}"`, 'g'), `"${next}"`],
];

let total = 0;
for (const [path, re, repl] of edits) {
  if (!existsSync(path)) {
    console.log(`  ${path}: not present, skipped`);
    continue;
  }
  const before = readFileSync(path, 'utf8');
  const count = (before.match(re) || []).length;
  if (count > 0) writeFileSync(path, before.replace(re, repl));
  total += count;
  console.log(`  ${path}: ${count} occurrence(s)`);
}

console.log(`\n${current} -> ${next}  (${total} fields updated).`);

// The list above is hand-maintained, so it goes stale the moment someone adds a manifest that
// carries a version. `.claude-plugin/plugin.json` sat outside it for four releases and only stayed
// correct because it happened to get edited by hand. Sweep the tracked files afterwards and name
// anything still on the old version, so the next omission is loud instead of silent.
const tracked = execFileSync('git', ['ls-files'], { encoding: 'utf8' }).split('\n').filter(Boolean);
const stale = tracked.filter((path) => {
  if (path === 'scripts/set-version.mjs' || !existsSync(path)) return false;
  let text;
  try {
    text = readFileSync(path, 'utf8');
  } catch {
    return false; // binary or unreadable, nothing to stamp
  }
  return text.includes(current);
});

if (stale.length > 0) {
  console.error(`\nStill on ${current}:`);
  for (const path of stale) console.error(`  ${path}`);
  console.error(`\nEither add these to the edits list above, or confirm the match is a different
version that happens to read the same. Nothing is committed either way.`);
  process.exit(1);
}

console.log('No tracked file is left on the old version. Review the diff, then commit.');
