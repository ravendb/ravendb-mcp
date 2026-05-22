using RavenDB.Mcp.Configuration;

namespace RavenDB.Mcp.Tests;

public sealed class RavenDbOptionsValidatorTests
{
    private readonly RavenDbOptionsValidator _validator = new();

    [Fact]
    public void RejectsMissingUrls()
    {
        var result = _validator.Validate(null, new RavenDbOptions());

        Assert.True(result.Failed);
    }

    [Fact]
    public void RejectsBlankUrls()
    {
        var result = _validator.Validate(null, new RavenDbOptions { Urls = [" "] });

        Assert.True(result.Failed);
    }

    [Fact]
    public void AcceptsConfiguredUrl()
    {
        var result = _validator.Validate(null, new RavenDbOptions { Urls = ["http://127.0.0.1:8070/"] });

        Assert.False(result.Failed);
    }
}
