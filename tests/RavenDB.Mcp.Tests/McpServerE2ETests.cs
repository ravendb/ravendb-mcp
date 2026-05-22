using System.Diagnostics;
using System.Text.Json;

namespace RavenDB.Mcp.Tests;

public sealed class McpServerE2ETests(RavenDbTestFixture fixture)
    : IClassFixture<RavenDbTestFixture>
{
    [Fact]
    public async Task ExposesAndCallsV1ToolsOverStdio()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await using var client = McpStdioClient.Start(fixture.Url);

        await client.Initialize(timeout.Token);

        using var tools = await client.Request("tools/list", null, timeout.Token);
        var toolNames = tools.RootElement
            .GetProperty("result")
            .GetProperty("tools")
            .EnumerateArray()
            .Select(tool => tool.GetProperty("name").GetString())
            .ToArray();

        Assert.Contains("get_server_info", toolNames);
        Assert.Contains("list_databases", toolNames);
        Assert.Contains("get_database_record", toolNames);
        Assert.Contains("get_cluster_topology", toolNames);
        Assert.Contains("get_database_stats", toolNames);
        Assert.Contains("list_indexes", toolNames);
        Assert.Contains("list_running_operations", toolNames);
        Assert.Contains("get_replication_active_connections", toolNames);
        Assert.Contains("list_ongoing_tasks", toolNames);
        Assert.Contains("get_database_tcp_info", toolNames);

        using var serverInfo = await client.CallTool("get_server_info", null, timeout.Token);
        Assert.Equal("7.2", serverInfo.RootElement.GetProperty("productVersion").GetString());

        using var databases = await client.CallTool("list_databases", null, timeout.Token);
        Assert.Contains(
            fixture.DatabaseName,
            databases.RootElement.GetProperty("databases").EnumerateArray().Select(database => database.GetString()));

        using var databaseRecord = await client.CallTool(
            "get_database_record",
            new { databaseName = fixture.DatabaseName },
            timeout.Token);

        Assert.Equal(
            fixture.DatabaseName,
            databaseRecord.RootElement.GetProperty("databaseName").GetString());

        using var topology = await client.CallTool("get_cluster_topology", null, timeout.Token);
        Assert.Equal(JsonValueKind.Object, topology.RootElement.GetProperty("topology").ValueKind);

        using var stats = await client.CallTool(
            "get_database_stats",
            new { databaseName = fixture.DatabaseName },
            timeout.Token);
        Assert.Equal(fixture.DatabaseName, stats.RootElement.GetProperty("databaseName").GetString());

        using var indexes = await client.CallTool(
            "list_indexes",
            new { databaseName = fixture.DatabaseName },
            timeout.Token);
        Assert.Equal(JsonValueKind.Array, indexes.RootElement.GetProperty("indexes").ValueKind);

        using var operations = await client.CallTool(
            "list_running_operations",
            new { databaseName = fixture.DatabaseName },
            timeout.Token);
        Assert.Equal(JsonValueKind.Object, operations.RootElement.GetProperty("operations").ValueKind);

        using var replication = await client.CallTool(
            "get_replication_active_connections",
            new { databaseName = fixture.DatabaseName },
            timeout.Token);
        Assert.Equal(JsonValueKind.Object, replication.RootElement.GetProperty("connections").ValueKind);
    }
}

internal sealed class McpStdioClient : IAsyncDisposable
{
    private readonly Process _process;
    private int _nextId;

    private McpStdioClient(Process process)
    {
        _process = process;
    }

    public static McpStdioClient Start(string ravenDbUrl)
    {
        var serverAssembly = typeof(global::RavenDB.Mcp.RavenDB.RavenDbAdminClient).Assembly.Location;

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            ArgumentList = { serverAssembly },
            WorkingDirectory = Path.GetDirectoryName(serverAssembly)!,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        startInfo.Environment["Urls__0"] = ravenDbUrl;

        var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start RavenDB.Mcp process.");

        process.ErrorDataReceived += (_, _) => { };
        process.BeginErrorReadLine();

        return new McpStdioClient(process);
    }

    public async Task Initialize(CancellationToken cancellationToken)
    {
        using var response = await Request("initialize", new
        {
            protocolVersion = "2025-06-18",
            capabilities = new { },
            clientInfo = new
            {
                name = "ravendb-mcp-tests",
                version = "0.1.0"
            }
        }, cancellationToken);

        await Send(new
        {
            jsonrpc = "2.0",
            method = "notifications/initialized",
            @params = new { }
        }, cancellationToken);
    }

    public async Task<JsonDocument> CallTool(string name, object? arguments, CancellationToken cancellationToken)
    {
        using var response = await Request("tools/call", new
        {
            name,
            arguments = arguments ?? new { }
        }, cancellationToken);

        if (response.RootElement.TryGetProperty("error", out var error))
            throw new InvalidOperationException(error.ToString());

        return JsonDocument.Parse(
            response.RootElement
                .GetProperty("result")
                .GetProperty("structuredContent")
                .GetRawText());
    }

    public async Task<JsonDocument> Request(string method, object? parameters, CancellationToken cancellationToken)
    {
        var id = Interlocked.Increment(ref _nextId);
        await Send(new
        {
            jsonrpc = "2.0",
            id,
            method,
            @params = parameters ?? new { }
        }, cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await _process.StandardOutput.ReadLineAsync(cancellationToken);
            if (line is null)
                throw new InvalidOperationException("MCP server closed stdout.");

            if (string.IsNullOrWhiteSpace(line))
                continue;

            var document = JsonDocument.Parse(line);
            if (document.RootElement.TryGetProperty("id", out var responseId) && responseId.GetInt32() == id)
                return document;

            document.Dispose();
        }

        throw new OperationCanceledException(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (!_process.HasExited)
        {
            await _process.StandardInput.DisposeAsync();
            _process.Kill(entireProcessTree: true);
        }

        _process.Dispose();
    }

    private async Task Send(object message, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(message);
        await _process.StandardInput.WriteLineAsync(json.AsMemory(), cancellationToken);
        await _process.StandardInput.FlushAsync(cancellationToken);
    }
}
