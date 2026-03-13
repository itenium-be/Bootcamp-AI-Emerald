using Itenium.SkillForge.Data;
using Itenium.SkillForge.Entities.Coaching;
using Itenium.SkillForge.Entities.Consultants;
using Itenium.SkillForge.Entities.Goals;
using Itenium.SkillForge.Entities.Profiles;
using Itenium.SkillForge.Entities.Skills;
using Itenium.SkillForge.Services.Roadmap;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.WebApi.Tests;

/// <summary>
/// Integration tests for <see cref="RoadmapService"/>.
/// Issues #13 (roadmap computation), #14 (prerequisite warnings), #15 (seniority progress).
/// </summary>
[TestFixture]
public class RoadmapServiceTests : DatabaseTestBase
{
    private RoadmapService _service = null!;

    // Stable fake user ID used across test helpers
    private const string UserId = "test-user-001";

    [SetUp]
    public void SetUp()
    {
        _service = new RoadmapService(Db);
    }

    // ── Null-guard: consultant / profile ─────────────────────────────────────

    [Test]
    public async Task GetRoadmap_ReturnsNull_WhenConsultantNotFound()
    {
        var result = await _service.GetRoadmapAsync(int.MaxValue);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetRoadmap_ReturnsNull_WhenNoProfileAssigned()
    {
        var consultant = await CreateConsultantAsync(profileId: null);

        var result = await _service.GetRoadmapAsync(consultant.Id);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetSeniorityProgress_ReturnsNull_WhenConsultantNotFound()
    {
        var result = await _service.GetSeniorityProgressAsync(int.MaxValue);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetSeniorityProgress_ReturnsNull_WhenNoProfileAssigned()
    {
        var consultant = await CreateConsultantAsync(profileId: null);

        var result = await _service.GetSeniorityProgressAsync(consultant.Id);

        Assert.That(result, Is.Null);
    }

    // ── Roadmap: basic correctness ────────────────────────────────────────────

    [Test]
    public async Task GetRoadmap_DefaultView_ReturnsSkillsWithNoPrerequisites()
    {
        var (consultant, _, skills) = await SetUpProfileWithConsultantAsync();
        // No validations — starter skills (no prerequisites) should appear

        var result = await _service.GetRoadmapAsync(consultant.Id);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Any(n => n.SkillId == skills.Starter.Id), Is.True);
    }

    [Test]
    public async Task GetRoadmap_SetsCurrentNiveau_FromValidations()
    {
        var (consultant, _, skills) = await SetUpProfileWithConsultantAsync();
        await CreateValidationAsync(consultant.UserId, skills.Starter.Id, niveau: 3);

        var result = await _service.GetRoadmapAsync(consultant.Id, full: true);

        var node = result!.Single(n => n.SkillId == skills.Starter.Id);
        Assert.That(node.CurrentNiveau, Is.EqualTo(3));
    }

    [Test]
    public async Task GetRoadmap_TakesMaxNiveau_WhenMultipleValidationsExist()
    {
        var (consultant, _, skills) = await SetUpProfileWithConsultantAsync();
        await CreateValidationAsync(consultant.UserId, skills.Starter.Id, niveau: 2);
        await CreateValidationAsync(consultant.UserId, skills.Starter.Id, niveau: 4);

        var result = await _service.GetRoadmapAsync(consultant.Id, full: true);

        var node = result!.Single(n => n.SkillId == skills.Starter.Id);
        Assert.That(node.CurrentNiveau, Is.EqualTo(4));
    }

    [Test]
    public async Task GetRoadmap_CurrentNiveauIsZero_WhenNotValidated()
    {
        var (consultant, _, skills) = await SetUpProfileWithConsultantAsync();

        var result = await _service.GetRoadmapAsync(consultant.Id, full: true);

        var node = result!.Single(n => n.SkillId == skills.Starter.Id);
        Assert.That(node.CurrentNiveau, Is.EqualTo(0));
    }

    [Test]
    public async Task GetRoadmap_SetsTargetNiveau_FromActiveGoal()
    {
        var (consultant, _, skills) = await SetUpProfileWithConsultantAsync();
        await CreateGoalAsync(consultant.UserId, skills.Starter.Id, targetNiveau: 5);

        var result = await _service.GetRoadmapAsync(consultant.Id, full: true);

        var node = result!.Single(n => n.SkillId == skills.Starter.Id);
        Assert.That(node.TargetNiveau, Is.EqualTo(5));
    }

    [Test]
    public async Task GetRoadmap_TargetNiveauIsNull_WhenNoActiveGoal()
    {
        var (consultant, _, skills) = await SetUpProfileWithConsultantAsync();

        var result = await _service.GetRoadmapAsync(consultant.Id, full: true);

        var node = result!.Single(n => n.SkillId == skills.Starter.Id);
        Assert.That(node.TargetNiveau, Is.Null);
    }

    [Test]
    public async Task GetRoadmap_TargetNiveauIsNull_WhenGoalIsNotActive()
    {
        var (consultant, _, skills) = await SetUpProfileWithConsultantAsync();
        await CreateGoalAsync(consultant.UserId, skills.Starter.Id, targetNiveau: 5, status: GoalStatus.Achieved);

        var result = await _service.GetRoadmapAsync(consultant.Id, full: true);

        var node = result!.Single(n => n.SkillId == skills.Starter.Id);
        Assert.That(node.TargetNiveau, Is.Null);
    }

    // ── Roadmap: prerequisite warnings (#14) ─────────────────────────────────

    [Test]
    public async Task GetRoadmap_PrerequisitesMet_WhenNoneExist()
    {
        var (consultant, _, skills) = await SetUpProfileWithConsultantAsync();

        var result = await _service.GetRoadmapAsync(consultant.Id, full: true);

        var node = result!.Single(n => n.SkillId == skills.Starter.Id);
        Assert.That(node.PrerequisitesMet, Is.True);
        Assert.That(node.UnmetPrerequisites, Is.Empty);
    }

    [Test]
    public async Task GetRoadmap_PrerequisitesNotMet_WhenRequiredSkillNotValidated()
    {
        var (consultant, _, skills) = await SetUpProfileWithConsultantAsync();
        // Dependent requires Starter at niveau 2; consultant has none

        var result = await _service.GetRoadmapAsync(consultant.Id, full: true);

        var node = result!.Single(n => n.SkillId == skills.Dependent.Id);
        Assert.That(node.PrerequisitesMet, Is.False);
        Assert.That(node.UnmetPrerequisites, Has.Count.EqualTo(1));

        var warning = node.UnmetPrerequisites[0];
        Assert.That(warning.RequiredSkillId, Is.EqualTo(skills.Starter.Id));
        Assert.That(warning.RequiredMinNiveau, Is.EqualTo(2));
        Assert.That(warning.CurrentNiveau, Is.EqualTo(0));
    }

    [Test]
    public async Task GetRoadmap_PrerequisitesMet_WhenRequiredSkillValidatedAtRequiredNiveau()
    {
        var (consultant, _, skills) = await SetUpProfileWithConsultantAsync();
        await CreateValidationAsync(consultant.UserId, skills.Starter.Id, niveau: 2);

        var result = await _service.GetRoadmapAsync(consultant.Id, full: true);

        var node = result!.Single(n => n.SkillId == skills.Dependent.Id);
        Assert.That(node.PrerequisitesMet, Is.True);
        Assert.That(node.UnmetPrerequisites, Is.Empty);
    }

    [Test]
    public async Task GetRoadmap_PrerequisitesNotMet_WhenValidatedBelowRequiredNiveau()
    {
        var (consultant, _, skills) = await SetUpProfileWithConsultantAsync();
        await CreateValidationAsync(consultant.UserId, skills.Starter.Id, niveau: 1);

        var result = await _service.GetRoadmapAsync(consultant.Id, full: true);

        var node = result!.Single(n => n.SkillId == skills.Dependent.Id);
        Assert.That(node.PrerequisitesMet, Is.False);
        Assert.That(node.UnmetPrerequisites[0].CurrentNiveau, Is.EqualTo(1));
    }

    // ── Roadmap: progressive disclosure ───────────────────────────────────────

    [Test]
    public async Task GetRoadmap_FullView_ReturnsAllProfileSkills()
    {
        var (consultant, _, _) = await SetUpProfileWithConsultantAsync();

        var result = await _service.GetRoadmapAsync(consultant.Id, full: true);

        Assert.That(result!, Has.Count.EqualTo(3)); // Starter, Dependent, Unlocked
    }

    [Test]
    public async Task GetRoadmap_DefaultView_ExcludesLockedSkills_WhenPrerequisitesUnmet()
    {
        var (consultant, _, skills) = await SetUpProfileWithConsultantAsync();
        // No validations — Dependent is locked (requires Starter >= 2)

        var result = await _service.GetRoadmapAsync(consultant.Id);

        var ids = result!.Select(n => n.SkillId).ToHashSet();
        Assert.That(ids.Contains(skills.Dependent.Id), Is.False);
    }

    [Test]
    public async Task GetRoadmap_DefaultView_IncludesAnchoredSkills()
    {
        var (consultant, _, skills) = await SetUpProfileWithConsultantAsync();
        await CreateValidationAsync(consultant.UserId, skills.Dependent.Id, niveau: 1);
        await CreateValidationAsync(consultant.UserId, skills.Starter.Id, niveau: 2); // meet prereq

        var result = await _service.GetRoadmapAsync(consultant.Id);

        var ids = result!.Select(n => n.SkillId).ToHashSet();
        Assert.That(ids.Contains(skills.Dependent.Id), Is.True);
    }

    // ── Seniority progress (#15) ──────────────────────────────────────────────

    [Test]
    public async Task GetSeniorityProgress_ReturnsJuniorProgress_WhenNoValidations()
    {
        var (consultant, _, _) = await SetUpProfileWithConsultantAsync();

        var result = await _service.GetSeniorityProgressAsync(consultant.Id);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.NextLevel, Is.EqualTo(SeniorityLevel.Junior));
        Assert.That(result.CurrentLevel, Is.Null);
    }

    [Test]
    public async Task GetSeniorityProgress_ReturnsMetCount()
    {
        var (consultant, _, skills) = await SetUpProfileWithConsultantAsync();
        // Junior threshold: Starter >= 2  (1 criterion)
        await CreateValidationAsync(consultant.UserId, skills.Starter.Id, niveau: 2);

        var result = await _service.GetSeniorityProgressAsync(consultant.Id);

        // Junior is now fully met — should progress to Medior
        Assert.That(result!.NextLevel, Is.EqualTo(SeniorityLevel.Medior));
        Assert.That(result.CurrentLevel, Is.EqualTo(SeniorityLevel.Junior));
    }

    [Test]
    public async Task GetSeniorityProgress_UnmetCriteria_ListsShortfall()
    {
        var (consultant, _, skills) = await SetUpProfileWithConsultantAsync();
        // Junior: Starter >= 2 — not met

        var result = await _service.GetSeniorityProgressAsync(consultant.Id);

        Assert.That(result!.UnmetCriteria, Has.Count.EqualTo(1));
        var criterion = result.UnmetCriteria[0];
        Assert.That(criterion.SkillId, Is.EqualTo(skills.Starter.Id));
        Assert.That(criterion.MinNiveau, Is.EqualTo(2));
        Assert.That(criterion.CurrentNiveau, Is.EqualTo(0));
    }

    [Test]
    public async Task GetSeniorityProgress_ReturnsNullNextLevel_WhenSeniorAchieved()
    {
        var (consultant, _, skills) = await SetUpProfileWithConsultantAsync();
        // Senior threshold: Starter >= 6
        await CreateValidationAsync(consultant.UserId, skills.Starter.Id, niveau: 7);

        var result = await _service.GetSeniorityProgressAsync(consultant.Id);

        Assert.That(result!.CurrentLevel, Is.EqualTo(SeniorityLevel.Senior));
        Assert.That(result.NextLevel, Is.Null);
        Assert.That(result.Met, Is.EqualTo(result.Required));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a profile with 3 skills:
    /// - Starter: no prerequisites, LevelCount = 7
    /// - Dependent: requires Starter >= 2
    /// - Unlocked: no prerequisites, LevelCount = 3
    ///
    /// Seniority thresholds:
    /// - Junior:  Starter >= 2
    /// - Medior:  Starter >= 4
    /// - Senior:  Starter >= 6
    /// </summary>
    private async Task<(ConsultantEntity Consultant, CompetenceCentreProfileEntity Profile, TestSkills Skills)>
        SetUpProfileWithConsultantAsync()
    {
        var category = new SkillCategoryEntity { Name = "Test" };
        Db.SkillCategories.Add(category);

        var starter = new SkillEntity { Name = "Starter", Category = category, LevelCount = 7 };
        var dependent = new SkillEntity { Name = "Dependent", Category = category, LevelCount = 5 };
        var unlocked = new SkillEntity { Name = "Unlocked", Category = category, LevelCount = 3 };
        Db.Skills.AddRange(starter, dependent, unlocked);
        await Db.SaveChangesAsync();

        Db.SkillPrerequisites.Add(new SkillPrerequisiteEntity
        {
            Skill = dependent,
            RequiredSkill = starter,
            RequiredMinNiveau = 2,
        });

        var profile = new CompetenceCentreProfileEntity { Name = "TestProfile" };
        Db.CompetenceCentreProfiles.Add(profile);
        await Db.SaveChangesAsync();

        Db.ProfileSkills.AddRange(
            new ProfileSkillEntity { Profile = profile, Skill = starter },
            new ProfileSkillEntity { Profile = profile, Skill = dependent },
            new ProfileSkillEntity { Profile = profile, Skill = unlocked });

        Db.SeniorityThresholds.AddRange(
            new SeniorityThresholdEntity { Profile = profile, Skill = starter, SeniorityLevel = SeniorityLevel.Junior, MinNiveau = 2 },
            new SeniorityThresholdEntity { Profile = profile, Skill = starter, SeniorityLevel = SeniorityLevel.Medior, MinNiveau = 4 },
            new SeniorityThresholdEntity { Profile = profile, Skill = starter, SeniorityLevel = SeniorityLevel.Senior, MinNiveau = 6 });

        await Db.SaveChangesAsync();

        var consultant = await CreateConsultantAsync(profile.Id);
        return (consultant, profile, new TestSkills(starter, dependent, unlocked));
    }

    private async Task<ConsultantEntity> CreateConsultantAsync(int? profileId)
    {
        var consultant = new ConsultantEntity { UserId = UserId, ProfileId = profileId };
        Db.Consultants.Add(consultant);
        await Db.SaveChangesAsync();
        return consultant;
    }

    private async Task CreateValidationAsync(string userId, int skillId, int niveau)
    {
        Db.SkillValidations.Add(new SkillValidationEntity
        {
            ConsultantUserId = userId,
            CoachUserId = "coach-001",
            SkillId = skillId,
            Niveau = niveau,
        });
        await Db.SaveChangesAsync();
    }

    private async Task CreateGoalAsync(
        string userId,
        int skillId,
        int targetNiveau,
        GoalStatus status = GoalStatus.Active)
    {
        Db.Goals.Add(new GoalEntity
        {
            ConsultantUserId = userId,
            CoachUserId = "coach-001",
            SkillId = skillId,
            CurrentNiveau = 0,
            TargetNiveau = targetNiveau,
            Status = status,
        });
        await Db.SaveChangesAsync();
    }

    private sealed record TestSkills(
        SkillEntity Starter,
        SkillEntity Dependent,
        SkillEntity Unlocked);
}
