using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Raven.Client.Documents;
using RavenDB.Mcp.Configuration;

namespace RavenDB.Mcp.RavenDB;

public static class DocumentStoreFactory
{
    // Claude Desktop stores a `"sensitive": true` user_config value encrypted and is meant to decrypt
    // it before substituting ${user_config.*} into the launch env. When it forwards the stored value
    // verbatim, the password arrives with this marker and the PKCS#12 load fails with an opaque
    // "password may be incorrect" error. Detect it so we can explain what actually happened.
    private const string EncryptedSecretMarker = "__encrypted__:";

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

        try
        {
            return X509CertificateLoader.LoadPkcs12FromFile(options.CertificatePath, options.CertificatePassword);
        }
        catch (CryptographicException ex)
        {
            if (options.CertificatePassword is not null && options.CertificatePassword.StartsWith(EncryptedSecretMarker, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    $"The RavenDB certificate password arrived still encrypted (it begins with \"{EncryptedSecretMarker}\"), " +
                    "so the certificate could not be opened. Claude Desktop forwarded the stored \"sensitive\" setting without " +
                    "decrypting it. Until that is fixed, use a certificate whose password is not marked sensitive, or an " +
                    "unencrypted certificate.", ex);

            throw new InvalidOperationException(
                $"The RavenDB certificate at \"{options.CertificatePath}\" could not be opened; the .pfx password is missing or incorrect.",
                ex);
        }
    }
}
