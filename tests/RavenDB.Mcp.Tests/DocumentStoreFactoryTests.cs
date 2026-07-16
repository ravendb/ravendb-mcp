using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using RavenDB.Mcp.Configuration;
using RavenDB.Mcp.RavenDB;

namespace RavenDB.Mcp.Tests;

public sealed class DocumentStoreFactoryTests
{
    [Fact]
    public async Task LoadsPfxCertificateFromConfiguration()
    {
        var password = "ravendb-mcp-test";
        var certificatePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.pfx");

        await File.WriteAllBytesAsync(certificatePath, CreateTestCertificate(password));

        try
        {
            using var store = DocumentStoreFactory.Create(new RavenDbOptions
            {
                Urls = ["https://127.0.0.1:8080/"],
                CertificatePath = certificatePath,
                CertificatePassword = password
            });

            Assert.NotNull(store.Certificate);
            Assert.True(store.Certificate.HasPrivateKey);
        }
        finally
        {
            File.Delete(certificatePath);
        }
    }

    [Fact]
    public async Task WrongPasswordThrowsClearError()
    {
        var certificatePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.pfx");
        await File.WriteAllBytesAsync(certificatePath, CreateTestCertificate("ravendb-mcp-test"));

        try
        {
            var ex = Assert.Throws<InvalidOperationException>(() => DocumentStoreFactory.LoadCertificate(new RavenDbOptions
            {
                CertificatePath = certificatePath,
                CertificatePassword = "wrong-password"
            }));

            Assert.Contains("password is missing or incorrect", ex.Message);
            Assert.IsType<CryptographicException>(ex.InnerException);
        }
        finally
        {
            File.Delete(certificatePath);
        }
    }

    [Fact]
    public async Task EncryptedMarkerPasswordExplainsClaudeDesktop()
    {
        var certificatePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.pfx");
        await File.WriteAllBytesAsync(certificatePath, CreateTestCertificate("ravendb-mcp-test"));

        try
        {
            var ex = Assert.Throws<InvalidOperationException>(() => DocumentStoreFactory.LoadCertificate(new RavenDbOptions
            {
                CertificatePath = certificatePath,
                CertificatePassword = "__encrypted__:djEwabc123=="
            }));

            Assert.Contains("still encrypted", ex.Message);
            Assert.Contains("__encrypted__:", ex.Message);
            Assert.IsType<CryptographicException>(ex.InnerException);
        }
        finally
        {
            File.Delete(certificatePath);
        }
    }

    private static byte[] CreateTestCertificate(string password)
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=RavenDB MCP Test",
            key,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(
            X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
            false));
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
            new OidCollection
            {
                new("1.3.6.1.5.5.7.3.1"),
                new("1.3.6.1.5.5.7.3.2")
            },
            false));

        using var certificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(1));

        return certificate.Export(X509ContentType.Pkcs12, password);
    }
}
