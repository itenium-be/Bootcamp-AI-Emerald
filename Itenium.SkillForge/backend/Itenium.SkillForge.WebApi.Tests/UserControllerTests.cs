using System.Security.Claims;
using Itenium.Forge.Security.OpenIddict;
using Itenium.SkillForge.Entities;
using Itenium.SkillForge.Services;
using Itenium.SkillForge.WebApi.Controllers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Itenium.SkillForge.WebApi.Tests;

[TestFixture]
public class UserControllerTests : DatabaseTestBase
{
    private UserManager<ForgeUser> _userManager = null!;
    private UserController _sut = null!;

    [SetUp]
    public void Setup()
    {
        _userManager = CreateUserManager();
        _sut = new UserController(_userManager, Db);
    }

    [TearDown]
    public void TearDown()
    {
        _userManager.Dispose();
    }

    // --- GET /api/users ---

    [Test]
    public async Task GetUsers_ExcludesArchivedByDefault()
    {
        var active = new ForgeUser { Id = "1", UserName = "active", Email = "active@test.local" };
        var archived = new ForgeUser { Id = "2", UserName = "archived", Email = "archived@test.local" };
        Db.Set<ForgeUser>().Add(archived);
        Db.UserProfiles.Add(new UserProfileEntity { UserId = "2", IsArchived = true });
        await Db.SaveChangesAsync();

        _userManager.Users.Returns(new[] { active, archived }.AsQueryable());
        SetupUserRolesAndClaims(active);
        SetupUserRolesAndClaims(archived);

        var result = await _sut.GetUsers();

        var users = ((result.Result as OkObjectResult)!.Value as List<UserResponse>)!;
        Assert.That(users.Select(u => u.Id), Does.Not.Contain("2"));
        Assert.That(users.Select(u => u.Id), Contains.Item("1"));
    }

    [Test]
    public async Task GetUsers_WhenIncludeArchived_ReturnsAll()
    {
        var active = new ForgeUser { Id = "1", UserName = "active2", Email = "active2@test.local" };
        var archived = new ForgeUser { Id = "2", UserName = "archived2", Email = "archived2@test.local" };
        Db.Set<ForgeUser>().Add(archived);
        Db.UserProfiles.Add(new UserProfileEntity { UserId = "2", IsArchived = true });
        await Db.SaveChangesAsync();

        _userManager.Users.Returns(new[] { active, archived }.AsQueryable());
        SetupUserRolesAndClaims(active);
        SetupUserRolesAndClaims(archived);

        var result = await _sut.GetUsers(includeArchived: true);

        var users = ((result.Result as OkObjectResult)!.Value as List<UserResponse>)!;
        Assert.That(users, Has.Count.EqualTo(2));
    }

    // --- GET /api/users/unassigned ---

    [Test]
    public async Task GetUnassigned_ReturnsLearnersWithNoTeams()
    {
        var learner = new ForgeUser { Id = "1", Email = "learner@test.local" };
        var assigned = new ForgeUser { Id = "2", Email = "assigned@test.local" };

        _userManager.Users.Returns(new[] { learner, assigned }.AsQueryable());
        _userManager.GetRolesAsync(learner).Returns(Task.FromResult<IList<string>>(["learner"]));
        _userManager.GetClaimsAsync(learner).Returns(Task.FromResult<IList<Claim>>([]));
        _userManager.GetRolesAsync(assigned).Returns(Task.FromResult<IList<string>>(["learner"]));
        _userManager.GetClaimsAsync(assigned).Returns(Task.FromResult<IList<Claim>>([new Claim("team", "1")]));

        var result = await _sut.GetUnassigned();

        var users = ((result.Result as OkObjectResult)!.Value as List<UserResponse>)!;
        Assert.That(users, Has.Count.EqualTo(1));
        Assert.That(users[0].Id, Is.EqualTo("1"));
    }

    // --- POST /api/users ---

