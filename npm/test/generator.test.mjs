import { test } from 'node:test';
import assert from 'node:assert/strict';
import { execFileSync, spawnSync } from 'node:child_process';
import { mkdtempSync, mkdirSync, writeFileSync, readFileSync, statSync, existsSync, rmSync } from 'node:fs';
import { gunzipSync } from 'node:zlib';
import { tmpdir } from 'node:os';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';

const here = dirname(fileURLToPath(import.meta.url));
const npmRoot = join(here, '..');
const generator = join(npmRoot, 'scripts', 'build-platform-packages.mjs');
const platforms = JSON.parse(readFileSync(join(npmRoot, 'platforms.json'), 'utf8'));
const node = process.execPath;

// Windows cannot store the POSIX executable bit, so it can only ever stage broken POSIX packages.
const onWindows = process.platform === 'win32';

// Stand in for `dotnet publish`: one binary per RID.
function fakePublishDir(work) {
  for (const meta of Object.values(platforms)) {
    mkdirSync(join(work, 'publish', meta.rid), { recursive: true });
    writeFileSync(join(work, 'publish', meta.rid, meta.bin), 'stub-binary');
  }
  return join(work, 'publish');
}

// Walk a tarball's 512-byte headers: name at offset 0, octal mode at 100, octal size at 124.
function tarModes(tgz) {
  const tar = gunzipSync(readFileSync(tgz));
  const modes = {};
  for (let off = 0; off + 512 <= tar.length; ) {
    const name = tar.toString('utf8', off, off + 100).replace(/\0.*$/, '');
    if (!name) break;
    const octal = (start, len) => parseInt(tar.toString('utf8', off + start, off + start + len).replace(/[\0 ]/g, ''), 8) || 0;
    modes[name.replace(/^package\//, '')] = octal(100, 8);
    off += 512 + Math.ceil(octal(124, 12) / 512) * 512;
  }
  return modes;
}

test('self-check confirms platforms.json matches the launcher optionalDependencies', () => {
  const out = execFileSync(node, [generator, '--self-check'], { encoding: 'utf8' });
  assert.match(out, /OK: platforms\.json and optionalDependencies agree/);
});

test('generator refuses to stage where the executable bit cannot be set',
  { skip: onWindows ? false : 'the host stores POSIX permissions, so staging is allowed' },
  () => {
    const work = mkdtempSync(join(tmpdir(), 'ravendb-mcp-gen-'));
    try {
      const publishDir = fakePublishDir(work);
      const r = spawnSync(node, [generator, '--version', '9.9.9', '--publish-dir', publishDir, '--out', join(work, 'dist')], { encoding: 'utf8' });
      assert.equal(r.status, 1, 'staging fails instead of publishing a non-executable binary');
      assert.match(r.stderr, /Cannot set the executable bit/);
    } finally {
      rmSync(work, { recursive: true, force: true });
    }
  });

test('generator stages the launcher + every platform package from published binaries',
  { skip: onWindows ? 'the generator refuses to stage POSIX packages here' : false },
  () => {
    const work = mkdtempSync(join(tmpdir(), 'ravendb-mcp-gen-'));
    try {
      const out = join(work, 'dist');
      execFileSync(node, [generator, '--version', '9.9.9', '--publish-dir', fakePublishDir(work), '--out', out]);

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
        if (os !== 'win32') {
          const mode = statSync(join(dir, 'bin', meta.bin)).mode;
          assert.ok(mode & 0o111, `${meta.pkg} binary is executable (mode ${(mode & 0o7777).toString(8)})`);
        }
      }
    } finally {
      rmSync(work, { recursive: true, force: true });
    }
  });

// The regression behind 1.0.3's EACCES: staging is only half the story, the mode has to survive
// `npm pack`, because that tarball is what users download.
test('packed platform tarballs keep the executable bit',
  { skip: onWindows ? 'the generator refuses to stage POSIX packages here' : false },
  () => {
    const work = mkdtempSync(join(tmpdir(), 'ravendb-mcp-pack-'));
    try {
      const out = join(work, 'dist');
      execFileSync(node, [generator, '--version', '9.9.9', '--publish-dir', fakePublishDir(work), '--out', out]);

      const tarballs = join(work, 'tarballs');
      mkdirSync(tarballs);
      for (const [key, meta] of Object.entries(platforms)) {
        if (key.startsWith('win32-')) continue;
        execFileSync('npm', ['pack', '--pack-destination', tarballs], { cwd: join(out, ...meta.pkg.split('/')), stdio: 'ignore' });
        const tgz = join(tarballs, `${meta.pkg.replace(/^@/, '').replace('/', '-')}-9.9.9.tgz`);
        const mode = tarModes(tgz)[`bin/${meta.bin}`];
        assert.ok(mode & 0o111, `${meta.pkg} tarball is executable (mode ${(mode & 0o7777).toString(8)})`);
      }
    } finally {
      rmSync(work, { recursive: true, force: true });
    }
  });
