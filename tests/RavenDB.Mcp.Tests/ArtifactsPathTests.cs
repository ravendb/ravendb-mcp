using RavenDB.Mcp.Configuration;
using RavenDB.Mcp.RavenDB;

namespace RavenDB.Mcp.Tests;

public sealed class ArtifactsPathTests
{
    [Fact]
    public void DefaultsToTempFolderWhenUnset()
    {
        var expected = Path.Combine(Path.GetTempPath(), "ravendb-mcp-artifacts");

        Assert.Equal(expected, RavenDbAdminClient.ResolveArtifactsPath(new RavenDbOptions()));
        Assert.Equal(expected, RavenDbAdminClient.ResolveArtifactsPath(new RavenDbOptions { ArtifactsPath = "  " }));
        Assert.Equal(expected, RavenDbAdminClient.ResolveArtifactsPath(null));
    }

    [Fact]
    public void UsesConfiguredPathVerbatim()
    {
        var configured = Path.Combine(Path.GetTempPath(), "custom-artifacts-location");

        Assert.Equal(configured, RavenDbAdminClient.ResolveArtifactsPath(new RavenDbOptions { ArtifactsPath = configured }));
    }
}
