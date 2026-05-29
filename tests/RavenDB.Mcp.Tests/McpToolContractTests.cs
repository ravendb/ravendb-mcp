using System.Text.Json;

namespace RavenDB.Mcp.Tests;

public sealed class McpToolContractTests
{
    [Fact]
    public async Task ExposesExpectedReadOnlyToolsWithoutConnectingToRavenDb()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await using var client = McpStdioClient.Start("http://127.0.0.1:1/");

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

        Assert.Equal(ToolCatalog.ExpectedToolNames.Order(), toolNames.Order());
        Assert.All(toolNames, name => Assert.Matches("^[a-z][a-z0-9]*(?:_[a-z0-9]+)*$", name!));
        Assert.All(toolsArray, tool => Assert.True(
            tool.GetProperty("annotations").GetProperty("readOnlyHint").GetBoolean()));
    }
}
