using System.Security.Claims;
using Itenium.SkillForge.Data;
using Itenium.SkillForge.Entities.Coaching;
using Itenium.SkillForge.Entities.Consultants;
using Itenium.SkillForge.Services.Coaching;
using Itenium.SkillForge.WebApi.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.WebApi.Tests;

[TestFixture]
public class CoachingSessionTests : DatabaseTestBase
{
    private SessionsController _sut = null!;
    private ConsultantEntity _consultant = null!;

    [SetUp]
    public async Task SetUp()
    {
        _consultant = new ConsultantEntity { UserId = "consultant-lea" };
        Db.Consultants.Add(_consultant);
        await Db.SaveChangesAsync();

        _sut = new SessionsController(new CoachingSessionService(Db), new FakeTeamQueryScope(isBackOffice: true));

        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, "coach-nathalie"),
                ]))
            }
        };
    }

    // ── Start session ─────────────────────────────────────────────────────────

    [Test]
    public async Task StartSession_WhenConsultantNotFound_ReturnsNotFound()
    {
        var result = await _sut.StartSession(int.MaxValue);

        Assert.That(result, Is.TypeOf<NotFoundResult>());
    }

    [Test]
    public async Task StartSession_WhenConsultantExists_Returns201WithSession()
    {
        var result = await _sut.StartSession(_consultant.Id);

        Assert.That(result, Is.TypeOf<CreatedAtActionResult>());
        var created = (CreatedAtActionResult)result;
        var session = (CoachingSessionRecord)created.Value!;
        Assert.That(session.ConsultantUserId, Is.EqualTo("consultant-lea"));
        Assert.That(session.CoachUserId, Is.EqualTo("coach-nathalie"));
        Assert.That(session.StartedAt, Is.Not.EqualTo(default(DateTime)));
        Assert.That(session.ClosedAt, Is.Null);
    }

    // ── Update notes ──────────────────────────────────────────────────────────

    [Test]
    public async Task UpdateNotes_WhenSessionNotFound_ReturnsNotFound()
    {
        var result = await _sut.UpdateNotes(int.MaxValue, new UpdateSessionNotesRequest("some notes"));

        Assert.That(result, Is.TypeOf<NotFoundResult>());
    }

    [Test]
    public async Task UpdateNotes_WhenSessionExists_ReturnsNoContent()
    {
        var session = new CoachingSessionEntity
        {
            ConsultantUserId = "consultant-lea",
            CoachUserId = "coach-nathalie",
        };
        Db.CoachingSessions.Add(session);
        await Db.SaveChangesAsync();

        var result = await _sut.UpdateNotes(session.Id, new UpdateSessionNotesRequest("Great progress on Clean Code."));

        Assert.That(result, Is.TypeOf<NoContentResult>());
        var saved = await Db.CoachingSessions.FindAsync(session.Id);
        Assert.That(saved!.Notes, Is.EqualTo("Great progress on Clean Code."));
    }

    // ── Close session ─────────────────────────────────────────────────────────

    [Test]
    public async Task CloseSession_WhenSessionNotFound_ReturnsNotFound()
    {
        var result = await _sut.CloseSession(int.MaxValue);

        Assert.That(result, Is.TypeOf<NotFoundResult>());
    }

    [Test]
    public async Task CloseSession_WhenSessionExists_SetsClosedAt()
    {
        var session = new CoachingSessionEntity
        {
            ConsultantUserId = "consultant-lea",
            CoachUserId = "coach-nathalie",
        };
        Db.CoachingSessions.Add(session);
        await Db.SaveChangesAsync();

        var result = await _sut.CloseSession(session.Id);

        Assert.That(result, Is.TypeOf<NoContentResult>());
        var saved = await Db.CoachingSessions.FindAsync(session.Id);
        Assert.That(saved!.ClosedAt, Is.Not.Null);
    }

    // ── Session history ───────────────────────────────────────────────────────

    [Test]
    public async Task GetSessions_WhenConsultantNotFound_ReturnsNotFound()
    {
        var result = await _sut.GetSessions(int.MaxValue);

        Assert.That(result, Is.TypeOf<NotFoundResult>());
    }

    [Test]
    public async Task GetSessions_ReturnsSessionsForConsultant()
    {
        var session1 = new CoachingSessionEntity { ConsultantUserId = "consultant-lea", CoachUserId = "coach-nathalie" };
        var session2 = new CoachingSessionEntity { ConsultantUserId = "consultant-lea", CoachUserId = "coach-nathalie" };
        Db.CoachingSessions.AddRange(session1, session2);
        await Db.SaveChangesAsync();

        var result = await _sut.GetSessions(_consultant.Id);

        Assert.That(result, Is.TypeOf<OkObjectResult>());
        var ok = (OkObjectResult)result;
        var sessions = (IReadOnlyList<CoachingSessionRecord>)ok.Value!;
        Assert.That(sessions, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetSessions_ClosedSessionIsVisibleImmediately()
    {
        var session = new CoachingSessionEntity
        {
            ConsultantUserId = "consultant-lea",
            CoachUserId = "coach-nathalie",
            ClosedAt = DateTime.UtcNow,
        };
        Db.CoachingSessions.Add(session);
        await Db.SaveChangesAsync();

        var result = await _sut.GetSessions(_consultant.Id);

        var ok = (OkObjectResult)result;
        var sessions = (IReadOnlyList<CoachingSessionRecord>)ok.Value!;
        Assert.That(sessions.Single().ClosedAt, Is.Not.Null);
    }
}
