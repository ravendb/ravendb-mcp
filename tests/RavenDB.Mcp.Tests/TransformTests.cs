using System.Text.Json;
using RavenDB.Mcp.RavenDB;

namespace RavenDB.Mcp.Tests;

// Unit tests for the pure JSON transforms behind the progressive-disclosure tools.
public sealed class TransformTests
{
    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;

    [Fact]
    public void SummarizeRecordIndexesReducesDefinitionsToNames()
    {
        var record = RavenDbAdminClient.SummarizeRecordIndexes(Parse("""
        {
          "DatabaseName": "db",
          "Indexes": { "A/ByName": { "Maps": ["huge"] }, "B/ByCity": { "Maps": ["huge"] } },
          "AutoIndexes": { "Auto/Users/ByAge": { "Fields": {} } },
          "IndexesHistory": {}
        }
        """));

        var indexes = record.GetProperty("Indexes");
        Assert.Equal(2, indexes.GetProperty("Count").GetInt32());
        Assert.Equal("A/ByName", indexes.GetProperty("Names")[0].GetString());
        Assert.Equal(1, record.GetProperty("AutoIndexes").GetProperty("Count").GetInt32());
        Assert.Equal(JsonValueKind.Object, record.GetProperty("IndexesHistory").ValueKind); // empty → untouched
        Assert.True(record.TryGetProperty("IndexesHint", out _));
        Assert.Equal("db", record.GetProperty("DatabaseName").GetString());
    }

    [Fact]
    public void SummarizeRecordIndexesLeavesRecordWithoutIndexesAlone()
    {
        var record = RavenDbAdminClient.SummarizeRecordIndexes(Parse("""{ "DatabaseName": "db" }"""));
        Assert.False(record.TryGetProperty("IndexesHint", out _));
    }

    [Fact]
    public void SlimMetadataKeepsIdentityAndDropsTheRest()
    {
        var result = RavenDbAdminClient.SlimMetadata(Parse("""
        {
          "TotalResults": 2,
          "Results": [
            { "Name": "a", "@metadata": { "@id": "users/1", "@collection": "Users", "@change-vector": "cv", "@flags": "x" } },
            { "Name": "b", "@metadata": { "@projection": true } }
          ],
          "Includes": {
            "users/1": { "Name": "a", "@metadata": { "@id": "users/1", "@collection": "Users", "@change-vector": "cv" } }
          }
        }
        """));

        var first = result.GetProperty("Results")[0].GetProperty("@metadata");
        Assert.Equal("users/1", first.GetProperty("@id").GetString());
        Assert.Equal("Users", first.GetProperty("@collection").GetString());
        Assert.False(first.TryGetProperty("@change-vector", out _));

        // No identity keys at all → metadata removed entirely.
        Assert.False(result.GetProperty("Results")[1].TryGetProperty("@metadata", out _));

        var include = result.GetProperty("Includes").GetProperty("users/1").GetProperty("@metadata");
        Assert.False(include.TryGetProperty("@change-vector", out _));
    }

    [Theory]
    [InlineData("Raven.Server.Documents.Queries.Parser.ParseException", true)]
    [InlineData("Raven.Client.Exceptions.InvalidQueryException", true)]
    [InlineData("Raven.Client.Exceptions.Documents.Indexes.IndexDoesNotExistException", true)]
    [InlineData("Raven.Client.Exceptions.Database.DatabaseLoadFailureException", false)]
    public void TryQueryErrorRecognizesCallerFixableTypes(string type, bool expected)
    {
        var body = $$"""{ "Type": "{{type}}", "Message": "boom" }""";
        Assert.Equal(expected, RavenDbAdminClient.TryQueryError(500, body, out var error));
        if (expected)
        {
            Assert.Equal("boom", error.GetProperty("Error").GetString());
            Assert.Contains("rql://", error.GetProperty("Hint").GetString());
        }
    }

    [Fact]
    public void TryQueryErrorTreatsBodyless404AsMissingIndex()
    {
        Assert.True(RavenDbAdminClient.TryQueryError(404, "", out var error));
        Assert.Contains("does not exist", error.GetProperty("Error").GetString());
    }

    [Fact]
    public void TryQueryErrorIgnoresNonJsonAndUntypedBodies()
    {
        Assert.False(RavenDbAdminClient.TryQueryError(500, "<html>gateway timeout</html>", out _));
        Assert.False(RavenDbAdminClient.TryQueryError(500, """{ "Message": "no type" }""", out _));
        Assert.False(RavenDbAdminClient.TryQueryError(503, "", out _)); // bodyless non-404 is a server fault
    }

