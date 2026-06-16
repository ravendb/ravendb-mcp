using RavenDB.Mcp.Tools;

namespace RavenDB.Mcp.Tests;

// The shared invariant every include[]-style facet tool relies on: Facet.Resolve picks the supplied
// defaults when nothing is requested (null OR empty array) and otherwise honours the request, deduped.
// Previously proven only in the external JS exerciser — pulled into dotnet test / CI here.
public sealed class FacetDispatchTests
{
    [Fact]
    public void ResolveUsesDefaultsWhenRequestedIsNull()
    {
        var resolved = Facet.Resolve<DatabaseStatsInclude>(
            null, DatabaseStatsInclude.Summary, DatabaseStatsInclude.Collections, DatabaseStatsInclude.Indexing);

        Assert.True(resolved.SetEquals([
            DatabaseStatsInclude.Summary, DatabaseStatsInclude.Collections, DatabaseStatsInclude.Indexing]));
    }

    [Fact]
    public void ResolveUsesDefaultsWhenRequestedIsEmpty()
    {
        var resolved = Facet.Resolve([], DocumentInclude.Document);

        Assert.True(resolved.SetEquals([DocumentInclude.Document]));
    }

    [Fact]
    public void ResolveHonoursRequestedSelectorsAndDedupes()
    {
        // Requested overrides defaults entirely, and duplicates collapse (HashSet).
        var resolved = Facet.Resolve(
            [IndexInclude.Definition, IndexInclude.Definition, IndexInclude.Errors],
            IndexInclude.Staleness);

        Assert.True(resolved.SetEquals([IndexInclude.Definition, IndexInclude.Errors]));
        Assert.DoesNotContain(IndexInclude.Staleness, resolved); // defaults ignored when a request is present
        Assert.Equal(2, resolved.Count);
    }
}
