using System.Diagnostics;
using System.Text.Json;

namespace RavenDB.Mcp.Tests;

public sealed class McpServerE2ETests(RavenDbTestFixture fixture)
    : IClassFixture<RavenDbTestFixture>
{
    [Fact]
    public async Task ExposesAndCallsV1ToolsOverStdio()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        await using var client = McpStdioClient.Start(fixture.Url);

        await client.Initialize(timeout.Token);

        using var tools = await client.Request("tools/list", null, timeout.Token);
        var toolsArray = tools.RootElement
            .GetProperty("result")
            .GetProperty("tools")
            .EnumerateArray()
            .ToArray();
        var toolNames = toolsArray
            .Select(tool => tool.GetProperty("name").GetString())
            .ToArray();

        Assert.Equal(ExpectedToolNames.Order(), toolNames.Order());
        Assert.All(toolNames, name => Assert.Equal(name, name!.ToLowerInvariant()));
        Assert.All(toolsArray, tool => Assert.True(
            tool.GetProperty("annotations").GetProperty("readOnlyHint").GetBoolean()));

        using var serverInfo = await client.CallTool("get_server_info", null, timeout.Token);
        Assert.Equal("7.2", serverInfo.RootElement.GetProperty("productVersion").GetString());

        using var nodeStatus = await client.CallTool("get_node_status", null, timeout.Token);
        Assert.Equal(JsonValueKind.Object, nodeStatus.RootElement.GetProperty("status").ValueKind);

        using var logsConfiguration = await client.CallTool("get_logs_configuration", null, timeout.Token);
        Assert.Equal(JsonValueKind.Object, logsConfiguration.RootElement.GetProperty("configuration").ValueKind);

        using var serverWideClientConfiguration = await client.CallTool("get_server_wide_client_configuration", null, timeout.Token);
        Assert.True(serverWideClientConfiguration.RootElement.GetProperty("configuration").ValueKind is JsonValueKind.Object or JsonValueKind.Null);

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
        var nodeTag = topology.RootElement
            .GetProperty("topology")
            .GetProperty("NodeTag")
            .GetString()!;

        using var stats = await client.CallTool(
            "get_database_stats",
            new { databaseName = fixture.DatabaseName },
            timeout.Token);
        Assert.Equal(fixture.DatabaseName, stats.RootElement.GetProperty("databaseName").GetString());

        using var detailedStats = await client.CallTool(
            "get_detailed_database_stats",
            new { databaseName = fixture.DatabaseName },
            timeout.Token);
        Assert.Equal(JsonValueKind.Object, detailedStats.RootElement.GetProperty("stats").ValueKind);

        using var collectionStats = await client.CallTool(
            "get_collection_stats",
            new { databaseName = fixture.DatabaseName },
            timeout.Token);
        Assert.Equal(JsonValueKind.Object, collectionStats.RootElement.GetProperty("stats").ValueKind);

        using var detailedCollectionStats = await client.CallTool(
            "get_detailed_collection_stats",
            new { databaseName = fixture.DatabaseName },
            timeout.Token);
        Assert.Equal(JsonValueKind.Object, detailedCollectionStats.RootElement.GetProperty("stats").ValueKind);

        using var databaseConfiguration = await client.CallTool(
            "get_database_configuration",
            new { databaseName = fixture.DatabaseName },
            timeout.Token);
        Assert.Equal(JsonValueKind.Object, databaseConfiguration.RootElement.GetProperty("configuration").ValueKind);

        using var clientConfiguration = await client.CallTool(
            "get_client_configuration",
            new { databaseName = fixture.DatabaseName },
            timeout.Token);
        Assert.Equal(JsonValueKind.Object, clientConfiguration.RootElement.GetProperty("configuration").ValueKind);

        using var health = await client.CallTool(
            "get_database_health_summary",
            new { databaseName = fixture.DatabaseName },
            timeout.Token);
        Assert.Equal(JsonValueKind.Object, health.RootElement.GetProperty("stats").ValueKind);

        using var indexes = await client.CallTool(
            "list_indexes",
            new { databaseName = fixture.DatabaseName },
            timeout.Token);
        Assert.Equal(JsonValueKind.Array, indexes.RootElement.GetProperty("indexes").ValueKind);

        using var indexStats = await client.CallTool(
            "get_index_stats",
            new { databaseName = fixture.DatabaseName },
            timeout.Token);
        Assert.Equal(JsonValueKind.Array, indexStats.RootElement.GetProperty("stats").ValueKind);

        using var indexErrors = await client.CallTool(
            "get_index_errors",
            new { databaseName = fixture.DatabaseName },
            timeout.Token);
        Assert.Equal(JsonValueKind.Array, indexErrors.RootElement.GetProperty("errors").ValueKind);

        using var indexPerformance = await client.CallTool(
            "get_index_performance",
            new { databaseName = fixture.DatabaseName },
            timeout.Token);
        Assert.Equal(JsonValueKind.Array, indexPerformance.RootElement.GetProperty("performance").ValueKind);

        using var indexingStatus = await client.CallTool(
            "get_indexing_status",
            new { databaseName = fixture.DatabaseName },
            timeout.Token);
        Assert.Equal(JsonValueKind.Object, indexingStatus.RootElement.GetProperty("status").ValueKind);

        using var index = await client.CallTool(
            "get_index",
            new { databaseName = fixture.DatabaseName, indexName = fixture.IndexName },
            timeout.Token);
        Assert.Equal(fixture.IndexName, index.RootElement.GetProperty("indexName").GetString());

        using var indexTerms = await client.CallTool(
            "get_index_terms",
            new
            {
                databaseName = fixture.DatabaseName,
                indexName = fixture.IndexName,
                fieldName = fixture.IndexFieldName,
                fromValue = (string?)null,
                pageSize = 16
            },
            timeout.Token);
        Assert.Equal(JsonValueKind.Array, indexTerms.RootElement.GetProperty("terms").ValueKind);

        using var replication = await client.CallTool(
            "get_replication_performance",
            new { databaseName = fixture.DatabaseName },
            timeout.Token);
        Assert.Equal(JsonValueKind.Object, replication.RootElement.GetProperty("performance").ValueKind);

        using var replicationTasks = await client.CallTool(
            "get_replication_tasks",
            new { databaseName = fixture.DatabaseName },
            timeout.Token);
        Assert.Equal(JsonValueKind.Object, replicationTasks.RootElement.GetProperty("tasks").ValueKind);

        using var backupTasks = await client.CallTool(
            "get_backup_tasks",
            new { databaseName = fixture.DatabaseName },
            timeout.Token);
        Assert.Equal(JsonValueKind.Object, backupTasks.RootElement.GetProperty("tasks").ValueKind);

        using var etlTasks = await client.CallTool(
            "get_etl_tasks",
            new { databaseName = fixture.DatabaseName },
            timeout.Token);
        Assert.Equal(JsonValueKind.Object, etlTasks.RootElement.GetProperty("tasks").ValueKind);

        using var ongoingTasks = await client.CallTool(
            "list_ongoing_tasks",
            new { databaseName = fixture.DatabaseName },
            timeout.Token);
        Assert.Equal(JsonValueKind.Object, ongoingTasks.RootElement.GetProperty("tasks").ValueKind);

        using var subscriptions = await client.CallTool(
            "get_subscriptions",
            new { databaseName = fixture.DatabaseName },
            timeout.Token);
        Assert.Equal(JsonValueKind.Array, subscriptions.RootElement.GetProperty("subscriptions").ValueKind);

        using var tcp = await client.CallTool(
            "get_database_tcp_info",
            new { databaseName = fixture.DatabaseName, nodeTag },
            timeout.Token);
        Assert.Equal(JsonValueKind.Object, tcp.RootElement.GetProperty("tcp").ValueKind);

        using var identities = await client.CallTool(
            "get_identities",
            new { databaseName = fixture.DatabaseName },
            timeout.Token);
        Assert.Equal(JsonValueKind.Object, identities.RootElement.GetProperty("identities").ValueKind);

        using var storageOverview = await client.CallTool(
            "get_storage_overview",
            new { databaseName = fixture.DatabaseName },
            timeout.Token);
        Assert.Equal(JsonValueKind.Object, storageOverview.RootElement.GetProperty("report").ValueKind);

        using var storageTrees = await client.CallTool(
            "get_storage_trees",
            new { databaseName = fixture.DatabaseName },
            timeout.Token);
        Assert.Equal(JsonValueKind.Object, storageTrees.RootElement.GetProperty("trees").ValueKind);

        using var storageTreeStructure = await client.CallTool(
            "get_storage_tree_structure",
            new
            {
                databaseName = fixture.DatabaseName,
                treeName = "Docs",
                treeKind = (string?)null
            },
            timeout.Token);
        Assert.NotEmpty(storageTreeStructure.RootElement.GetProperty("structure").GetString()!);

        using var performanceOverview = await client.CallTool("get_performance_overview", null, timeout.Token);
        Assert.Equal(JsonValueKind.Object, performanceOverview.RootElement.GetProperty("metrics").ValueKind);

        using var cpuStats = await client.CallTool("get_cpu_stats", null, timeout.Token);
        Assert.Equal(JsonValueKind.Object, cpuStats.RootElement.GetProperty("cpu").ValueKind);

        using var processStats = await client.CallTool("get_process_stats", null, timeout.Token);
        Assert.Equal(JsonValueKind.Object, processStats.RootElement.GetProperty("process").ValueKind);

        using var threadStats = await client.CallTool("get_thread_stats", null, timeout.Token);
        Assert.Equal(JsonValueKind.Array, threadStats.RootElement.GetProperty("threads").ValueKind);

        using var tcpStats = await client.CallTool("get_tcp_stats", null, timeout.Token);
        Assert.Equal(JsonValueKind.Object, tcpStats.RootElement.GetProperty("tcp").ValueKind);
    }

    private static readonly string[] ExpectedToolNames =
    [
        "get_backup_status",
        "get_backup_tasks",
        "get_client_configuration",
        "get_cluster_topology",
        "get_collection_stats",
        "get_cpu_stats",
        "get_database_configuration",
        "get_database_health_summary",
        "get_database_record",
        "get_database_stats",
        "get_database_tcp_info",
        "get_detailed_collection_stats",
        "get_detailed_database_stats",
        "get_etl_task_info",
        "get_etl_tasks",
        "get_encryption_buffer_pool_stats",
        "get_gc_memory_stats",
        "get_identities",
        "get_index",
        "get_index_errors",
        "get_index_performance",
        "get_index_stats",
        "get_index_terms",
        "get_indexing_status",
        "get_io_stats",
        "get_logs_configuration",
        "get_low_memory_log",
        "get_node_status",
        "get_ongoing_task_info",
        "get_operation_state",
        "get_os_memory_stats",
        "get_performance_overview",
        "get_process_stats",
        "get_replication_performance",
        "get_replication_tasks",
        "get_script_runners",
        "get_server_info",
        "get_server_wide_client_configuration",
        "get_stack_traces",
        "get_storage_compression_dictionaries",
        "get_storage_environment_report",
        "get_storage_free_space_snapshot",
        "get_storage_overview",
        "get_storage_scratch_buffer_info",
        "get_storage_tree_structure",
        "get_storage_trees",
        "get_subscription_state",
        "get_subscriptions",
        "get_tcp_stats",
        "get_thread_stats",
        "list_databases",
        "list_indexes",
        "list_ongoing_tasks",
        "list_tcp_connections",
        "sample_runtime_events",
        "sample_thread_diagnostics"
    ];
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

        var result = response.RootElement.GetProperty("result");

        if (!result.TryGetProperty("structuredContent", out var structuredContent))
            throw new InvalidOperationException(response.RootElement.GetRawText());

        return JsonDocument.Parse(structuredContent.GetRawText());
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
