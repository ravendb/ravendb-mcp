using System.Text.Json;
using ModelContextProtocol.Server;
using RavenDB.Mcp.RavenDB;

namespace RavenDB.Mcp.Tools;

[McpServerToolType]
public static class DatabaseActivityTools
{
    [McpServerTool(Name = "get_subscriptions", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetSubscriptionsResult> GetSubscriptions(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetSubscriptions(databaseName, cancellationToken);
    }

    [McpServerTool(Name = "get_subscription_connection_details", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetSubscriptionConnectionDetailsResult> GetSubscriptionConnectionDetails(
        RavenDbAdminClient client,
        string databaseName,
        string subscriptionName,
        CancellationToken cancellationToken)
    {
        return client.GetSubscriptionConnectionDetails(databaseName, subscriptionName, cancellationToken);
    }

    [McpServerTool(Name = "get_notification_center_alerts", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetNotificationCenterAlertsResult> GetNotificationCenterAlerts(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetNotificationCenterAlerts(databaseName, cancellationToken);
    }

    [McpServerTool(Name = "get_database_tcp_info", ReadOnly = true, UseStructuredContent = true)]
    public static Task<GetDatabaseTcpInfoResult> GetDatabaseTcpInfo(
        RavenDbAdminClient client,
        string databaseName,
        CancellationToken cancellationToken)
    {
        return client.GetDatabaseTcpInfo(databaseName, cancellationToken);
    }
}

public sealed record GetSubscriptionsResult(string DatabaseName, JsonElement Subscriptions);

public sealed record GetSubscriptionConnectionDetailsResult(
    string DatabaseName,
    string SubscriptionName,
    JsonElement ConnectionDetails);

public sealed record GetNotificationCenterAlertsResult(string DatabaseName, JsonElement Alerts);

public sealed record GetDatabaseTcpInfoResult(string DatabaseName, JsonElement Tcp);
