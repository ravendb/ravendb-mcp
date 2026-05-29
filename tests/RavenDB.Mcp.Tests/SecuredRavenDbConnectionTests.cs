using System.Text.Json;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Options;
using Raven.Client.ServerWide.Operations.Certificates;
using RavenDB.Mcp.Configuration;
using RavenDB.Mcp.RavenDB;

namespace RavenDB.Mcp.Tests;

public sealed class SecuredRavenDbConnectionTests
{
    private static readonly SemaphoreSlim RegistrationLock = new(1, 1);
    private static bool _registeredOperatorCertificate;

    [SecuredRavenDbFact]
    public async Task RavenDbClientConnectsWithOperatorPfx()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await RegisterOperatorCertificate(timeout.Token);

        var options = SecuredRavenDbTestSettings.OperatorOptions;
        using var store = DocumentStoreFactory.Create(options);
        var client = new RavenDbAdminClient(store, Options.Create(options));

        var serverInfo = await client.GetServerInfo(timeout.Token);

        Assert.NotEmpty(serverInfo.ProductVersion);
    }

    [SecuredRavenDbFact]
    public async Task McpServerConnectsWithOperatorPfxOverStdio()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await RegisterOperatorCertificate(timeout.Token);

        await using var client = McpStdioClient.Start(SecuredRavenDbTestSettings.OperatorOptions);

        await client.Initialize(timeout.Token);

        using var databases = await client.CallTool("list_databases", null, timeout.Token);

        Assert.Equal(JsonValueKind.Array, databases.RootElement.GetProperty("databases").ValueKind);
    }

    private static async Task RegisterOperatorCertificate(CancellationToken cancellationToken)
    {
        await RegistrationLock.WaitAsync(cancellationToken);

        try
        {
            if (_registeredOperatorCertificate)
                return;

            var adminOptions = SecuredRavenDbTestSettings.AdminOptions;
            var operatorOptions = SecuredRavenDbTestSettings.OperatorOptions;
            using var adminStore = DocumentStoreFactory.Create(adminOptions);
            var operatorCertificate = X509CertificateLoader.LoadPkcs12FromFile(
                operatorOptions.CertificatePath!,
                operatorOptions.CertificatePassword);

            await adminStore.Maintenance.Server.SendAsync(
                new PutClientCertificateOperation(
                    "ravendb-mcp-operator",
                    operatorCertificate,
                    new Dictionary<string, DatabaseAccess>(),
                    SecurityClearance.Operator),
                cancellationToken);

            _registeredOperatorCertificate = true;
        }
        finally
        {
            RegistrationLock.Release();
        }
    }
}
