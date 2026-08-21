using System.Text.Json;
using RavenDB.Mcp.RavenDB;

namespace RavenDB.Mcp.Tests;

public class ServerSettingsIndexTests
{
    // Two prefixes, three settings, one of which the operator has actually set.
    private const string Settings = """
        {"Settings":[
          {"Metadata":{"Keys":["Indexing.MaxNumberOfConcurrentIndexes"],"DefaultValue":"4","Description":"Documentation that dwarfs the value and never changes."},"ServerValues":{}},
          {"Metadata":{"Keys":["Indexing.MapTimeoutInSec"],"DefaultValue":"30","Description":"More documentation."},"ServerValues":{"Value":"120"}},
          {"Metadata":{"Keys":["Http.MaxRequestBufferSize"],"DefaultValue":"1024","Description":"More documentation."},"ServerValues":{}}
        ]}
        """;

    private static string Filter(string? prefix, string json = Settings) =>
        RavenDbAdminClient.FilterServerSettings(
            JsonSerializer.Deserialize<JsonElement>(json), prefix).GetRawText();

    [Fact]
    public void IndexReportsWhereSomethingIsSetAndStillListsEveryPrefix()
    {
        var index = Filter(null);

        Assert.Contains("\"overriddenTotal\":1", index);
        Assert.Contains("\"totalEntries\":3", index);
        Assert.Contains("Indexing", index);
        Assert.Contains("Http", index);
    }

    [Fact]
    public void IndexSaysThereIsNothingToFetchWhenNothingIsSet()
    {
        var untouched = Settings.Replace("\"ServerValues\":{\"Value\":\"120\"}", "\"ServerValues\":{}");

        Assert.Contains("\"overriddenTotal\":0", Filter(null, untouched));
        Assert.Contains("Nothing is overridden", Filter(null, untouched));
    }

    [Fact]
    public void SeveralPrefixesComeBackInOneCall()
    {
        var both = Filter("Indexing,Http");

        // Http is entirely at its defaults; it must still be represented.
        Assert.Contains("MapTimeoutInSec", both);
        Assert.Contains("MaxRequestBufferSize", both);
    }

    [Fact]
    public void ASinglePrefixBehavesAsBefore()
    {
        var one = Filter("Indexing");

        Assert.Contains("MapTimeoutInSec", one);
        Assert.DoesNotContain("MaxRequestBufferSize", one);
        Assert.DoesNotContain("Documentation", one);
    }
}
