using Itenium.SkillForge.Entities.Coaching;
using Itenium.SkillForge.Entities.Consultants;
using Itenium.SkillForge.Services;
using Itenium.SkillForge.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Itenium.SkillForge.WebApi.Tests;

[TestFixture]
public class CoachingSessionControllerTests : DatabaseTestBase
{
    private ISkillForgeUser _user = null!;
    private CoachingSessionController _sut = null!;

    [SetUp]
    public void Setup()
    {
        _user = Substitute.For<ISkillForgeUser>();
        var scope = new FakeTeamQueryScope(isBackOffice: false, teamIds: [1]);
        _sut = new CoachingSessionController(Db, scope, _user);
    }

    [Test]
    public async Task StartSession_WhenManagerForConsultant_CreatesSession()
    {
        var scope = new FakeTeamQueryScope(isBackOffice: false, teamIds: [1]);
        _sut = new CoachingSessionController(Db, scope, _user);
        _user.UserId.Returns("coach-1");

        Db.Consultants.Add(new ConsultantEntity { UserId = "consultant-1", TeamId = 1 });
        await Db.SaveChangesAsync();

        var result = await _sut.StartSession(new StartSessionRequest("consultant-1"));

        var created = result.Result as CreatedAtActionResult;
        Assert.That(created, Is.Not.Null);
        var session = created!.Value as CoachingSessionEntity;
        Assert.That(session!.ConsultantUserId, Is.EqualTo("consultant-1"));
        Assert.That(session.CoachUserId, Is.EqualTo("coach-1"));
        Assert.That(session.ClosedAt, Is.Null);
    }

    [Test]
    public async Task StartSession_WhenManagerForDifferentTeam_ReturnsForbidden()
    {
        var scope = new FakeTeamQueryScope(isBackOffice: false, teamIds: [2]);
        _sut = new CoachingSessionController(Db, scope, _user);

        Db.Consultants.Add(new ConsultantEntity { UserId = "consultant-team-1", TeamId = 1 });
        await Db.SaveChangesAsync();

        var result = await _sut.StartSession(new StartSessionRequest("consultant-team-1"));

        Assert.That(result.Result, Is.TypeOf<ForbidResult>());
    }

    [Test]
    public async Task CloseSession_WithNotes_ClosesSession()
    {
        _user.UserId.Returns("coach-1");
        var session = new CoachingSessionEntity
        {
            ConsultantUserId = "consultant-1",
            CoachUserId = "coach-1",
        };
        Db.CoachingSessions.Add(session);
        await Db.SaveChangesAsync();

        var result = await _sut.CloseSession(session.Id, new CloseSessionRequest("Good progress."));

        Assert.That(result, Is.TypeOf<NoContentResult>());
        Assert.That(session.ClosedAt, Is.Not.Null);
        Assert.That(session.Notes, Is.EqualTo("Good progress."));
    }

    [Test]
    public async Task CloseSession_WhenAlreadyClosed_ReturnsBadRequest()
    {
        var session = new CoachingSessionEntity
        {
            ConsultantUserId = "consultant-1",
            CoachUserId = "coach-1",
            ClosedAt = DateTime.UtcNow.AddHours(-1),
        };
        Db.CoachingSessions.Add(session);
        await Db.SaveChangesAsync();

        var result = await _sut.CloseSession(session.Id, new CloseSessionRequest(null));

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task GetConsultantSessions_WhenManager_ReturnsSessions()
    {
        var scope = new FakeTeamQueryScope(isBackOffice: false, teamIds: [1]);
        _sut = new CoachingSessionController(Db, scope, _user);

        Db.Consultants.Add(new ConsultantEntity { UserId = "consultant-1", TeamId = 1 });
        Db.CoachingSessions.Add(new CoachingSessionEntity { ConsultantUserId = "consultant-1", CoachUserId = "coach-1" });
        await Db.SaveChangesAsync();

        var result = await _sut.GetSessions("consultant-1");

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var sessions = ok!.Value as List<CoachingSessionEntity>;
        Assert.That(sessions, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task GetConsultantSessions_WhenLearner_ReturnsOwnSessions()
    {
        _user.UserId.Returns("consultant-learner");
        var scope = new FakeTeamQueryScope(isBackOffice: false, teamIds: []);
        _sut = new CoachingSessionController(Db, scope, _user);

        Db.CoachingSessions.Add(new CoachingSessionEntity { ConsultantUserId = "consultant-learner", CoachUserId = "coach-1" });
        Db.CoachingSessions.Add(new CoachingSessionEntity { ConsultantUserId = "other-consultant", CoachUserId = "coach-1" });
        await Db.SaveChangesAsync();

        var result = await _sut.GetSessions("consultant-learner");

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var sessions = ok!.Value as List<CoachingSessionEntity>;
        Assert.That(sessions, Has.Count.EqualTo(1));
        Assert.That(sessions![0].ConsultantUserId, Is.EqualTo("consultant-learner"));
    }
}
