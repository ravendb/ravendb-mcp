using System.ComponentModel;
using System.Reflection;
using ModelContextProtocol.Server;
using RavenDB.Mcp.RavenDB;

namespace RavenDB.Mcp.Resources;

[McpServerResourceType]
public static class RqlResources
{
    [McpServerResource(UriTemplate = "rql://index", Name = "rql_index", MimeType = "text/markdown")]
    [Description("START HERE for RQL. Version-aware index of the query features available on this cluster; drill into a feature resource, escalate to rql://docs only as a last resort.")]
    public static async Task<string> Index(RavenDbAdminClient client, CancellationToken cancellationToken)
    {
        string version;
        try { version = await client.GetDocsVersion(cancellationToken); }
        catch { version = "?"; }
        return Read("index.md").Replace("{VERSION}", version, StringComparison.Ordinal);
    }

    [McpServerResource(UriTemplate = "rql://cheatsheet", Name = "rql_cheatsheet", MimeType = "text/markdown")]
    [Description("RQL syntax cheat-sheet: clause order, from/where/order-by/select, group-by aggregation, include/load, paging.")]
    public static string Cheatsheet() => Read("cheatsheet.md");

    [McpServerResource(UriTemplate = "rql://reference", Name = "rql_reference", MimeType = "text/markdown")]
    [Description("Shared RQL concepts: dynamic vs index queries, auto-indexes, staleness, projections, includes, gotchas, discovery loop.")]
    public static string Reference() => Read("reference.md");

    [McpServerResource(UriTemplate = "rql://spatial", Name = "rql_spatial", MimeType = "text/markdown")]
    [Description("Spatial RQL: filter by radius/shape (spatial.within/circle/wkt) and order by spatial.distance.")]
    public static string Spatial() => Read("spatial.md");

    [McpServerResource(UriTemplate = "rql://timeseries", Name = "rql_timeseries", MimeType = "text/markdown")]
    [Description("Time-series RQL: select/declare timeseries, between ranges, group-by buckets, aggregations.")]
    public static string TimeSeries() => Read("timeseries.md");

    [McpServerResource(UriTemplate = "rql://facets", Name = "rql_facets", MimeType = "text/markdown")]
    [Description("Faceted (aggregated) search RQL: facet by value/range with per-bucket aggregations over a static index.")]
    public static string Facets() => Read("facets.md");

    [McpServerResource(UriTemplate = "rql://functions", Name = "rql_functions", MimeType = "text/markdown")]
    [Description("Complete RQL method/function reference (from the server parser): where/aggregation/projection/spatial/timeseries methods with signatures.")]
    public static string Functions() => Read("functions.md");

    [McpServerResource(UriTemplate = "rql://fulltext", Name = "rql_fulltext", MimeType = "text/markdown")]
    [Description("Full-text search RQL: search(), boost, fuzzy, proximity, exact, wildcards, and analyzer behavior.")]
    public static string FullText() => Read("fulltext.md");

    [McpServerResource(UriTemplate = "rql://advanced", Name = "rql_advanced", MimeType = "text/markdown")]
    [Description("Advanced RQL: suggestions (suggest), more-like-this, highlighting, and vector/AI search (vector.search, 7.x+).")]
    public static string Advanced() => Read("advanced.md");

    [McpServerResource(UriTemplate = "rql://docs", Name = "rql_docs", MimeType = "text/markdown")]
    [Description("Official RavenDB RQL doc links for THIS cluster's version (auto-detected from the connected server). Last-resort escalation from the local rql:// resources.")]
    public static async Task<string> Docs(RavenDbAdminClient client, CancellationToken cancellationToken)
        => Render(await client.GetDocsVersion(cancellationToken), "docs-index.md");

    [McpServerResource(UriTemplate = "rql://docs/{version}", Name = "rql_docs_version", MimeType = "text/markdown")]
    [Description("Official RavenDB RQL doc links for a specific version, e.g. 6.2 / 7.1 / 7.2. Prefer rql://docs (auto-detects this cluster).")]
    public static string DocsForVersion(string version)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(version, @"^\d+\.\d+$"))
            throw new ArgumentException($"Version must be major.minor, e.g. '7.2' (got '{version}').", nameof(version));

        return Render(version, "docs-index.md");
    }

    private static string Render(string version, string file) => Read(file).Replace("{VERSION}", version, StringComparison.Ordinal);

    private static string Read(string file)
    {
        var assembly = typeof(RqlResources).Assembly;
        var name = Array.Find(assembly.GetManifestResourceNames(), n => n.EndsWith($".rql.{file}", StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"Embedded RQL resource '{file}' not found.");

        using var stream = assembly.GetManifestResourceStream(name)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
