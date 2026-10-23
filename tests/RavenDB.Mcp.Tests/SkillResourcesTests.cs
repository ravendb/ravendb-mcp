using System.ComponentModel;
using System.Reflection;
using System.Text.RegularExpressions;
using RavenDB.Mcp.RavenDB;
using RavenDB.Mcp.Resources;
using RavenDB.Mcp.Tools;

namespace RavenDB.Mcp.Tests;

// The skill is vendored from github.com/ravendb/skills; these catch a submodule bump that moves a file we point agents at.
public sealed class SkillResourcesTests
{
    [Fact]
    public void ServesTheSkillWithItsFrontmatter()
    {
        var skill = SkillResources.Files["skill://ravendb/SKILL.md"];
        Assert.StartsWith("---", skill);
        Assert.Contains("name: ravendb", skill);
    }

    [Fact]
    public void ServesEveryReferenceSkillMdRoutesTo()
    {
        var paths = Regex.Matches(SkillResources.Files["skill://ravendb/SKILL.md"], @"references/[\w/.-]+\.md")
            .Select(match => match.Value)
            .Distinct()
            .ToArray();

        Assert.NotEmpty(paths);
        Assert.All(paths, path => Assert.Contains("skill://ravendb/" + path, SkillResources.Files.Keys));
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
        Assert.All(uris, uri => Assert.Contains(uri, SkillResources.Files.Keys));
    }
}
