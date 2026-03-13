using Itenium.SkillForge.Entities.Consultants;
using Itenium.SkillForge.Services;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.WebApi.Tests;

/// <summary>
/// Integration tests verifying the global query filter on ConsultantEntity
/// that excludes archived consultants from all default queries.
/// </summary>
[TestFixture]
public class ConsultantQueryFilterTests : DatabaseTestBase
{
    [Test]
    public async Task Consultants_WhenNotArchived_AreIncludedInDefaultQuery()
    {
        Db.Consultants.Add(new ConsultantEntity { UserId = "user-1" });
        await Db.SaveChangesAsync();

        var result = await Db.Consultants.ToListAsync();

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].UserId, Is.EqualTo("user-1"));
    }

    [Test]
    public async Task Consultants_WhenArchived_AreExcludedFromDefaultQuery()
    {
        Db.Consultants.Add(new ConsultantEntity { UserId = "archived-user", ArchivedAt = DateTime.UtcNow });
        await Db.SaveChangesAsync();

        var result = await Db.Consultants.ToListAsync();

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task Consultants_MixedArchivedAndActive_ReturnsOnlyActive()
    {
        Db.Consultants.AddRange(
            new ConsultantEntity { UserId = "active-user-1" },
            new ConsultantEntity { UserId = "archived-user", ArchivedAt = DateTime.UtcNow },
            new ConsultantEntity { UserId = "active-user-2" });
        await Db.SaveChangesAsync();

        var result = await Db.Consultants.ToListAsync();

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result.Select(c => c.UserId), Contains.Item("active-user-1"));
        Assert.That(result.Select(c => c.UserId), Contains.Item("active-user-2"));
        Assert.That(result.Select(c => c.UserId), Does.Not.Contain("archived-user"));
    }

    [Test]
    public async Task Consultants_WhenArchivedWithIgnoreQueryFilters_ReturnsAll()
    {
        Db.Consultants.AddRange(
            new ConsultantEntity { UserId = "active-user" },
            new ConsultantEntity { UserId = "archived-user", ArchivedAt = DateTime.UtcNow });
        await Db.SaveChangesAsync();

        var result = await Db.Consultants.IgnoreQueryFilters().ToListAsync();

        Assert.That(result, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task Consultants_TeamScopeFilter_ManagerFromTeamA_CannotSeeTeamBConsultants()
    {
        const int teamA = 1;
        const int teamB = 2;

        Db.Consultants.AddRange(
            new ConsultantEntity { UserId = "team-a-consultant", TeamId = teamA },
            new ConsultantEntity { UserId = "team-b-consultant", TeamId = teamB });
        await Db.SaveChangesAsync();

        var teamAScope = new FakeTeamQueryScope(teamIds: [teamA]);
        var result = await Db.Consultants.ApplyTeamScope(teamAScope).ToListAsync();

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].UserId, Is.EqualTo("team-a-consultant"));
        Assert.That(result.Select(c => c.UserId), Does.Not.Contain("team-b-consultant"));
    }

    [Test]
    public async Task Consultants_TeamScopeFilter_BackOfficeUser_SeesAllTeams()
    {
        const int teamA = 1;
        const int teamB = 2;

        Db.Consultants.AddRange(
            new ConsultantEntity { UserId = "team-a-consultant", TeamId = teamA },
            new ConsultantEntity { UserId = "team-b-consultant", TeamId = teamB });
        await Db.SaveChangesAsync();

        var backOfficeScope = new FakeTeamQueryScope(isBackOffice: true);
        var result = await Db.Consultants.ApplyTeamScope(backOfficeScope).ToListAsync();

        Assert.That(result, Has.Count.EqualTo(2));
    }

}
