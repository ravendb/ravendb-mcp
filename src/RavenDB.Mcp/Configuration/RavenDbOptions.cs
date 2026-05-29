namespace RavenDB.Mcp.Configuration;

public sealed record RavenDbOptions
{
    public string[] Urls { get; init; } = [];

    public string? CertificatePath { get; init; }

    public string? CertificatePassword { get; init; }

    public string? ArtifactsPath { get; init; }
}
