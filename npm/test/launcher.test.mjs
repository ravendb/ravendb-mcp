import { test } from 'node:test';
import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { mkdtempSync, mkdirSync, writeFileSync, copyFileSync, chmodSync, readFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';

const here = dirname(fileURLToPath(import.meta.url));
const npmRoot = join(here, '..');
const platforms = JSON.parse(readFileSync(join(npmRoot, 'platforms.json'), 'utf8'));
const node = process.execPath;
const key = `${process.platform}-${process.arch}`;

// Stage the launcher the way npm would install it, under <root>/node_modules/@ravendb/mcp.
function stageLauncher(root) {
  const dir = join(root, 'node_modules', '@ravendb', 'mcp', 'bin');
  mkdirSync(dir, { recursive: true });
  copyFileSync(join(npmRoot, 'bin', 'cli.js'), join(dir, 'cli.js'));
  copyFileSync(join(npmRoot, 'platforms.json'), join(root, 'node_modules', '@ravendb', 'mcp', 'platforms.json'));
  return join(dir, 'cli.js');
}

test('platforms.json covers the current runtime', () => {
  assert.ok(platforms[key], `platform ${key} is present in platforms.json`);
});

test('missing platform package fails with a clean message and exit 1', () => {
  const work = mkdtempSync(join(tmpdir(), 'ravendb-mcp-launch-'));
  const cli = stageLauncher(work); // launcher only; no platform package installed
  const r = spawnSync(node, [cli], { cwd: work, encoding: 'utf8' });
  assert.equal(r.status, 1);
  assert.match(r.stderr, /not installed/);
});

test('launcher execs the platform binary, forwarding args and exit code',
  { skip: process.platform === 'win32' ? 'uses a POSIX shell stub' : false },
  () => {
    const meta = platforms[key];
    const work = mkdtempSync(join(tmpdir(), 'ravendb-mcp-launch-'));
    const cli = stageLauncher(work);
    const binDir = join(work, 'node_modules', ...meta.pkg.split('/'), 'bin');
    mkdirSync(binDir, { recursive: true });
    const binPath = join(binDir, meta.bin);
    writeFileSync(binPath, `#!/bin/sh\necho "STUB $*"\nexit 7\n`);
    chmodSync(binPath, 0o755);
    const r = spawnSync(node, [cli, '--config', 'x.json'], { cwd: work, encoding: 'utf8' });
    assert.equal(r.status, 7, 'exit code forwarded');
    assert.match(r.stdout, /STUB --config x\.json/, 'args forwarded to the binary');
  });