    private const string RunawayJson = """
    {
      "Runaway Threads": {
        "TotalCpuUsage": 42,
        "List": [
          { "Id": 1, "Name": "Indexing of A", "CpuUsage": 5.0, "IoStats": { "big": true } },
          { "Id": 2, "Name": "Backup", "CpuUsage": 9.0, "IoStats": { "big": true } },
          { "Id": 3, "Name": "Indexing of B", "CpuUsage": 7.0, "IoStats": { "big": true } },
          { "Id": 4, "Name": "GC", "CpuUsage": 1.0, "IoStats": { "big": true } },
          { "Id": 5, "Name": "TxMerger", "CpuUsage": 3.0, "IoStats": { "big": true } },
          { "Id": 6, "Name": "Follower", "CpuUsage": 2.0, "IoStats": { "big": true } },
          { "Id": 7, "Name": "Indexing of C", "CpuUsage": 8.0, "IoStats": { "big": true } }
        ]
      }
    }
    """;

    [Fact]
    public void SummarizeRunawayThreadsReturnsTopByCpuPlusCompactIndex()
    {
        var summary = Parse(RavenDbAdminClient.SummarizeRunawayThreads(RunawayJson, null));

        var top = summary.GetProperty("TopByCpu");
        Assert.Equal(5, top.GetArrayLength());
        Assert.Equal("Backup", top[0].GetProperty("Name").GetString()); // 9.0 first
        Assert.True(top[0].TryGetProperty("IoStats", out _));           // top entries stay full

        var all = summary.GetProperty("AllThreads");
        Assert.Equal(7, all.GetArrayLength());
        Assert.False(all[0].TryGetProperty("IoStats", out _));          // index entries are compact

        Assert.Equal(42, summary.GetProperty("TotalCpuUsage").GetInt32());
        Assert.True(summary.TryGetProperty("Hint", out _));
    }

    [Fact]
    public void SummarizeRunawayThreadsFiltersByNamePrefix()
    {
        var summary = Parse(RavenDbAdminClient.SummarizeRunawayThreads(RunawayJson, "indexing"));

        Assert.Equal(3, summary.GetProperty("MatchCount").GetInt32());
        var matching = summary.GetProperty("MatchingThreads");
        Assert.Equal("Indexing of C", matching[0].GetProperty("Name").GetString()); // sorted by CPU desc
        Assert.True(matching[0].TryGetProperty("IoStats", out _));
    }

    [Theory]
    [InlineData("not json at all")]
    [InlineData("[1,2,3]")]
    [InlineData("""{ "SomethingElse": true }""")]
    public void SummarizeRunawayThreadsPassesUnknownShapesThrough(string json)
    {
        Assert.Equal(json, RavenDbAdminClient.SummarizeRunawayThreads(json, null));
    }

    private const string SettingsJson = """
    {
      "available": true,
      "value": {
        "Settings": [
          { "Metadata": { "Keys": ["Indexing.MapTimeoutInSec"] }, "ServerValues": {} },
          { "Metadata": { "Keys": ["Indexing.MaxTimeForDocumentTransactionToRemainOpenInSec"] }, "ServerValues": {} },
          { "Metadata": { "Keys": ["Storage.TempPath"] }, "ServerValues": {} }
        ]
      }
    }
    """;

    [Fact]
    public void FilterServerSettingsWithoutPrefixReturnsPrefixIndex()
    {
        var result = RavenDbAdminClient.FilterServerSettings(Parse(SettingsJson), null);

        Assert.Equal(3, result.GetProperty("totalEntries").GetInt32());
        Assert.Equal(2, result.GetProperty("prefixes").GetProperty("Indexing").GetInt32());
        Assert.Equal(1, result.GetProperty("prefixes").GetProperty("Storage").GetInt32());
        Assert.True(result.TryGetProperty("hint", out _));
    }

    [Fact]
    public void FilterServerSettingsWithPrefixReturnsMatchingEntries()
    {
        var result = RavenDbAdminClient.FilterServerSettings(Parse(SettingsJson), "indexing");

        Assert.Equal(3, result.GetProperty("totalEntries").GetInt32());
        Assert.Equal(2, result.GetProperty("matched").GetInt32());
        Assert.Equal(2, result.GetProperty("settings").GetArrayLength());
    }

    [Fact]
    public void FilterServerSettingsPassesUnavailableResultThrough()
    {
        var error = Parse("""{ "available": false, "error": "403" }""");
        var result = RavenDbAdminClient.FilterServerSettings(error, null);
        Assert.False(result.GetProperty("available").GetBoolean());
    }
}
