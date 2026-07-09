import { test } from 'node:test';
import assert from 'node:assert/strict';
import { execFileSync } from 'node:child_process';
import { mkdtempSync, mkdirSync, writeFileSync, readFileSync, existsSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';

const here = dirname(fileURLToPath(import.meta.url));
const npmRoot = join(here, '..');
const generator = join(npmRoot, 'scripts', 'build-platform-packages.mjs');
const platforms = JSON.parse(readFileSync(join(npmRoot, 'platforms.json'), 'utf8'));
const node = process.execPath;

test('self-check confirms platforms.json matches the launcher optionalDependencies', () => {
  const out = execFileSync(node, [generator, '--self-check'], { encoding: 'utf8' });
  assert.match(out, /OK: platforms\.json and optionalDependencies agree/);
});

test('generator stages the launcher + every platform package from published binaries', () => {
  const work = mkdtempSync(join(tmpdir(), 'ravendb-mcp-gen-'));
  try {
    // Stand in for `dotnet publish`: one binary per RID.
    for (const meta of Object.values(platforms)) {
      mkdirSync(join(work, 'publish', meta.rid), { recursive: true });
      writeFileSync(join(work, 'publish', meta.rid, meta.bin), 'stub-binary');
    }
    const out = join(work, 'dist');
    execFileSync(node, [generator, '--version', '9.9.9', '--publish-dir', join(work, 'publish'), '--out', out]);

    const launcher = JSON.parse(readFileSync(join(out, '@ravendb', 'mcp', 'package.json'), 'utf8'));
    assert.equal(launcher.name, '@ravendb/mcp');
    assert.equal(launcher.version, '9.9.9');
    assert.ok(existsSync(join(out, '@ravendb', 'mcp', 'bin', 'cli.js')), 'launcher ships cli.js');
    assert.ok(existsSync(join(out, '@ravendb', 'mcp', 'platforms.json')), 'launcher ships platforms.json');

    for (const [key, meta] of Object.entries(platforms)) {
      const [os, cpu] = key.split('-');
      const dir = join(out, ...meta.pkg.split('/'));
      const pkg = JSON.parse(readFileSync(join(dir, 'package.json'), 'utf8'));
      assert.equal(pkg.name, meta.pkg);
      assert.equal(pkg.version, '9.9.9');
      assert.deepEqual(pkg.os, [os]);
      assert.deepEqual(pkg.cpu, [cpu]);
      assert.ok(existsSync(join(dir, 'bin', meta.bin)), `${meta.pkg} ships its binary`);
      assert.equal(launcher.optionalDependencies[meta.pkg], '9.9.9', `${meta.pkg} pinned in launcher`);
    }
  } finally {
    rmSync(work, { recursive: true, force: true });
  }
});
