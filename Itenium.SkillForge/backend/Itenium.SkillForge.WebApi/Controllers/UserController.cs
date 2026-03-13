using System.Globalization;
using System.Security.Claims;
using Itenium.Forge.Security.OpenIddict;
using Itenium.SkillForge.Data;
using Itenium.SkillForge.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = SkillForgePolicies.Backoffice)]
public class UserController(UserManager<ForgeUser> userManager, AppDbContext db) : ControllerBase
{
    /// <summary>
    /// Get all users, optionally including archived ones.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<UserResponse>>> GetUsers([FromQuery] bool includeArchived = false)
    {
        var profiles = await db.UserProfiles.ToDictionaryAsync(p => p.UserId);
        var users = userManager.Users.ToList();

        var responses = new List<UserResponse>();
        foreach (var user in users)
        {
            profiles.TryGetValue(user.Id, out var profile);
            if (!includeArchived && (profile?.IsArchived ?? false))
                continue;

            var roles = await userManager.GetRolesAsync(user);
            var claims = await userManager.GetClaimsAsync(user);
            responses.Add(ToResponse(user, roles, claims, profile?.IsArchived ?? false));
        }

        return Ok(responses);
    }

    /// <summary>
    /// Get learners with no team assignment.
    /// </summary>
    [HttpGet("unassigned")]
    public async Task<ActionResult<List<UserResponse>>> GetUnassigned()
    {
        var users = userManager.Users.ToList();
        var responses = new List<UserResponse>();

        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            var claims = await userManager.GetClaimsAsync(user);
            var teamClaims = claims.Where(c => c.Type == "team").ToList();

            if (roles.Contains("learner", StringComparer.Ordinal) && teamClaims.Count == 0)
                responses.Add(ToResponse(user, roles, claims, false));
        }

        return Ok(responses);
    }

    /// <summary>
    /// Create a new user with role and optional team assignments.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<UserResponse>> CreateUser([FromBody] CreateUserRequest request)
    {
        var user = new ForgeUser
        {
            UserName = request.Email,
            Email = request.Email,
            EmailConfirmed = true,
            FirstName = request.FirstName,
            LastName = request.LastName,
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        var roleResult = await userManager.AddToRoleAsync(user, request.Role);
        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            return BadRequest(roleResult.Errors);
        }

        if (request.Teams != null)
        {
            foreach (var teamId in request.Teams)
                await userManager.AddClaimAsync(user, new Claim("team", teamId.ToString(CultureInfo.InvariantCulture)));
        }

        var roles = await userManager.GetRolesAsync(user);
        var claims = await userManager.GetClaimsAsync(user);
        return CreatedAtAction(nameof(GetUsers), ToResponse(user, roles, claims, false));
    }

    /// <summary>
    /// Replace the role of a user.
    /// </summary>
    [HttpPut("{id}/role")]
    public async Task<IActionResult> UpdateRole(string id, [FromBody] UpdateUserRoleRequest request)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound();

        var currentRoles = await userManager.GetRolesAsync(user);
        await userManager.RemoveFromRolesAsync(user, currentRoles);
        await userManager.AddToRoleAsync(user, request.Role);

        return NoContent();
    }

    /// <summary>
    /// Replace the team assignments of a user.
    /// </summary>
    [HttpPut("{id}/teams")]
    public async Task<IActionResult> UpdateTeams(string id, [FromBody] UpdateUserTeamsRequest request)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound();

        var currentClaims = await userManager.GetClaimsAsync(user);
        var teamClaims = currentClaims.Where(c => c.Type == "team").ToList();
        await userManager.RemoveClaimsAsync(user, teamClaims);

        foreach (var teamId in request.Teams)
            await userManager.AddClaimAsync(user, new Claim("team", teamId.ToString(CultureInfo.InvariantCulture)));

        return NoContent();
    }

    /// <summary>
    /// Soft-archive a user: disables login and hides from non-admin queries.
    /// All history is preserved.
    /// </summary>
    [HttpPost("{id}/archive")]
    public async Task<IActionResult> ArchiveUser(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound();

        await userManager.SetLockoutEnabledAsync(user, true);
        await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);

        var profile = await db.UserProfiles.FindAsync(id)
            ?? db.UserProfiles.Add(new UserProfileEntity { UserId = id }).Entity;
        profile.IsArchived = true;
        await db.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Restore an archived user: re-enables login.
    /// </summary>
    [HttpPost("{id}/restore")]
    public async Task<IActionResult> RestoreUser(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound();

        var profile = await db.UserProfiles.FindAsync(id);
        if (profile != null)
        {
            profile.IsArchived = false;
            await db.SaveChangesAsync();
        }

        await userManager.SetLockoutEnabledAsync(user, false);
        await userManager.SetLockoutEndDateAsync(user, null);

        return NoContent();
    }

    private static UserResponse ToResponse(
        ForgeUser user,
        IList<string> roles,
        IList<Claim> claims,
        bool isArchived)
    {
        var teams = claims
            .Where(c => c.Type == "team")
            .Select(c => int.Parse(c.Value, CultureInfo.InvariantCulture))
            .ToList();

        return new UserResponse(
            user.Id,
            user.Email ?? string.Empty,
            user.FirstName,
            user.LastName,
            roles.FirstOrDefault() ?? string.Empty,
            teams,
            isArchived);
    }
}

public record UserResponse(
    string Id,
    string Email,
    string? FirstName,
    string? LastName,
    string Role,
    ICollection<int> Teams,
    bool IsArchived);

public record CreateUserRequest(
    string Email,
    string FirstName,
    string LastName,
    string Role,
    string Password,
    ICollection<int>? Teams);

public record UpdateUserRoleRequest(string Role);

public record UpdateUserTeamsRequest(ICollection<int> Teams);
