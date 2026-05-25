using System.Security.Cryptography.X509Certificates;
using Raven.Client.Documents;
using RavenDB.Mcp.Configuration;

namespace RavenDB.Mcp.RavenDB;

public static class DocumentStoreFactory
{
    public static IDocumentStore Create(RavenDbOptions options)
    {
        var store = new DocumentStore
        {
            Urls = options.Urls,
            Certificate = LoadCertificate(options)
        };

        store.Initialize();
        return store;
    }

    public static X509Certificate2? LoadCertificate(RavenDbOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.CertificatePath))
            return null;

        if (!File.Exists(options.CertificatePath))
            throw new InvalidOperationException($"RavenDB certificate file was not found: {options.CertificatePath}");

        return X509CertificateLoader.LoadPkcs12FromFile(options.CertificatePath, options.CertificatePassword);
    }
}
