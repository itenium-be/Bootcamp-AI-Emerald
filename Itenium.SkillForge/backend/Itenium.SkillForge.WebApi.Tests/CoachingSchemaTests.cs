using Itenium.SkillForge.Entities.Coaching;
using Itenium.SkillForge.Entities.Goals;
using Itenium.SkillForge.Entities.Resources;
using Itenium.SkillForge.Entities.Skills;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.WebApi.Tests;

[TestFixture]
public class CoachingSchemaTests : DatabaseTestBase
{
    private SkillEntity _skill = null!;

    [SetUp]
    public async Task SetUp()
    {
        var category = new SkillCategoryEntity { Name = "Test Category" };
        Db.SkillCategories.Add(category);
        _skill = new SkillEntity { Name = "Clean Code", Category = category, LevelCount = 3 };
        Db.Skills.Add(_skill);
        await Db.SaveChangesAsync();
    }

    [Test]
    public async Task Goal_CanBeCreatedByCoachForConsultant()
    {
        var goal = new GoalEntity
        {
            ConsultantUserId = "consultant-1",
            CoachUserId = "coach-1",
            SkillId = _skill.Id,
            CurrentNiveau = 1,
            TargetNiveau = 3,
            Deadline = DateTime.UtcNow.AddDays(30),
        };
        Db.Goals.Add(goal);
        await Db.SaveChangesAsync();

        var saved = await Db.Goals
            .Include(g => g.Skill)
            .FirstAsync(g => g.Id == goal.Id);

        Assert.That(saved.ConsultantUserId, Is.EqualTo("consultant-1"));
        Assert.That(saved.CoachUserId, Is.EqualTo("coach-1"));
        Assert.That(saved.Skill.Name, Is.EqualTo("Clean Code"));
        Assert.That(saved.Status, Is.EqualTo(GoalStatus.Active));
    }

    [Test]
    public async Task Goal_CanHaveLinkedResources()
    {
        var resource = new ResourceEntity
        {
            Title = "Clean Code book",
            Url = "https://example.com",
            Type = ResourceType.Book,
            SkillId = _skill.Id,
            FromNiveau = 1,
            ToNiveau = 3,
            AddedByUserId = "coach-1",
        };
        Db.Resources.Add(resource);
        var goal = new GoalEntity
        {
            ConsultantUserId = "consultant-1",
            CoachUserId = "coach-1",
            SkillId = _skill.Id,
            CurrentNiveau = 1,
            TargetNiveau = 3,
            Deadline = DateTime.UtcNow.AddDays(30),
        };
        Db.Goals.Add(goal);
        await Db.SaveChangesAsync();

        Db.GoalResources.Add(new GoalResourceEntity { GoalId = goal.Id, ResourceId = resource.Id });
        await Db.SaveChangesAsync();

        var saved = await Db.Goals
            .Include(g => g.GoalResources)
            .ThenInclude(gr => gr.Resource)
            .FirstAsync(g => g.Id == goal.Id);

        Assert.That(saved.GoalResources, Has.Count.EqualTo(1));
        Assert.That(saved.GoalResources.First().Resource.Title, Is.EqualTo("Clean Code book"));
    }

    [Test]
    public async Task ReadinessFlag_CanBeRaisedAndLaterDismissed()
    {
        var goal = new GoalEntity
        {
            ConsultantUserId = "consultant-1",
            CoachUserId = "coach-1",
            SkillId = _skill.Id,
            CurrentNiveau = 1,
            TargetNiveau = 3,
            Deadline = DateTime.UtcNow.AddDays(30),
        };
        Db.Goals.Add(goal);
        await Db.SaveChangesAsync();

        var flag = new ReadinessFlagEntity { GoalId = goal.Id };
        Db.ReadinessFlags.Add(flag);
        await Db.SaveChangesAsync();

        Assert.That(flag.DismissedAt, Is.Null);
        Assert.That(flag.RaisedAt, Is.Not.EqualTo(default(DateTime)));

        flag.DismissedAt = DateTime.UtcNow;
        await Db.SaveChangesAsync();

        var saved = await Db.ReadinessFlags.FindAsync(flag.Id);
        Assert.That(saved!.DismissedAt, Is.Not.Null);
    }

    [Test]
    public async Task SkillValidation_RecordsCoachIdentityAndTimestamp()
    {
        var validation = new SkillValidationEntity
        {
            ConsultantUserId = "consultant-1",
            CoachUserId = "coach-nathalie",
            SkillId = _skill.Id,
            Niveau = 2,
        };
        Db.SkillValidations.Add(validation);
        await Db.SaveChangesAsync();

        var saved = await Db.SkillValidations.FindAsync(validation.Id);

        Assert.That(saved, Is.Not.Null);
        Assert.That(saved!.CoachUserId, Is.EqualTo("coach-nathalie"));
        Assert.That(saved.ValidatedAt, Is.Not.EqualTo(default(DateTime)));
        Assert.That(saved.Niveau, Is.EqualTo(2));
    }

    [Test]
    public async Task CoachingSession_CanRecordNotesAndLinkValidations()
    {
        var session = new CoachingSessionEntity
        {
            ConsultantUserId = "consultant-lea",
            CoachUserId = "coach-nathalie",
        };
        Db.CoachingSessions.Add(session);
        await Db.SaveChangesAsync();

        var validation = new SkillValidationEntity
        {
            ConsultantUserId = "consultant-lea",
            CoachUserId = "coach-nathalie",
            SkillId = _skill.Id,
            Niveau = 2,
            SessionId = session.Id,
        };
        Db.SkillValidations.Add(validation);
        session.Notes = "Strong grasp of naming. Not yet applying at architectural level.";
        session.ClosedAt = DateTime.UtcNow;
        await Db.SaveChangesAsync();

        var saved = await Db.CoachingSessions
            .Include(s => s.Validations)
            .FirstAsync(s => s.Id == session.Id);

        Assert.That(saved.Notes, Does.Contain("Strong grasp"));
        Assert.That(saved.ClosedAt, Is.Not.Null);
        Assert.That(saved.Validations, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task Resource_CanBeCompletedAndRated()
    {
        var resource = new ResourceEntity
        {
            Title = "Clean Code book",
            Url = "https://example.com",
            Type = ResourceType.Book,
            SkillId = _skill.Id,
            FromNiveau = 1,
            ToNiveau = 3,
            AddedByUserId = "user-1",
        };
        Db.Resources.Add(resource);
        await Db.SaveChangesAsync();

        Db.ResourceCompletions.Add(new ResourceCompletionEntity
        {
            ResourceId = resource.Id,
            UserId = "consultant-lea",
        });
        Db.ResourceRatings.Add(new ResourceRatingEntity
        {
            ResourceId = resource.Id,
            UserId = "consultant-lea",
            IsPositive = true,
        });
        await Db.SaveChangesAsync();

        var saved = await Db.Resources
            .Include(r => r.Completions)
            .Include(r => r.Ratings)
            .FirstAsync(r => r.Id == resource.Id);

        Assert.That(saved.Completions, Has.Count.EqualTo(1));
        Assert.That(saved.Ratings, Has.Count.EqualTo(1));
        Assert.That(saved.Ratings.First().IsPositive, Is.True);
    }
}
