using Itenium.SkillForge.Data;
using Itenium.SkillForge.Entities.Consultants;
using Itenium.SkillForge.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.WebApi.Tests;

/// <summary>
/// Tests verifying that GET /api/consultants respects team scope (issue #52).
/// </summary>
[TestFixture]
public class TeamMembersTests : DatabaseTestBase
{
    private const int TeamA = 1;
    private const int TeamB = 2;

    [SetUp]
    public async Task SetUp()
    {
        await SkillCatalogueSeedData.Seed(Db);
    }

    [Test]
    public async Task GetTeamMembers_ManagerFromTeamA_SeesOnlyOwnTeamConsultants()
    {
        var consultantA = new ConsultantEntity { UserId = "user-tm-a1", TeamId = TeamA };
        var consultantB = new ConsultantEntity { UserId = "user-tm-b1", TeamId = TeamB };
        Db.Consultants.AddRange(consultantA, consultantB);
        await Db.SaveChangesAsync();

        var scope = new FakeTeamQueryScope(teamIds: [TeamA]);
        var controller = new ConsultantsController(new ProfileService(Db), scope, new RoadmapService(Db), new GoalService(Db), new ReadinessFlagService(Db), Db, new SkillValidationService(Db));

        var result = await controller.GetTeamMembers();

        var ok = (OkObjectResult)result;
        var members = (IReadOnlyList<TeamMemberResponse>)ok.Value!;
        Assert.That(members, Has.Count.EqualTo(1));
        Assert.That(members[0].TeamId, Is.EqualTo(TeamA));
    }

    [Test]
    public async Task GetTeamMembers_BackofficeUser_SeesConsultantsFromAllTeams()
    {
        var consultantA = new ConsultantEntity { UserId = "user-tm-a2", TeamId = TeamA };
        var consultantB = new ConsultantEntity { UserId = "user-tm-b2", TeamId = TeamB };
        Db.Consultants.AddRange(consultantA, consultantB);
        await Db.SaveChangesAsync();

        var scope = new FakeTeamQueryScope(isBackOffice: true);
        var controller = new ConsultantsController(new ProfileService(Db), scope, new RoadmapService(Db), new GoalService(Db), new ReadinessFlagService(Db), Db, new SkillValidationService(Db));

        var result = await controller.GetTeamMembers();

        var ok = (OkObjectResult)result;
        var members = (IReadOnlyList<TeamMemberResponse>)ok.Value!;
        Assert.That(members.Any(m => m.TeamId == TeamA), Is.True);
        Assert.That(members.Any(m => m.TeamId == TeamB), Is.True);
    }

    [Test]
    public async Task GetTeamMembers_ExcludesArchivedConsultants()
    {
        var active = new ConsultantEntity { UserId = "user-tm-active", TeamId = TeamA };
        var archived = new ConsultantEntity { UserId = "user-tm-archived", TeamId = TeamA, ArchivedAt = DateTime.UtcNow };
        Db.Consultants.AddRange(active, archived);
        await Db.SaveChangesAsync();

        var scope = new FakeTeamQueryScope(teamIds: [TeamA]);
        var controller = new ConsultantsController(new ProfileService(Db), scope, new RoadmapService(Db), new GoalService(Db), new ReadinessFlagService(Db), Db, new SkillValidationService(Db));

        var result = await controller.GetTeamMembers();

        var ok = (OkObjectResult)result;
        var members = (IReadOnlyList<TeamMemberResponse>)ok.Value!;
        Assert.That(members.Any(m => m.Id == archived.Id), Is.False);
        Assert.That(members.Any(m => m.Id == active.Id), Is.True);
    }

    [Test]
    public async Task GetTeamMembers_IncludesAssignedProfileName()
    {
        var dotnetProfile = await Db.CompetenceCentreProfiles
            .FirstAsync(p => p.Name == ".NET");
        var consultant = new ConsultantEntity { UserId = "user-tm-profile", TeamId = TeamA, ProfileId = dotnetProfile.Id };
        Db.Consultants.Add(consultant);
        await Db.SaveChangesAsync();

        var scope = new FakeTeamQueryScope(teamIds: [TeamA]);
        var controller = new ConsultantsController(new ProfileService(Db), scope, new RoadmapService(Db), new GoalService(Db), new ReadinessFlagService(Db), Db, new SkillValidationService(Db));

        var result = await controller.GetTeamMembers();

        var ok = (OkObjectResult)result;
        var members = (IReadOnlyList<TeamMemberResponse>)ok.Value!;
        var member = members.Single(m => m.Id == consultant.Id);
        Assert.That(member.ProfileName, Is.EqualTo(".NET"));
    }
}
