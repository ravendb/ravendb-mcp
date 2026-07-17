#!/usr/bin/env node
'use strict';

// MCPB launcher. Claude Desktop runs this with its bundled Node; it picks the self-contained
// RavenDB binary for the host OS and architecture and runs it, forwarding the MCP stdio stream.
// One bundle carries all platforms, which the `binary` server type cannot do (its platform
// overrides distinguish OS only, not arm64 vs x64).

const { spawn } = require('node:child_process');
const { existsSync, chmodSync } = require('node:fs');
const path = require('node:path');

// os-arch -> bundled binary directory. Mirrors npm/platforms.json.
const platforms = {
  'win32-x64': { rid: 'win-x64', bin: 'ravendb-mcp.exe' },
  'win32-arm64': { rid: 'win-arm64', bin: 'ravendb-mcp.exe' },
  'linux-x64': { rid: 'linux-x64', bin: 'ravendb-mcp' },
  'darwin-x64': { rid: 'osx-x64', bin: 'ravendb-mcp' },
  'darwin-arm64': { rid: 'osx-arm64', bin: 'ravendb-mcp' },
};

const key = `${process.platform}-${process.arch}`;
const target = platforms[key];
if (!target) {
  console.error(`[ravendb-mcp] Unsupported platform "${key}". Supported: ${Object.keys(platforms).join(', ')}.`);
  process.exit(1);
}

const binaryPath = path.join(__dirname, 'bin', target.rid, target.bin);
if (!existsSync(binaryPath)) {
  console.error(`[ravendb-mcp] Bundled binary not found for "${key}" at ${binaryPath}.`);
  process.exit(1);
}
if (process.platform !== 'win32') {
  try { chmodSync(binaryPath, 0o755); } catch { /* archives may not preserve the exec bit; best effort */ }
}

// Optional secured-cluster values may arrive empty or unsubstituted when the user left them blank;
// drop them so the server treats the cluster as unsecured rather than loading a bogus certificate.
const env = { ...process.env };
for (const name of ['RAVENDB_CERTIFICATE_PATH', 'RAVENDB_CERTIFICATE_PASSWORD']) {
  const value = env[name];
  if (!value || value.includes('${')) delete env[name];
}

// Proxy stdio by forwarding the streams in JS rather than inheriting OS handles. Claude Desktop's
// bundled Node hands us pipe handles that a spawned native binary cannot reliably inherit on
// Windows, which leaves the server "connected" but mute; copying the bytes ourselves works on
// every runtime. stdout carries the MCP JSON-RPC stream; we never write to it directly.
const child = spawn(binaryPath, process.argv.slice(2), { stdio: ['pipe', 'pipe', 'pipe'], env });
process.stdin.pipe(child.stdin);
child.stdout.pipe(process.stdout);
child.stderr.pipe(process.stderr);
child.stdin.on('error', () => { /* the server exited; ignore EPIPE on further client writes */ });
child.on('error', (err) => {
  console.error(`[ravendb-mcp] Failed to launch the server: ${err.message}`);
  process.exit(1);
});
child.on('exit', (code, signal) => process.exit(code ?? (signal ? 1 : 0)));
