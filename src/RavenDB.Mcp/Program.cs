using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Raven.Client.Documents;
using RavenDB.Mcp.Configuration;
using RavenDB.Mcp.RavenDB;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services
    .AddOptions<RavenDbOptions>()
    .Bind(builder.Configuration)
    .ValidateOnStart();

builder.Services.AddSingleton<IValidateOptions<RavenDbOptions>, RavenDbOptionsValidator>();

builder.Services.AddSingleton<IDocumentStore>(serviceProvider =>
{
    var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<RavenDbOptions>>().Value;
    return DocumentStoreFactory.Create(options);
});

builder.Services.AddSingleton<RavenDbAdminClient>();

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithMessageFilters(filters => filters.AddIncomingFilter(next => async (context, cancellationToken) =>
    {
        if (context.JsonRpcMessage is JsonRpcNotification { Method: NotificationMethods.CancelledNotification, Params: { } parameters })
        {
            var cancelled = parameters.Deserialize<CancelledNotificationParams>();
            var logger = context.Services?.GetService<ILoggerFactory>()?.CreateLogger("RavenDB.Mcp.Cancellation");
            logger?.LogInformation("MCP request {RequestId} cancelled. Reason: {Reason}", cancelled?.RequestId, cancelled?.Reason);
        }

        await next(context, cancellationToken);
    }))
    .WithToolsFromAssembly();

await builder.Build().RunAsync();