    [Test]
    public async Task CreateUser_CreatesUserWithRoleAndTeams()
    {
        _userManager.CreateAsync(Arg.Any<ForgeUser>(), Arg.Any<string>())
            .Returns(Task.FromResult(IdentityResult.Success));
        _userManager.AddToRoleAsync(Arg.Any<ForgeUser>(), Arg.Any<string>())
            .Returns(Task.FromResult(IdentityResult.Success));
        _userManager.AddClaimAsync(Arg.Any<ForgeUser>(), Arg.Any<Claim>())
            .Returns(Task.FromResult(IdentityResult.Success));
        _userManager.GetRolesAsync(Arg.Any<ForgeUser>())
            .Returns(Task.FromResult<IList<string>>(["manager"]));
        _userManager.GetClaimsAsync(Arg.Any<ForgeUser>())
            .Returns(Task.FromResult<IList<Claim>>([new Claim("team", "1")]));

        var request = new CreateUserRequest("new@test.local", "New", "User", "manager", "pass123!", [1]);

        var result = await _sut.CreateUser(request);

        Assert.That(result.Result, Is.TypeOf<CreatedAtActionResult>());
        await _userManager.Received(1).CreateAsync(Arg.Any<ForgeUser>(), "pass123!");
        await _userManager.Received(1).AddToRoleAsync(Arg.Any<ForgeUser>(), "manager");
        await _userManager.Received(1).AddClaimAsync(Arg.Any<ForgeUser>(), Arg.Is<Claim>(c => c.Type == "team" && c.Value == "1"));
    }

