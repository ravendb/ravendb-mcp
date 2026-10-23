using System.ComponentModel;
using System.Reflection;
using ModelContextProtocol.Server;
using RavenDB.Mcp.RavenDB;

namespace RavenDB.Mcp.Resources;

[McpServerResourceType]
public static class RqlResources
{
    [McpServerResource(UriTemplate = "rql://index", Name = "rql_index", MimeType = "text/markdown")]
    [Description("START HERE for RQL. Version-aware index that routes each query feature to its skill://ravendb reference; escalate to rql://docs only as a last resort.")]
    public static async Task<string> Index(RavenDbAdminClient client, CancellationToken cancellationToken)
    {
        string version;
        try { version = await client.GetDocsVersion(cancellationToken); }
        catch (Exception e) when (e is not OperationCanceledException) { version = "?"; }
        return Read("index.md").Replace("{VERSION}", version, StringComparison.Ordinal);
    }

    [McpServerResource(UriTemplate = "rql://docs", Name = "rql_docs", MimeType = "text/markdown")]
    [Description("Official RavenDB RQL doc links for THIS cluster's version (auto-detected from the connected server). Last-resort escalation from the skill://ravendb RQL references.")]
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

    internal static string Read(string file)
    {
        var assembly = typeof(RqlResources).Assembly;
        var name = Array.Find(assembly.GetManifestResourceNames(), n => n.EndsWith($".rql.{file}", StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"Embedded RQL resource '{file}' not found.");

        using var stream = assembly.GetManifestResourceStream(name)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
