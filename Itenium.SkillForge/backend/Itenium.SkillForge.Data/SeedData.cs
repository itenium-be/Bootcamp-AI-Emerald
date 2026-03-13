using System.Security.Claims;
using Itenium.Forge.Security.OpenIddict;
using Itenium.SkillForge.Entities;
using Itenium.SkillForge.Entities.Coaching;
using Itenium.SkillForge.Entities.Consultants;
using Itenium.SkillForge.Entities.Goals;
using Itenium.SkillForge.Entities.Resources;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Itenium.SkillForge.Data;

public static class SeedData
{
    public static async Task SeedDevelopmentData(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await SeedTeams(db);
        await SeedCourses(db);
        await SkillCatalogueSeedData.Seed(db);
        await app.SeedTestUsers();
        await app.SeedDemoUsers();
    }

    private static async Task SeedTeams(AppDbContext db)
    {
        if (!await db.Teams.AnyAsync())
        {
            db.Teams.AddRange(
                new TeamEntity { Id = 1, Name = "Java" },
                new TeamEntity { Id = 2, Name = ".NET" },
                new TeamEntity { Id = 3, Name = "PO & Analysis" },
                new TeamEntity { Id = 4, Name = "QA" });
            await db.SaveChangesAsync();
        }
    }

    private static async Task SeedCourses(AppDbContext db)
    {
        if (!await db.Courses.AnyAsync())
        {
            db.Courses.AddRange(
                new CourseEntity { Id = 1, Name = "Introduction to Programming", Description = "Learn the basics of programming", Category = "Development", Level = "Beginner" },
                new CourseEntity { Id = 2, Name = "Advanced C#", Description = "Master C# programming language", Category = "Development", Level = "Advanced" },
                new CourseEntity { Id = 3, Name = "Cloud Architecture", Description = "Design scalable cloud solutions", Category = "Architecture", Level = "Intermediate" },
                new CourseEntity { Id = 4, Name = "Agile Project Management", Description = "Learn agile methodologies", Category = "Management", Level = "Beginner" });
            await db.SaveChangesAsync();
        }
    }

