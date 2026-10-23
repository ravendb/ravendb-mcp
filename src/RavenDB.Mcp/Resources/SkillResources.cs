using System.Text.RegularExpressions;
using ModelContextProtocol.Server;

namespace RavenDB.Mcp.Resources;

// The ravendb agent skill (github.com/ravendb/skills, vendored as the external/skills submodule), served file by
// file under skill://ravendb/, the layout of the MCP Skills extension (SEP-2640). Relative links inside the skill
// resolve against skill://ravendb/.
public static class SkillResources
{
    private const string SkillUri = "skill://ravendb/SKILL.md";

    private const string ManifestPrefix = "skill/";

    internal static IReadOnlyDictionary<string, string> Files { get; } = Load();

    public static IEnumerable<McpServerResource> Create() => Files.Select(file => McpServerResource.Create(
        () => file.Value,
        new McpServerResourceCreateOptions
        {
            UriTemplate = file.Key,
            Name = file.Key["skill://".Length..],
            MimeType = "text/markdown",
            Description = file.Key == SkillUri ? FrontmatterDescription(file.Value) : null,
        }));

    private static Dictionary<string, string> Load()
    {
        var assembly = typeof(SkillResources).Assembly;
        return assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(ManifestPrefix, StringComparison.Ordinal))
            .ToDictionary(
                name => "skill://" + name[ManifestPrefix.Length..].Replace('\\', '/'),
                name =>
                {
                    using var reader = new StreamReader(assembly.GetManifestResourceStream(name)!);
                    return reader.ReadToEnd();
                });
    }

    private static string FrontmatterDescription(string skill) =>
        Regex.Match(skill, "^description: (.+)$", RegexOptions.Multiline) is { Success: true } match
            ? match.Groups[1].Value.Trim()
            : throw new InvalidOperationException($"{SkillUri} has no description in its frontmatter.");
}
