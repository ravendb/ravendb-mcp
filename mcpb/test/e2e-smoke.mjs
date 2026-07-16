// CI smoke test for the MCPB launcher, using the official MCP client SDK. A green run proves the
// launcher (server/cli.js) finds and execs the bundled binary, the stdio transport works, and the
// native library (libzstd) self-extracts on this OS/arch -- because list_databases goes through the
// typed RavenDB client. Requires a RavenDB reachable at RAVENDB_URLS and a binary staged at
// server/bin/<rid>/.

import { Client } from '@modelcontextprotocol/sdk/client/index.js';
import { StdioClientTransport } from '@modelcontextprotocol/sdk/client/stdio.js';

const RAVENDB_URLS = process.env.RAVENDB_URLS || 'http://localhost:8080';

const transport = new StdioClientTransport({
  command: process.execPath, // node
  args: ['mcpb/server/cli.js'], // the bundled launcher
  env: { ...process.env, RAVENDB_URLS },
});
const client = new Client({ name: 'mcpb-ci-smoke', version: '1.0.0' });

try {
  await client.connect(transport); // performs the MCP initialize handshake
  console.log('connect OK: initialize handshake round-tripped through the launcher');

  const { tools } = await client.listTools();
  if (tools.length !== 21) throw new Error(`expected 21 tools, got ${tools.length}`);
  console.log(`tools/list OK: ${tools.length} tools`);

  // list_databases goes through the typed RavenDB client, exercising the native libzstd self-extract.
  const result = await client.callTool({ name: 'list_databases', arguments: {} });
  if (result.isError) throw new Error(`list_databases failed: ${JSON.stringify(result.content).slice(0, 300)}`);
  console.log('list_databases OK: launcher exec, typed client, and native library all work');

  console.log('\nMCPB launcher smoke test passed.');
  await client.close();
  process.exit(0);
} catch (err) {
  console.error(`FAIL: ${err.message}`);
  process.exit(1);
}