    private static async Task SeedTestUsers(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ForgeUser>>();

        // BackOffice admin - no team claim (manages all)
        if (await userManager.FindByEmailAsync("backoffice@test.local") == null)
        {
            var admin = new ForgeUser
            {
                UserName = "backoffice",
                Email = "backoffice@test.local",
                EmailConfirmed = true,
                FirstName = "BackOffice",
                LastName = "Admin"
            };
            var result = await userManager.CreateAsync(admin, "AdminPassword123!");
            if (result.Succeeded)
            {
                await userManager.AddToRolesAsync(admin, ["backoffice"]);
            }
        }

        // Local user for Java team only
        if (await userManager.FindByEmailAsync("java@test.local") == null)
        {
            var user = new ForgeUser
            {
                UserName = "java",
                Email = "java@test.local",
                EmailConfirmed = true,
                FirstName = "Java",
                LastName = "Developer"
            };
            var result = await userManager.CreateAsync(user, "UserPassword123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, "manager");
                await userManager.AddClaimAsync(user, new Claim("team", "1")); // Java
            }
        }

        // Local user for .NET team only
        if (await userManager.FindByEmailAsync("dotnet@test.local") == null)
        {
            var user = new ForgeUser
            {
                UserName = "dotnet",
                Email = "dotnet@test.local",
                EmailConfirmed = true,
                FirstName = "DotNet",
                LastName = "Developer"
            };
            var result = await userManager.CreateAsync(user, "UserPassword123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, "manager");
                await userManager.AddClaimAsync(user, new Claim("team", "2")); // .NET
            }
        }

        // User with access to multiple teams (Java + .NET)
        if (await userManager.FindByEmailAsync("multi@test.local") == null)
        {
            var user = new ForgeUser
            {
                UserName = "multi",
                Email = "multi@test.local",
                EmailConfirmed = true,
                FirstName = "Multi",
                LastName = "Team"
            };
            var result = await userManager.CreateAsync(user, "UserPassword123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, "manager");
                await userManager.AddClaimAsync(user, new Claim("team", "1")); // Java
                await userManager.AddClaimAsync(user, new Claim("team", "2")); // .NET
            }
        }

        // Learner user - basic learner role
        if (await userManager.FindByEmailAsync("learner@test.local") == null)
        {
            var user = new ForgeUser
            {
                UserName = "learner",
                Email = "learner@test.local",
                EmailConfirmed = true,
                FirstName = "Test",
                LastName = "Learner"
            };
            var result = await userManager.CreateAsync(user, "UserPassword123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, "learner");
            }
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // Demo users — covers the full bootcamp demo script (issue #6)
    // ────────────────────────────────────────────────────────────────────────

    private static async Task SeedDemoUsers(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ForgeUser>>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Coaches (manager role)
        var nathalie = await CreateUserIfAbsent(userManager, "nathalie@test.local", "nathalie", "Nathalie", "Dubois", "manager", teamId: 2);
        var javaCoach = await CreateUserIfAbsent(userManager, "javacoach@test.local", "javacoach", "Marc", "Janssen", "manager", teamId: 1);

        // Consultants (learner role)
        var lea = await CreateUserIfAbsent(userManager, "lea@test.local", "lea", "Lea", "Martens", "learner", teamId: 2);
        var sander = await CreateUserIfAbsent(userManager, "sander@test.local", "sander", "Sander", "Claes", "learner", teamId: 1);
        var thomas = await CreateUserIfAbsent(userManager, "thomas@test.local", "thomas", "Thomas", "Vander", "learner", teamId: 2);
        var ana = await CreateUserIfAbsent(userManager, "ana@test.local", "ana", "Ana", "Peeters", "learner", teamId: 2);
        var javier = await CreateUserIfAbsent(userManager, "javier@test.local", "javier", "Javier", "Garcia", "learner", teamId: 1);
        var kim = await CreateUserIfAbsent(userManager, "kim@test.local", "kim", "Kim", "Wouters", "learner", teamId: 4);

        // Resolve existing users by email for idempotent data patching
        var leaUser = lea ?? await userManager.FindByEmailAsync("lea@test.local");
        var sanderUser = sander ?? await userManager.FindByEmailAsync("sander@test.local");
        var thomasUser = thomas ?? await userManager.FindByEmailAsync("thomas@test.local");
        var anaUser = ana ?? await userManager.FindByEmailAsync("ana@test.local");
        var javierUser = javier ?? await userManager.FindByEmailAsync("javier@test.local");
        var kimUser = kim ?? await userManager.FindByEmailAsync("kim@test.local");
        var nathalieUser = nathalie ?? await userManager.FindByEmailAsync("nathalie@test.local");
        var javaCoachUser = javaCoach ?? await userManager.FindByEmailAsync("javacoach@test.local");

        if (nathalieUser is null || javaCoachUser is null || leaUser is null || sanderUser is null || thomasUser is null)
            return;

        await SeedDemoData(db, nathalieUser, javaCoachUser, leaUser, sanderUser, thomasUser, anaUser, javierUser, kimUser);
    }

    private static async Task<ConsultantEntity> UpsertConsultant(AppDbContext db, string userId, int teamId, int? profileId)
    {
        var existing = await db.Consultants.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.UserId == userId);
        if (existing is not null)
        {
            if (existing.TeamId == 0) existing.TeamId = teamId;
            if (existing.ProfileId is null && profileId is not null) existing.ProfileId = profileId;
            return existing;
        }

        var consultant = new ConsultantEntity { UserId = userId, TeamId = teamId, ProfileId = profileId };
        db.Consultants.Add(consultant);
        return consultant;
    }

    private static async Task<ForgeUser?> CreateUserIfAbsent(
        UserManager<ForgeUser> userManager,
        string email,
        string userName,
        string firstName,
        string lastName,
        string role,
        int? teamId = null)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null) return null;

        var user = new ForgeUser
        {
            UserName = userName,
            Email = email,
            EmailConfirmed = true,
            FirstName = firstName,
            LastName = lastName,
        };

        var result = await userManager.CreateAsync(user, "UserPassword123!");
        if (!result.Succeeded) return null;

