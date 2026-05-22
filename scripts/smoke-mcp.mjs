import { spawn } from "node:child_process";
import { createInterface } from "node:readline";

const server = spawn(
  "dotnet",
  ["run", "--no-build"],
  {
    cwd: new URL("../src/RavenDB.Mcp/", import.meta.url),
    stdio: ["pipe", "pipe", "pipe"],
  }
);

server.stderr.on("data", chunk => {
  process.stderr.write(chunk);
});

const pending = new Map();
const stdout = createInterface({ input: server.stdout });

stdout.on("line", line => {
  if (!line.trim()) {
    return;
  }

  const message = JSON.parse(line);
  if (message.id !== undefined && pending.has(message.id)) {
    pending.get(message.id)(message);
    pending.delete(message.id);
  }
});

let nextId = 1;

function request(method, params = {}) {
  const id = nextId++;
  const message = { jsonrpc: "2.0", id, method, params };

  const response = new Promise(resolve => pending.set(id, resolve));
  server.stdin.write(`${JSON.stringify(message)}\n`);
  return response;
}

function notify(method, params = {}) {
  server.stdin.write(`${JSON.stringify({ jsonrpc: "2.0", method, params })}\n`);
}

function fail(message) {
  server.kill();
  throw new Error(message);
}

const initialize = await request("initialize", {
  protocolVersion: "2025-06-18",
  capabilities: {},
  clientInfo: {
    name: "ravendb-mcp-smoke",
    version: "0.1.0",
  },
});

if (initialize.error) {
  fail(`initialize failed: ${JSON.stringify(initialize.error)}`);
}

notify("notifications/initialized");

const tools = await request("tools/list");
if (tools.error) {
  fail(`tools/list failed: ${JSON.stringify(tools.error)}`);
}

const toolNames = tools.result.tools.map(tool => tool.name);
console.log(`Tools: ${toolNames.join(", ")}`);

for (const expected of ["list_databases", "get_database_record", "get_server_info"]) {
  if (!toolNames.includes(expected)) {
    fail(`Missing expected tool: ${expected}`);
  }
}

const serverInfo = await request("tools/call", {
  name: "get_server_info",
  arguments: {},
});

if (serverInfo.error) {
  fail(`get_server_info failed: ${JSON.stringify(serverInfo.error)}`);
}

console.log(`get_server_info: ${JSON.stringify(serverInfo.result.structuredContent)}`);

const databases = await request("tools/call", {
  name: "list_databases",
  arguments: {},
});

if (databases.error) {
  fail(`list_databases failed: ${JSON.stringify(databases.error)}`);
}

const databaseResult = databases.result.structuredContent;
console.log(`list_databases: ${JSON.stringify(databaseResult)}`);

if (databaseResult.databases?.length > 0) {
  const databaseName = databaseResult.databases[0];
  const databaseRecord = await request("tools/call", {
    name: "get_database_record",
    arguments: { databaseName },
  });

  if (databaseRecord.error) {
    fail(`get_database_record failed: ${JSON.stringify(databaseRecord.error)}`);
  }

  console.log(`get_database_record(${databaseName}): ok`);
} else {
  console.log("get_database_record: skipped because no databases were returned");
}

server.stdin.end();
server.kill();
