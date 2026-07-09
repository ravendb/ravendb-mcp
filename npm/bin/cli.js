#!/usr/bin/env node
'use strict';

const { spawnSync } = require('node:child_process');
const platforms = require('../platforms.json');

const key = `${process.platform}-${process.arch}`;
const target = platforms[key];

if (!target) {
  console.error(`[ravendb-mcp] Unsupported platform "${key}". Supported: ${Object.keys(platforms).join(', ')}.`);
  process.exit(1);
}

let binaryPath;
try {
  binaryPath = require.resolve(`${target.pkg}/bin/${target.bin}`);
} catch {
  console.error(`[ravendb-mcp] Platform package "${target.pkg}" is not installed; reinstall without disabling optional dependencies.`);
  process.exit(1);
}

// stdout is the MCP JSON-RPC stream; never write to it. Inherit stdio so the binary owns it, and our own output goes to stderr.
const result = spawnSync(binaryPath, process.argv.slice(2), { stdio: 'inherit' });

if (result.error) {
  console.error(`[ravendb-mcp] Failed to launch "${binaryPath}": ${result.error.message}`);
  process.exit(1);
}
if (result.signal) {
  console.error(`[ravendb-mcp] Server terminated by signal ${result.signal}.`);
  process.exit(1);
}
process.exit(result.status ?? 0);
