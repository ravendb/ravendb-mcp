using RavenDB.Mcp.RavenDB;

namespace RavenDB.Mcp.Tests;

public class TrafficWatchFoldingTests
{
    private const string TwoIdenticalQueries = """
        {"TrafficWatchType":"Http","TimeStamp":"2026-08-04T14:24:38.6069597Z","DatabaseName":"reports","CustomInfo":"from Orders where Customer == $c limit 5\n{\"c\":\"customers/1\"}","ClientIP":"172.24.0.3","CertificateThumbprint":"23A437908D30438D97AE9D2F240A1443298110BE","RequestId":842,"HttpMethod":"POST","ElapsedMilliseconds":10,"ResponseStatusCode":200,"RequestUri":"https://x/databases/reports/queries?queryHash=1","AbsoluteUri":"https://x","RequestSizeInBytes":141,"ResponseSizeInBytes":7663,"Type":"Queries","QueryTimings":{"DurationInMs":6,"Timings":{"Optimizer":{"DurationInMs":0}}}}
        {"TrafficWatchType":"Http","TimeStamp":"2026-08-04T14:24:39.6069597Z","DatabaseName":"reports","CustomInfo":"from Orders where Customer == $c limit 5\n{\"c\":\"customers/2\"}","ClientIP":"172.24.0.3","CertificateThumbprint":"23A437908D30438D97AE9D2F240A1443298110BE","RequestId":843,"HttpMethod":"POST","ElapsedMilliseconds":20,"ResponseStatusCode":200,"RequestUri":"https://x/databases/reports/queries?queryHash=1","AbsoluteUri":"https://x","RequestSizeInBytes":141,"ResponseSizeInBytes":7663,"Type":"Queries","QueryTimings":{"DurationInMs":6,"Timings":{"Optimizer":{"DurationInMs":0}}}}
        """;

    [Fact]
    public void FoldsRepeatedRequestsIntoOneRowWithCounts()
    {
        var folded = RavenDbAdminClient.SummariseTrafficWatch(TwoIdenticalQueries);

        Assert.Contains("\"summarised\":true", folded);
        Assert.Contains("\"events\":2", folded);
        Assert.Contains("\"distinctShapes\":1", folded);
        // Timings survive the fold; the per-event noise does not.
        Assert.Contains("\"count\":2", folded);
        Assert.Contains("\"maxMs\":20", folded);
        Assert.DoesNotContain("CertificateThumbprint", folded);
        Assert.DoesNotContain("QueryTimings", folded);
    }

    [Fact]
    public void KeepsDistinctShapesApart()
    {
        var mixed = TwoIdenticalQueries + "\n" + """
            {"TrafficWatchType":"Http","DatabaseName":"reports","CustomInfo":"from Products limit 5","HttpMethod":"GET","ElapsedMilliseconds":3,"ResponseStatusCode":200,"ResponseSizeInBytes":100,"Type":"Queries"}
            """;

        var folded = RavenDbAdminClient.SummariseTrafficWatch(mixed);

        Assert.Contains("\"events\":3", folded);
        Assert.Contains("\"distinctShapes\":2", folded);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json at all")]
    [InlineData("{\"SomethingElse\":1}")]
    public void PassesThroughAnythingItDoesNotRecognise(string raw)
    {
        Assert.Equal(raw, RavenDbAdminClient.SummariseTrafficWatch(raw));
    }
}
