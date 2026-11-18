using System.ComponentModel;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using ModelContextProtocol.Extensions.Skills;
using RavenDB.Mcp.RavenDB;
using RavenDB.Mcp.Resources;
using RavenDB.Mcp.Tools;

namespace RavenDB.Mcp.Tests;

// The skill is vendored from github.com/ravendb/skills; these catch a submodule bump that moves a file we point agents at.
public sealed class RavenDbSkillTests
{
    private static readonly Skill Entry = RavenDbSkill.Create().Skill.ProtocolSkill!;

    private static string[] ManifestUris => Entry.Resources.Entries!.Select(entry => entry.Uri).ToArray();

    [Fact]
    public void PublishesTheSkillWithAFullManifest()
    {
        Assert.Equal("skill://ravendb/SKILL.md", Entry.Uri);
        Assert.Equal("ravendb", Entry.Name);
        Assert.False(string.IsNullOrWhiteSpace(Entry.Description));
        Assert.Equal(RavenDbSkill.Files.Count, ManifestUris.Length);
    }

    [Fact]
    public void ServesEveryReferenceSkillMdRoutesTo()
    {
        var skillMd = Encoding.UTF8.GetString(RavenDbSkill.Files.Single(file => file.Path == "SKILL.md").Content.Span);
        var paths = Regex.Matches(skillMd, @"references/[\w/.-]+\.md")
            .Select(match => match.Value)
            .Distinct()
            .ToArray();

        Assert.NotEmpty(paths);
        Assert.All(paths, path => Assert.Contains("skill://ravendb/" + path, ManifestUris));
    }

    [Fact]
    public void ServesEverySkillUriWePointAgentsAt()
    {
        Assert.True(RavenDbAdminClient.TryQueryError(500, """{ "Type": "Raven.Client.Exceptions.InvalidQueryException", "Message": "boom" }""", out var error));
        var runQuery = typeof(DocumentDataTools).GetMethod(nameof(DocumentDataTools.RunQuery))!.GetCustomAttribute<DescriptionAttribute>()!.Description;
        var pointers = string.Join('\n', error.GetProperty("Hint").GetString(), runQuery);

        var uris = Regex.Matches(pointers, @"skill://[\w/.-]+\.md")
            .Select(match => match.Value)
            .Distinct()
            .ToArray();

        Assert.NotEmpty(uris);
        Assert.All(uris, uri => Assert.Contains(uri, ManifestUris));
    }
}