        await userManager.AddToRoleAsync(user, role);
        if (teamId.HasValue)
            await userManager.AddClaimAsync(user, new Claim("team", teamId.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)));

        return user;
    }

    private static async Task SeedDemoData(
        AppDbContext db,
        ForgeUser nathalie,
        ForgeUser javaCoach,
        ForgeUser lea,
        ForgeUser sander,
        ForgeUser thomas,
        ForgeUser? ana,
        ForgeUser? javier,
        ForgeUser? kim)
    {
        var dotnetProfile = await db.CompetenceCentreProfiles.FirstOrDefaultAsync(p => p.Name == ".NET");
        var javaProfile = await db.CompetenceCentreProfiles.FirstOrDefaultAsync(p => p.Name == "Java");

        // Upsert consultant records — create if absent, fix TeamId if 0
        var leaConsultant = await UpsertConsultant(db, lea.Id, teamId: 2, profileId: dotnetProfile?.Id);
        var sanderConsultant = await UpsertConsultant(db, sander.Id, teamId: 1, profileId: javaProfile?.Id);
        var thomasConsultant = await UpsertConsultant(db, thomas.Id, teamId: 2, profileId: dotnetProfile?.Id);
        if (ana is not null) await UpsertConsultant(db, ana.Id, teamId: 2, profileId: dotnetProfile?.Id);
        if (javier is not null) await UpsertConsultant(db, javier.Id, teamId: 1, profileId: javaProfile?.Id);
        if (kim is not null) await UpsertConsultant(db, kim.Id, teamId: 4, profileId: null);
        await db.SaveChangesAsync();

        // Skip goals/sessions seeding if already present
        if (await db.Goals.AnyAsync()) return;

        // Skills needed for goals (looked up by name)
        var cleanCode = await db.Skills.FirstAsync(s => s.Name == "Clean Code");
        var efCore = await db.Skills.FirstAsync(s => s.Name == "Entity Framework Core");
        var aspNet = await db.Skills.FirstAsync(s => s.Name == "ASP.NET Core");
        var java = await db.Skills.FirstAsync(s => s.Name == "Java");
        var springBoot = await db.Skills.FirstAsync(s => s.Name == "Spring Boot");
        var testingFundamentals = await db.Skills.FirstAsync(s => s.Name == "Testing Fundamentals");

        // Resources for Lea's goals (added by Nathalie as coach)
        var cleanCodeBook = new ResourceEntity
        {
            Title = "Clean Code by Robert C. Martin",
            Url = "https://www.oreilly.com/library/view/clean-code-a/9780136083238/",
            Type = ResourceType.Book,
            Skill = cleanCode,
            FromNiveau = 2,
            ToNiveau = 4,
            AddedByUserId = nathalie.Id,
        };
        var efCoreDocs = new ResourceEntity
        {
            Title = "EF Core Getting Started",
            Url = "https://learn.microsoft.com/en-us/ef/core/get-started/overview/first-app",
            Type = ResourceType.Documentation,
            Skill = efCore,
            FromNiveau = 1,
            ToNiveau = 3,
            AddedByUserId = nathalie.Id,
        };
        var aspNetTutorial = new ResourceEntity
        {
            Title = "ASP.NET Core Web API Tutorial",
            Url = "https://learn.microsoft.com/en-us/aspnet/core/tutorials/first-web-api",
            Type = ResourceType.Documentation,
            Skill = aspNet,
            FromNiveau = 1,
            ToNiveau = 3,
            AddedByUserId = nathalie.Id,
        };
        db.Resources.AddRange(cleanCodeBook, efCoreDocs, aspNetTutorial);
        await db.SaveChangesAsync();

        // Lea's 3 goals with linked resources (set by Nathalie)
        var leaGoalCleanCode = new GoalEntity
        {
            ConsultantUserId = lea.Id,
            CoachUserId = nathalie.Id,
            Skill = cleanCode,
            CurrentNiveau = 1,
            TargetNiveau = 3,
            Deadline = DateTime.UtcNow.AddMonths(3),
            GoalResources = [new GoalResourceEntity { Resource = cleanCodeBook }],
        };
        var leaGoalEfCore = new GoalEntity
        {
            ConsultantUserId = lea.Id,
            CoachUserId = nathalie.Id,
            Skill = efCore,
            CurrentNiveau = 0,
            TargetNiveau = 2,
            Deadline = DateTime.UtcNow.AddMonths(2),
            GoalResources = [new GoalResourceEntity { Resource = efCoreDocs }],
        };
        var leaGoalAspNet = new GoalEntity
        {
            ConsultantUserId = lea.Id,
            CoachUserId = nathalie.Id,
            Skill = aspNet,
            CurrentNiveau = 0,
            TargetNiveau = 2,
            Deadline = DateTime.UtcNow.AddMonths(2),
            GoalResources = [new GoalResourceEntity { Resource = aspNetTutorial }],
        };
        db.Goals.AddRange(leaGoalCleanCode, leaGoalEfCore, leaGoalAspNet);
        await db.SaveChangesAsync();

        // Readiness flag on Lea's Clean Code goal (so Nathalie's dashboard shows it)
        db.ReadinessFlags.Add(new ReadinessFlagEntity
        {
            GoalId = leaGoalCleanCode.Id,
            RaisedAt = DateTime.UtcNow.AddDays(-2),
        });

        // Sander's 3 onboarding goals (set before first login by Java coach)
        db.Goals.AddRange(
            new GoalEntity
            {
                ConsultantUserId = sander.Id,
                CoachUserId = javaCoach.Id,
                Skill = java,
                CurrentNiveau = 0,
                TargetNiveau = 2,
                Deadline = DateTime.UtcNow.AddMonths(1),
            },
            new GoalEntity
            {
                ConsultantUserId = sander.Id,
                CoachUserId = javaCoach.Id,
                Skill = springBoot,
                CurrentNiveau = 0,
                TargetNiveau = 1,
                Deadline = DateTime.UtcNow.AddMonths(2),
            },
            new GoalEntity
            {
                ConsultantUserId = sander.Id,
                CoachUserId = javaCoach.Id,
                Skill = testingFundamentals,
                CurrentNiveau = 0,
                TargetNiveau = 1,
                Deadline = DateTime.UtcNow.AddMonths(2),
            });

        // Thomas: last coaching session closed 23 days ago (triggers inactivity alert)
        db.CoachingSessions.Add(new CoachingSessionEntity
        {
            ConsultantUserId = thomas.Id,
            CoachUserId = nathalie.Id,
            StartedAt = DateTime.UtcNow.AddDays(-25),
            ClosedAt = DateTime.UtcNow.AddDays(-23),
            Notes = "Initial onboarding session.",
        });

        await db.SaveChangesAsync();
    }
}