    [Test]
    public async Task CreateUser_WhenCreateFails_ReturnsBadRequest()
    {
        _userManager.CreateAsync(Arg.Any<ForgeUser>(), Arg.Any<string>())
            .Returns(Task.FromResult(IdentityResult.Failed(new IdentityError { Description = "Email taken" })));

        var result = await _sut.CreateUser(new CreateUserRequest("x@test.local", "X", "Y", "learner", "pass!", null));

        Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task CreateUser_WhenRoleInvalid_ReturnsBadRequestAndDeletesUser()
    {
        _userManager.CreateAsync(Arg.Any<ForgeUser>(), Arg.Any<string>())
            .Returns(Task.FromResult(IdentityResult.Success));
        _userManager.AddToRoleAsync(Arg.Any<ForgeUser>(), Arg.Any<string>())
            .Returns(Task.FromResult(IdentityResult.Failed(new IdentityError { Description = "Role not found" })));
        _userManager.DeleteAsync(Arg.Any<ForgeUser>())
            .Returns(Task.FromResult(IdentityResult.Success));

        var result = await _sut.CreateUser(new CreateUserRequest("x@test.local", "X", "Y", "nonexistent", "pass!", null));

        Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
        await _userManager.Received(1).DeleteAsync(Arg.Any<ForgeUser>());
    }

    // --- PUT /api/users/{id}/role ---

    [Test]
    public async Task UpdateRole_ReplacesExistingRole()
    {
        var user = new ForgeUser { Id = "1" };
        _userManager.FindByIdAsync("1").Returns(Task.FromResult<ForgeUser?>(user));
        _userManager.GetRolesAsync(user).Returns(Task.FromResult<IList<string>>(["learner"]));
        _userManager.RemoveFromRolesAsync(user, Arg.Any<IEnumerable<string>>())
            .Returns(Task.FromResult(IdentityResult.Success));
        _userManager.AddToRoleAsync(user, Arg.Any<string>())
            .Returns(Task.FromResult(IdentityResult.Success));

        var result = await _sut.UpdateRole("1", new UpdateUserRoleRequest("manager"));

        Assert.That(result, Is.TypeOf<NoContentResult>());
        await _userManager.Received(1).RemoveFromRolesAsync(user, Arg.Is<IEnumerable<string>>(r => r.Contains("learner")));
        await _userManager.Received(1).AddToRoleAsync(user, "manager");
    }

    [Test]
    public async Task UpdateRole_WhenUserNotFound_ReturnsNotFound()
    {
        _userManager.FindByIdAsync("999").Returns(Task.FromResult<ForgeUser?>(null));

        var result = await _sut.UpdateRole("999", new UpdateUserRoleRequest("manager"));

        Assert.That(result, Is.TypeOf<NotFoundResult>());
    }

    // --- PUT /api/users/{id}/teams ---

    [Test]
    public async Task UpdateTeams_ReplacesExistingTeamClaims()
    {
        var user = new ForgeUser { Id = "1" };
        _userManager.FindByIdAsync("1").Returns(Task.FromResult<ForgeUser?>(user));
        _userManager.GetClaimsAsync(user).Returns(Task.FromResult<IList<Claim>>([new Claim("team", "1")]));
        _userManager.RemoveClaimsAsync(user, Arg.Any<IEnumerable<Claim>>())
            .Returns(Task.FromResult(IdentityResult.Success));
        _userManager.AddClaimAsync(user, Arg.Any<Claim>())
            .Returns(Task.FromResult(IdentityResult.Success));

        var result = await _sut.UpdateTeams("1", new UpdateUserTeamsRequest([2, 3]));

        Assert.That(result, Is.TypeOf<NoContentResult>());
        await _userManager.Received(1).RemoveClaimsAsync(user, Arg.Any<IEnumerable<Claim>>());
        await _userManager.Received(1).AddClaimAsync(user, Arg.Is<Claim>(c => c.Type == "team" && c.Value == "2"));
        await _userManager.Received(1).AddClaimAsync(user, Arg.Is<Claim>(c => c.Type == "team" && c.Value == "3"));
    }

    // --- POST /api/users/{id}/archive ---

    [Test]
    public async Task ArchiveUser_SetsIsArchivedAndDisablesLogin()
    {
        var user = new ForgeUser { Id = "1", UserName = "archive-test", Email = "archive@test.local" };
        Db.Set<ForgeUser>().Add(user);
        Db.UserProfiles.Add(new UserProfileEntity { UserId = "1", IsArchived = false });
        await Db.SaveChangesAsync();
        _userManager.FindByIdAsync("1").Returns(Task.FromResult<ForgeUser?>(user));
        _userManager.SetLockoutEnabledAsync(user, true).Returns(Task.FromResult(IdentityResult.Success));
        _userManager.SetLockoutEndDateAsync(user, Arg.Any<DateTimeOffset?>()).Returns(Task.FromResult(IdentityResult.Success));

        var result = await _sut.ArchiveUser("1");

        Assert.That(result, Is.TypeOf<NoContentResult>());
        var profile = await Db.UserProfiles.FindAsync("1");
        Assert.That(profile!.IsArchived, Is.True);
        await _userManager.Received(1).SetLockoutEnabledAsync(user, true);
        await _userManager.Received(1).SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
    }

    [Test]
    public async Task ArchiveUser_WhenUserNotFound_ReturnsNotFound()
    {
        _userManager.FindByIdAsync("999").Returns(Task.FromResult<ForgeUser?>(null));

        var result = await _sut.ArchiveUser("999");

        Assert.That(result, Is.TypeOf<NotFoundResult>());
    }

    // --- POST /api/users/{id}/restore ---

    [Test]
    public async Task RestoreUser_ClearsIsArchivedAndEnablesLogin()
    {
        var user = new ForgeUser { Id = "1", UserName = "restore-test", Email = "restore@test.local" };
        Db.Set<ForgeUser>().Add(user);
        Db.UserProfiles.Add(new UserProfileEntity { UserId = "1", IsArchived = true });
        await Db.SaveChangesAsync();
        _userManager.FindByIdAsync("1").Returns(Task.FromResult<ForgeUser?>(user));
        _userManager.SetLockoutEnabledAsync(user, false).Returns(Task.FromResult(IdentityResult.Success));
        _userManager.SetLockoutEndDateAsync(user, null).Returns(Task.FromResult(IdentityResult.Success));

        var result = await _sut.RestoreUser("1");

        Assert.That(result, Is.TypeOf<NoContentResult>());
        var profile = await Db.UserProfiles.FindAsync("1");
        Assert.That(profile!.IsArchived, Is.False);
        await _userManager.Received(1).SetLockoutEnabledAsync(user, false);
        await _userManager.Received(1).SetLockoutEndDateAsync(user, null);
    }

    private static UserManager<ForgeUser> CreateUserManager()
    {
        var store = Substitute.For<IUserStore<ForgeUser>>();
        return Substitute.For<UserManager<ForgeUser>>(
            store, null, null, null, null, null, null, null, null);
    }

    private void SetupUserRolesAndClaims(ForgeUser user,
        IList<string>? roles = null,
        IList<Claim>? claims = null)
    {
        _userManager.GetRolesAsync(user)
            .Returns(Task.FromResult(roles ?? (IList<string>)[]));
        _userManager.GetClaimsAsync(user)
            .Returns(Task.FromResult(claims ?? (IList<Claim>)[]));
    }
}
