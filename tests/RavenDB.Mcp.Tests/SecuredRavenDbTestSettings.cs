using RavenDB.Mcp.Configuration;

namespace RavenDB.Mcp.Tests;

internal static class SecuredRavenDbTestSettings
{
    public const string UrlVariable = "RAVENDB_SECURE_TEST_URL";
    public const string AdminCertificatePathVariable = "RAVENDB_SECURE_ADMIN_CERTIFICATE_PATH";
    public const string AdminCertificatePasswordVariable = "RAVENDB_SECURE_ADMIN_CERTIFICATE_PASSWORD";
    public const string OperatorCertificatePathVariable = "RAVENDB_SECURE_OPERATOR_CERTIFICATE_PATH";
    public const string OperatorCertificatePasswordVariable = "RAVENDB_SECURE_OPERATOR_CERTIFICATE_PASSWORD";

    public static bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(UrlVariable)) &&
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(AdminCertificatePathVariable)) &&
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(AdminCertificatePasswordVariable)) &&
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(OperatorCertificatePathVariable)) &&
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(OperatorCertificatePasswordVariable));

    public static RavenDbOptions AdminOptions => new()
    {
        Urls = [GetRequired(UrlVariable)],
        CertificatePath = GetRequired(AdminCertificatePathVariable),
        CertificatePassword = GetRequired(AdminCertificatePasswordVariable)
    };

    public static RavenDbOptions OperatorOptions => new()
    {
        Urls = [GetRequired(UrlVariable)],
        CertificatePath = GetRequired(OperatorCertificatePathVariable),
        CertificatePassword = GetRequired(OperatorCertificatePasswordVariable)
    };

    public static string MissingMessage =>
        $"Set {UrlVariable}, admin certificate variables, and operator certificate variables to run secured RavenDB tests.";

    private static string GetRequired(string name)
    {
        return Environment.GetEnvironmentVariable(name)
            ?? throw new InvalidOperationException($"{name} must be set.");
    }
}

internal sealed class SecuredRavenDbFactAttribute : FactAttribute
{
    public SecuredRavenDbFactAttribute()
    {
        if (!SecuredRavenDbTestSettings.IsConfigured)
            Skip = SecuredRavenDbTestSettings.MissingMessage;
    }
}
