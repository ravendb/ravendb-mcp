using Microsoft.Extensions.Options;

namespace RavenDB.Mcp.Configuration;

public sealed class RavenDbOptionsValidator : IValidateOptions<RavenDbOptions>
{
    public ValidateOptionsResult Validate(string? name, RavenDbOptions options)
    {
        if (options.Urls.Length == 0 || options.Urls.Any(string.IsNullOrWhiteSpace))
            return ValidateOptionsResult.Fail("At least one RavenDB URL must be configured.");

        return ValidateOptionsResult.Success;
    }
}
