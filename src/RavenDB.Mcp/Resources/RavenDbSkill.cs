using ModelContextProtocol.Extensions.Skills;

namespace RavenDB.Mcp.Resources;

// The ravendb agent skill (github.com/ravendb/skills, vendored as the external/skills submodule), served through the
// MCP Skills extension: skills/list and skills/get carry its manifest, and every file is a resource under skill://ravendb/.
public static class RavenDbSkill
{
    private const string ManifestPrefix = "skill/ravendb/";

    internal static IReadOnlyList<McpServerSkillFile> Files { get; } = Load();

    public static McpServerSkillPackage Create() => McpServerSkillPackage.Create(Files);

    private static McpServerSkillFile[] Load()
    {
        var assembly = typeof(RavenDbSkill).Assembly;
        return assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(ManifestPrefix, StringComparison.Ordinal))
            .Select(name =>
            {
                using var stream = assembly.GetManifestResourceStream(name)!;
                using var content = new MemoryStream();
                stream.CopyTo(content);
                return new McpServerSkillFile { Path = name[ManifestPrefix.Length..].Replace('\\', '/'), Content = content.ToArray() };
            })
            .ToArray();
    }
}
