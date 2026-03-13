using NSubstitute;

namespace Itenium.SkillForge.Services.Tests;

[TestFixture]
public class TeamQueryScopeTests
{
    private ISkillForgeUser _user = null!;

    [SetUp]
    public void Setup()
    {
        _user = Substitute.For<ISkillForgeUser>();
    }

    [Test]
    public void IsBackOffice_WhenUserIsBackOffice_ReturnsTrue()
    {
        _user.IsBackOffice.Returns(true);
        var sut = new TeamQueryScope(_user);

        Assert.That(sut.IsBackOffice, Is.True);
    }

    [Test]
    public void IsBackOffice_WhenUserIsNotBackOffice_ReturnsFalse()
    {
        _user.IsBackOffice.Returns(false);
        var sut = new TeamQueryScope(_user);

        Assert.That(sut.IsBackOffice, Is.False);
    }

    [Test]
    public void TeamIds_ReturnsSameAsUserTeams()
    {
        ICollection<int> teams = [1, 2, 3];
        _user.Teams.Returns(teams);
        var sut = new TeamQueryScope(_user);

        Assert.That(sut.TeamIds, Is.EquivalentTo(teams));
    }

    [Test]
    public void CanAccessTeam_WhenBackOffice_AlwaysReturnsTrue()
    {
        _user.IsBackOffice.Returns(true);
        _user.Teams.Returns([]);
        var sut = new TeamQueryScope(_user);

        Assert.That(sut.CanAccessTeam(42), Is.True);
    }

    [Test]
    public void CanAccessTeam_WhenTeamIdInUserTeams_ReturnsTrue()
    {
        _user.IsBackOffice.Returns(false);
        _user.Teams.Returns([1, 3, 5]);
        var sut = new TeamQueryScope(_user);

        Assert.That(sut.CanAccessTeam(3), Is.True);
    }

    [Test]
    public void CanAccessTeam_WhenTeamIdNotInUserTeams_ReturnsFalse()
    {
        _user.IsBackOffice.Returns(false);
        _user.Teams.Returns([1, 3, 5]);
        var sut = new TeamQueryScope(_user);

        Assert.That(sut.CanAccessTeam(2), Is.False);
    }

    [Test]
    public void CanAccessTeam_WhenUserHasNoTeams_ReturnsFalse()
    {
        _user.IsBackOffice.Returns(false);
        _user.Teams.Returns([]);
        var sut = new TeamQueryScope(_user);

        Assert.That(sut.CanAccessTeam(1), Is.False);
    }
}
