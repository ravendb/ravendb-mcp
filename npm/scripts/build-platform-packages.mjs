import { readFileSync, writeFileSync, mkdirSync, copyFileSync, chmodSync, existsSync, rmSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const here = dirname(fileURLToPath(import.meta.url));
const npmRoot = resolve(here, '..');

const platforms = JSON.parse(readFileSync(join(npmRoot, 'platforms.json'), 'utf8'));
const launcher = JSON.parse(readFileSync(join(npmRoot, 'package.json'), 'utf8'));

function parseArgs(argv) {
  const args = {};
  for (let i = 0; i < argv.length; i++) {
    const a = argv[i];
    if (a === '--self-check') args.selfCheck = true;
    else if (a === '--version') args.version = argv[++i];
    else if (a === '--publish-dir') args.publishDir = argv[++i];
    else if (a === '--out') args.out = argv[++i];
  }
  return args;
}

function selfCheck() {
  const expected = Object.values(platforms).map((p) => p.pkg).sort();
  const declared = Object.keys(launcher.optionalDependencies ?? {}).sort();
  const missing = expected.filter((p) => !declared.includes(p));
  const extra = declared.filter((p) => !expected.includes(p));
  if (missing.length || extra.length) {
    if (missing.length) console.error(`Missing from optionalDependencies: ${missing.join(', ')}`);
    if (extra.length) console.error(`Declared but absent from platforms.json: ${extra.join(', ')}`);
    process.exit(1);
  }
  console.log(`OK: platforms.json and optionalDependencies agree on ${expected.length} platforms.`);
}

function buildPlatformPackage(key, meta, version, publishDir, outRoot) {
  const [os, cpu] = key.split('-');
  const srcBinary = join(publishDir, meta.rid, meta.bin);
  if (!existsSync(srcBinary)) {
    console.error(`Missing binary for ${meta.pkg}: ${srcBinary}`);
    process.exit(1);
  }
  const binDir = join(outRoot, meta.pkg, 'bin');
  mkdirSync(binDir, { recursive: true });
  const destBinary = join(binDir, meta.bin);
  copyFileSync(srcBinary, destBinary);
  if (os !== 'win32') chmodSync(destBinary, 0o755);
  const pkgJson = {
    name: meta.pkg,
    version,
    description: `${key} native binary for @ravendb/mcp (read-only MCP diagnostics server for RavenDB).`,
    os: [os],
    cpu: [cpu],
    files: ['bin'],
    license: launcher.license,
    repository: launcher.repository,
    homepage: launcher.homepage,
    bugs: launcher.bugs,
  };
  writeFileSync(join(outRoot, meta.pkg, 'package.json'), JSON.stringify(pkgJson, null, 2) + '\n');
  console.log(`Staged ${meta.pkg}@${version}`);
}

function stageLauncher(version, outRoot) {
  const pkgDir = join(outRoot, launcher.name);
  mkdirSync(join(pkgDir, 'bin'), { recursive: true });
  const staged = {
    ...launcher,
    version,
    optionalDependencies: Object.fromEntries(
      Object.keys(launcher.optionalDependencies).map((name) => [name, version]),
    ),
  };
  writeFileSync(join(pkgDir, 'package.json'), JSON.stringify(staged, null, 2) + '\n');
  copyFileSync(join(npmRoot, 'bin', 'cli.js'), join(pkgDir, 'bin', 'cli.js'));
  copyFileSync(join(npmRoot, 'platforms.json'), join(pkgDir, 'platforms.json'));
  if (existsSync(join(npmRoot, 'README.md'))) copyFileSync(join(npmRoot, 'README.md'), join(pkgDir, 'README.md'));
  console.log(`Staged ${launcher.name}@${version}`);
}

const args = parseArgs(process.argv.slice(2));

if (args.selfCheck) {
  selfCheck();
} else {
  if (!args.version || !args.publishDir || !args.out) {
    console.error('Usage: --version <v> --publish-dir <dir> --out <dir>  (or --self-check)');
    process.exit(1);
  }
  selfCheck();
  const outRoot = resolve(args.out);
  const publishDir = resolve(args.publishDir);
  if (existsSync(outRoot)) rmSync(outRoot, { recursive: true, force: true });
  for (const [key, meta] of Object.entries(platforms)) buildPlatformPackage(key, meta, args.version, publishDir, outRoot);
  stageLauncher(args.version, outRoot);
  console.log(`\nStaged ${Object.keys(platforms).length + 1} packages under ${outRoot}`);
}
