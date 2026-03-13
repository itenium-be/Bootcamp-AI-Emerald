using Itenium.SkillForge.Entities;
using Itenium.SkillForge.Services;
using Itenium.SkillForge.WebApi.Controllers;
using NSubstitute;

namespace Itenium.SkillForge.WebApi.Tests;

[TestFixture]
public class TeamControllerTests : DatabaseTestBase
{
    private ITeamQueryScope _teamScope = null!;
    private TeamController _sut = null!;

    [SetUp]
    public void Setup()
    {
        _teamScope = Substitute.For<ITeamQueryScope>();
        _sut = new TeamController(Db, _teamScope);
    }

    [Test]
    public async Task GetUserTeams_WhenBackOffice_ReturnsAllTeams()
    {
        Db.Teams.AddRange(
            new TeamEntity { Name = "Java" },
            new TeamEntity { Name = ".NET" },
            new TeamEntity { Name = "QA" });
        await Db.SaveChangesAsync();
        _teamScope.IsBackOffice.Returns(true);

        var result = await _sut.GetUserTeams();

        var teams = result.Value!;
        Assert.That(teams, Has.Count.EqualTo(3));
        Assert.That(teams.Select(t => t.Name), Contains.Item("Java"));
        Assert.That(teams.Select(t => t.Name), Contains.Item(".NET"));
        Assert.That(teams.Select(t => t.Name), Contains.Item("QA"));
    }

    [Test]
    public async Task GetUserTeams_WhenNotBackOffice_ReturnsOnlyUserTeams()
    {
        var javaTeam = new TeamEntity { Name = "Java" };
        var dotnetTeam = new TeamEntity { Name = ".NET" };
        var qaTeam = new TeamEntity { Name = "QA" };
        Db.Teams.AddRange(javaTeam, dotnetTeam, qaTeam);
        await Db.SaveChangesAsync();
        _teamScope.IsBackOffice.Returns(false);
        _teamScope.TeamIds.Returns(new[] { javaTeam.Id, qaTeam.Id });

        var result = await _sut.GetUserTeams();

        var teams = result.Value!;
        Assert.That(teams, Has.Count.EqualTo(2));
        Assert.That(teams.Select(t => t.Name), Contains.Item("Java"));
        Assert.That(teams.Select(t => t.Name), Contains.Item("QA"));
        Assert.That(teams.Select(t => t.Name), Does.Not.Contain(".NET"));
    }

    [Test]
    public async Task GetUserTeams_WhenNotBackOfficeAndNoTeams_ReturnsEmpty()
    {
        Db.Teams.AddRange(
            new TeamEntity { Name = "Java" },
            new TeamEntity { Name = ".NET" });
        await Db.SaveChangesAsync();
        _teamScope.IsBackOffice.Returns(false);
        _teamScope.TeamIds.Returns(Array.Empty<int>());

        var result = await _sut.GetUserTeams();

        var teams = result.Value!;
        Assert.That(teams, Is.Empty);
    }

    [Test]
    public async Task GetUserTeams_WhenUserHasNonExistentTeamId_IgnoresIt()
    {
        var javaTeam = new TeamEntity { Name = "Java" };
        Db.Teams.Add(javaTeam);
        await Db.SaveChangesAsync();
        _teamScope.IsBackOffice.Returns(false);
        _teamScope.TeamIds.Returns(new[] { javaTeam.Id, 999 });

        var result = await _sut.GetUserTeams();

        var teams = result.Value!;
        Assert.That(teams, Has.Count.EqualTo(1));
        Assert.That(teams.Select(t => t.Name), Contains.Item("Java"));
    }
}
