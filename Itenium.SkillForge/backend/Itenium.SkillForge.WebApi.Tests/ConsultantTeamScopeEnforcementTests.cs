using Itenium.SkillForge.Data;
using Itenium.SkillForge.Entities.Consultants;
using Itenium.SkillForge.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.WebApi.Tests;

/// <summary>
/// Integration tests verifying that team-scoped enforcement is applied at the service/controller
/// layer: a manager from Team A cannot access or modify Team B consultant data.
/// Acceptance criteria for issue #32.
/// </summary>
[TestFixture]
public class ConsultantTeamScopeEnforcementTests : DatabaseTestBase
{
    private const int TeamA = 1;
    private const int TeamB = 2;

    [SetUp]
    public async Task SetUp()
    {
        await SkillCatalogueSeedData.Seed(Db);
    }

    // ── ProfileService (repository layer) ───────────────────────────────────

    [Test]
    public async Task AssignProfile_ManagerFromTeamA_CannotAssignProfileToTeamBConsultant_ReturnsFalse()
    {
        var teamBConsultant = new ConsultantEntity { UserId = "team-b-user", TeamId = TeamB };
        Db.Consultants.Add(teamBConsultant);
        await Db.SaveChangesAsync();

        var dotnetId = (await Db.CompetenceCentreProfiles.FirstAsync(p => p.Name == ".NET")).Id;
        var teamAScope = new FakeTeamQueryScope(teamIds: [TeamA]);
        var service = new ProfileService(Db);

        var result = await service.AssignProfileToConsultantAsync(teamBConsultant.Id, dotnetId, teamAScope);

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task AssignProfile_ManagerFromTeamA_CanAssignProfileToOwnTeamConsultant_ReturnsTrue()
    {
        var teamAConsultant = new ConsultantEntity { UserId = "team-a-user", TeamId = TeamA };
        Db.Consultants.Add(teamAConsultant);
        await Db.SaveChangesAsync();

        var dotnetId = (await Db.CompetenceCentreProfiles.FirstAsync(p => p.Name == ".NET")).Id;
        var teamAScope = new FakeTeamQueryScope(teamIds: [TeamA]);
        var service = new ProfileService(Db);

        var result = await service.AssignProfileToConsultantAsync(teamAConsultant.Id, dotnetId, teamAScope);

        Assert.That(result, Is.True);
        var updated = await Db.Consultants.FindAsync(teamAConsultant.Id);
        Assert.That(updated!.ProfileId, Is.EqualTo(dotnetId));
    }

    [Test]
    public async Task AssignProfile_BackofficeUser_CanAssignProfileToAnyTeamConsultant_ReturnsTrue()
    {
        var teamBConsultant = new ConsultantEntity { UserId = "team-b-user-2", TeamId = TeamB };
        Db.Consultants.Add(teamBConsultant);
        await Db.SaveChangesAsync();

        var dotnetId = (await Db.CompetenceCentreProfiles.FirstAsync(p => p.Name == ".NET")).Id;
        var backofficeScope = new FakeTeamQueryScope(isBackOffice: true);
        var service = new ProfileService(Db);

        var result = await service.AssignProfileToConsultantAsync(teamBConsultant.Id, dotnetId, backofficeScope);

        Assert.That(result, Is.True);
    }

    // ── ArchivedAt global query filter ───────────────────────────────────────

    [Test]
    public async Task AssignProfile_ArchivedConsultant_IsExcludedByGlobalFilter_ReturnsFalse()
    {
        var archivedConsultant = new ConsultantEntity
        {
            UserId = "archived-user-scope",
            TeamId = TeamA,
            ArchivedAt = DateTime.UtcNow
        };
        Db.Consultants.Add(archivedConsultant);
        await Db.SaveChangesAsync();

        var dotnetId = (await Db.CompetenceCentreProfiles.FirstAsync(p => p.Name == ".NET")).Id;
        var teamAScope = new FakeTeamQueryScope(teamIds: [TeamA]);
        var service = new ProfileService(Db);

        // Archived consultant is invisible via the global query filter —
        // ApplyTeamScope operates on the already-filtered set, so this returns false.
        var result = await service.AssignProfileToConsultantAsync(archivedConsultant.Id, dotnetId, teamAScope);

        Assert.That(result, Is.False);
    }

    // ── Controller layer ─────────────────────────────────────────────────────

    [Test]
    public async Task AssignProfile_Controller_ManagerFromTeamA_CannotAccessTeamBConsultant_ReturnsNotFound()
    {
        var teamBConsultant = new ConsultantEntity { UserId = "team-b-user-ctrl", TeamId = TeamB };
        Db.Consultants.Add(teamBConsultant);
        await Db.SaveChangesAsync();

        var dotnetId = (await Db.CompetenceCentreProfiles.FirstAsync(p => p.Name == ".NET")).Id;
        var teamAScope = new FakeTeamQueryScope(teamIds: [TeamA]);
        var controller = new ConsultantsController(new ProfileService(Db), teamAScope, new RoadmapService(Db), Db);

        var result = await controller.AssignProfile(teamBConsultant.Id, new AssignProfileRequest(dotnetId));

        Assert.That(result, Is.TypeOf<NotFoundResult>());
    }
}
