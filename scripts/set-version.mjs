// Sets the release version across every manifest the release check enforces.
// Usage: node scripts/set-version.mjs 1.0.0

import { readFileSync, writeFileSync } from 'node:fs';

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
];

let total = 0;
for (const [path, re, repl] of edits) {
  const before = readFileSync(path, 'utf8');
  const count = (before.match(re) || []).length;
  if (count > 0) writeFileSync(path, before.replace(re, repl));
  total += count;
  console.log(`  ${path}: ${count} occurrence(s)`);
}

console.log(`\n${current} -> ${next}  (${total} fields updated). Review the diff, then commit.`);
