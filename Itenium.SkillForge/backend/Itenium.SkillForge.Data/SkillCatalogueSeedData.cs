using Itenium.SkillForge.Entities.Profiles;
using Itenium.SkillForge.Entities.Skills;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.Data;

/// <summary>
/// Representative seed data for the .NET and Java competence centre profiles.
/// Idempotent: checks SkillCategories before inserting to avoid duplicates on restart.
/// </summary>
public static class SkillCatalogueSeedData
{
    public static async Task Seed(AppDbContext db)
    {
        if (await db.SkillCategories.AnyAsync()) return;

        var categories = SeedCategories(db);
        await db.SaveChangesAsync();

        var skills = SeedSkills(db, categories);
        await db.SaveChangesAsync();

        SeedPrerequisites(db, skills);
        await db.SaveChangesAsync();

        var profiles = SeedProfiles(db);
        await db.SaveChangesAsync();

        SeedProfileSkills(db, profiles, skills);
        await db.SaveChangesAsync();

        SeedSeniorityThresholds(db, profiles, skills);
        await db.SaveChangesAsync();
    }

    // ────────────────────────────────────────────────────────
    // Categories
    // ────────────────────────────────────────────────────────

    private static Categories SeedCategories(AppDbContext db)
    {
        var lang = new SkillCategoryEntity { Name = "Language & Runtime" };
        var web = new SkillCategoryEntity { Name = "Web & API" };
        var data = new SkillCategoryEntity { Name = "Data & Persistence" };
        var testing = new SkillCategoryEntity { Name = "Testing" };
        var arch = new SkillCategoryEntity { Name = "Architecture & Design" };
        var tooling = new SkillCategoryEntity { Name = "Tooling & DevOps" };
        db.SkillCategories.AddRange(lang, web, data, testing, arch, tooling);
        return new(lang, web, data, testing, arch, tooling);
    }

    // ────────────────────────────────────────────────────────
    // Skills
    // ────────────────────────────────────────────────────────

    private static Skills SeedSkills(AppDbContext db, Categories cat)
    {
        // ── Shared ──────────────────────────────────────────
        var cleanCode = Skill("Clean Code", cat.Arch, 5,
            description: "Write readable, maintainable, self-documenting code.",
            levels: [
                "Applies basic naming conventions and avoids magic numbers",
                "Consistently writes short, focused methods and classes",
                "Proactively refactors duplication; uses meaningful abstractions",
                "Leads code review with quality focus; coaches team on clean code principles",
                "Defines team standards; contributes to internal clean-code guides",
            ]);

        var git = CheckboxSkill("Git", cat.Tooling,
            description: "Version control with Git: branching, merging, rebasing.");

        var docker = Skill("Docker", cat.Tooling, 3,
            description: "Container-based packaging and runtime for applications.",
            levels: [
                "Runs and inspects existing containers and images",
                "Writes Dockerfiles, builds images, and manages volumes and networks",
                "Designs multi-stage builds and docker-compose setups for local dev",
            ]);

        var testingFundamentals = Skill("Testing Fundamentals", cat.Testing, 4,
            description: "Unit, integration, and end-to-end testing principles.",
            levels: [
                "Writes basic unit tests for isolated functions",
                "Uses mocking frameworks and understands test doubles",
                "Applies TDD; writes integration tests against real dependencies",
                "Designs test strategy; balances unit/integration/e2e pyramid",
            ]);

        var cicd = Skill("CI/CD", cat.Tooling, 3,
            description: "Continuous integration and delivery pipelines.",
            levels: [
                "Understands pipeline concepts and can read existing configurations",
                "Configures build and test stages in a pipeline (e.g., GitHub Actions)",
                "Designs multi-environment deployment pipelines with rollback strategy",
            ]);

        // ── .NET ────────────────────────────────────────────
        var csharp = Skill("C#", cat.Lang, 7,
            description: "The C# programming language, from fundamentals to advanced CLR features.",
            levels: [
                "Writes simple procedural code using basic types and control flow",
                "Uses OOP: classes, inheritance, interfaces, and access modifiers",
                "Applies generics, delegates, events, and LINQ",
                "Writes async/await code; understands Task, ValueTask, and threading basics",
                "Uses advanced patterns: expression trees, reflection, and dynamic typing",
                "Designs reusable libraries with a minimal, expressive public API",
                "Deep CLR knowledge: memory model, GC tuning, and performance profiling",
            ]);

        var dotnet = Skill(".NET", cat.Lang, 5,
            description: ".NET runtime, SDK, and ecosystem including NuGet and hosting model.",
            levels: [
                "Understands the SDK/runtime separation; runs and debugs .NET applications",
                "Configures dependency injection, configuration, and the generic host",
                "Manages NuGet packages; uses IOptions, logging abstractions, and middleware",
                "Optimises startup time, manages versioning, and builds NuGet packages",
                "Contributes to shared infrastructure libraries; understands runtime internals",
            ]);

        var aspnet = Skill("ASP.NET Core", cat.Web, 5,
            description: "Building REST APIs and web applications with ASP.NET Core.",
            levels: [
                "Creates minimal APIs or basic MVC controllers returning JSON",
                "Configures middleware pipeline, routing, and dependency injection",
                "Implements authentication/authorization, model validation, and error handling",
                "Designs versioned APIs with OpenAPI documentation and problem details",
                "Optimises performance: response caching, output compression, health checks",
            ]);

        var efcore = Skill("Entity Framework Core", cat.Data, 5,
            description: "ORM for .NET using EF Core with code-first migrations.",
            levels: [
                "Performs basic CRUD operations with a DbContext",
                "Uses navigation properties, eager loading, and migrations",
                "Configures relationships and indexes using Fluent API",
                "Writes raw SQL for performance-critical queries; uses projections",
                "Tunes query performance; handles concurrency conflicts and bulk operations",
            ]);

        var linq = Skill("LINQ", cat.Lang, 4,
            description: "Language Integrated Query for collections and data sources.",
            levels: [
                "Uses basic operators: Select, Where, First, OrderBy, ToList",
                "Writes multi-step pipelines with joins, grouping, and aggregations",
                "Understands deferred execution; distinguishes IQueryable from IEnumerable",
                "Optimises LINQ-to-SQL translation; avoids N+1 and Cartesian explosion",
            ]);

        var di = Skill("Dependency Injection", cat.Arch, 3,
            description: "IoC and DI patterns; Microsoft.Extensions.DependencyInjection.",
            levels: [
                "Registers and resolves services; understands constructor injection",
                "Chooses correct lifetimes (Singleton, Scoped, Transient) and avoids captive dependencies",
                "Designs modular service registration using extension methods and modules",
            ]);

        var xunit = Skill("xUnit", cat.Testing, 3,
            description: "Unit testing with xUnit.net for .NET projects.",
            levels: [
                "Writes Fact and Theory tests; understands test runner and output",
                "Uses fixtures and collection fixtures for shared setup; applies custom assertions",
                "Designs test projects with shared infrastructure (builders, fakes, test doubles)",
            ]);

        var azure = Skill("Azure", cat.Tooling, 5,
            description: "Microsoft Azure cloud platform for hosting and managed services.",
            levels: [
                "Navigates Azure Portal; deploys a pre-configured web app or function",
                "Provisions and connects App Service, SQL Database, and Storage",
                "Manages identity (RBAC, Managed Identity) and configures Key Vault secrets",
                "Designs multi-region deployments with load balancing and auto-scaling",
                "Architects cost-optimised, secure cloud-native solutions with IaC (Bicep/Terraform)",
            ]);

        // ── Java ────────────────────────────────────────────
        var java = Skill("Java", cat.Lang, 7,
            description: "The Java programming language from fundamentals to advanced JVM topics.",
            levels: [
                "Writes Java programs using basic OOP: classes, interfaces, and collections",
                "Uses generics, exception handling, and the Collections framework effectively",
                "Applies Streams, lambdas, Optional, and functional interfaces",
                "Understands JVM internals: GC basics, class loading, and memory areas",
                "Writes concurrent code with threads, locks, and the Executor framework",
                "Uses modern Java features (records, sealed classes, pattern matching, Java 21+)",
                "Tunes JVM performance: profiling, heap dump analysis, and GC configuration",
            ]);

        var springBoot = Skill("Spring Boot", cat.Web, 5,
            description: "Building REST APIs and services with Spring Boot.",
            levels: [
                "Creates REST controllers and configures application startup",
                "Uses Spring beans, profiles, and application properties effectively",
                "Implements Spring Security, transaction management, and bean validation",
                "Designs modular applications with Spring Data and test slices (@WebMvcTest etc.)",
                "Builds custom starters; tunes auto-configuration and actuator endpoints",
            ]);

        var hibernate = Skill("Hibernate / JPA", cat.Data, 5,
            description: "ORM for Java using Hibernate and the JPA specification.",
            levels: [
                "Performs basic CRUD via EntityManager or Spring Data repositories",
                "Maps associations (OneToMany, ManyToMany) and uses JPQL queries",
                "Configures cascade types, fetch strategies, and schema generation",
                "Writes native SQL and optimises queries using query hints and projections",
                "Tunes second-level cache, batch processing, and handles optimistic locking",
            ]);

        var maven = Skill("Maven", cat.Tooling, 3,
            description: "Build automation and dependency management with Apache Maven.",
            levels: [
                "Understands POM structure; runs build, test, and package goals",
                "Manages transitive dependencies, exclusions, and multi-module projects",
                "Configures plugins, profiles, and CI-friendly version management",
            ]);

        var junit = Skill("JUnit 5", cat.Testing, 3,
            description: "Unit testing with JUnit 5 for Java projects.",
            levels: [
                "Writes @Test methods; uses assertions and @BeforeEach/@AfterEach",
                "Uses @ParameterizedTest, extensions, and Mockito for test doubles",
                "Designs test infrastructure with custom extensions and Spring test slices",
            ]);

        var javaStreams = Skill("Java Streams", cat.Lang, 4,
            description: "Functional-style data processing with the Java Streams API.",
            levels: [
                "Uses map, filter, collect, and basic terminal operations",
                "Writes complex pipelines with flatMap, groupingBy, and custom collectors",
                "Understands lazy evaluation and stream lifecycle; avoids common pitfalls",
                "Applies parallel streams appropriately; measures and justifies performance gains",
            ]);

        var springSecurity = Skill("Spring Security", cat.Web, 4,
            description: "Authentication and authorisation with Spring Security.",
            levels: [
                "Configures basic HTTP security: form login, HTTP Basic, and permit rules",
                "Implements JWT or OAuth2 resource server; customises UserDetailsService",
                "Secures method-level access with @PreAuthorize; writes security integration tests",
                "Designs multi-tenant security; customises filter chains and authentication providers",
            ]);

        var aws = Skill("AWS", cat.Tooling, 5,
            description: "Amazon Web Services cloud platform for hosting and managed services.",
            levels: [
                "Navigates AWS Console; deploys a pre-configured EC2 or Lambda function",
                "Provisions and connects Elastic Beanstalk, RDS, and S3",
                "Manages IAM roles and policies; uses Secrets Manager and Parameter Store",
                "Designs multi-AZ deployments with ELB, Auto Scaling, and CloudWatch",
                "Architects cost-optimised, secure cloud-native solutions with IaC (CDK/Terraform)",
            ]);

        db.Skills.AddRange(
            cleanCode, git, docker, testingFundamentals, cicd,
            csharp, dotnet, aspnet, efcore, linq, di, xunit, azure,
            java, springBoot, hibernate, maven, junit, javaStreams, springSecurity, aws);

        return new(cleanCode, git, docker, testingFundamentals, cicd,
            csharp, dotnet, aspnet, efcore, linq, di, xunit, azure,
            java, springBoot, hibernate, maven, junit, javaStreams, springSecurity, aws);
    }

    // ────────────────────────────────────────────────────────
    // Prerequisites
    // ────────────────────────────────────────────────────────

    private static void SeedPrerequisites(AppDbContext db, Skills s)
    {
        db.SkillPrerequisites.AddRange(
            // .NET chain
            Prereq(s.AspNet, s.CSharp, requiredNiveau: 2),
            Prereq(s.EfCore, s.AspNet, requiredNiveau: 1),
            Prereq(s.EfCore, s.CSharp, requiredNiveau: 2),
            Prereq(s.Linq, s.CSharp, requiredNiveau: 2),
            Prereq(s.Di, s.DotNet, requiredNiveau: 1),
            Prereq(s.Azure, s.AspNet, requiredNiveau: 2),
            // Java chain
            Prereq(s.SpringBoot, s.Java, requiredNiveau: 2),
            Prereq(s.Hibernate, s.SpringBoot, requiredNiveau: 1),
            Prereq(s.Hibernate, s.Java, requiredNiveau: 2),
            Prereq(s.JavaStreams, s.Java, requiredNiveau: 2),
            Prereq(s.SpringSecurity, s.SpringBoot, requiredNiveau: 2),
            Prereq(s.Aws, s.SpringBoot, requiredNiveau: 2));
    }

    // ────────────────────────────────────────────────────────
    // Competence centre profiles
    // ────────────────────────────────────────────────────────

    private static Profiles SeedProfiles(AppDbContext db)
    {
        var dotnet = new CompetenceCentreProfileEntity { Name = ".NET" };
        var java = new CompetenceCentreProfileEntity { Name = "Java" };
        db.CompetenceCentreProfiles.AddRange(dotnet, java);
        return new(dotnet, java);
    }

    private static void SeedProfileSkills(AppDbContext db, Profiles p, Skills s)
    {
        // Shared skills for both profiles
        SkillEntity[] shared = [s.CleanCode, s.Git, s.Docker, s.TestingFundamentals, s.CiCd];

        // .NET-specific
        SkillEntity[] dotnetSpecific = [s.CSharp, s.DotNet, s.AspNet, s.EfCore, s.Linq, s.Di, s.XUnit, s.Azure];

        // Java-specific
        SkillEntity[] javaSpecific = [s.Java, s.SpringBoot, s.Hibernate, s.Maven, s.Junit, s.JavaStreams, s.SpringSecurity, s.Aws];

        foreach (var skill in shared.Concat(dotnetSpecific))
            db.ProfileSkills.Add(new ProfileSkillEntity { Profile = p.DotNet, Skill = skill });

        foreach (var skill in shared.Concat(javaSpecific))
            db.ProfileSkills.Add(new ProfileSkillEntity { Profile = p.Java, Skill = skill });
    }

    private static void SeedSeniorityThresholds(AppDbContext db, Profiles p, Skills s)
    {
        // .NET thresholds
        db.SeniorityThresholds.AddRange(
            // Junior: can write basic code in the stack
            Threshold(p.DotNet, s.CSharp, SeniorityLevel.Junior, minNiveau: 2),
            Threshold(p.DotNet, s.DotNet, SeniorityLevel.Junior, minNiveau: 1),
            Threshold(p.DotNet, s.AspNet, SeniorityLevel.Junior, minNiveau: 1),
            Threshold(p.DotNet, s.CleanCode, SeniorityLevel.Junior, minNiveau: 1),
            // Medior: independently delivers features end-to-end
            Threshold(p.DotNet, s.CSharp, SeniorityLevel.Medior, minNiveau: 4),
            Threshold(p.DotNet, s.DotNet, SeniorityLevel.Medior, minNiveau: 3),
            Threshold(p.DotNet, s.AspNet, SeniorityLevel.Medior, minNiveau: 3),
            Threshold(p.DotNet, s.EfCore, SeniorityLevel.Medior, minNiveau: 2),
            Threshold(p.DotNet, s.CleanCode, SeniorityLevel.Medior, minNiveau: 3),
            // Senior: leads design, mentors others
            Threshold(p.DotNet, s.CSharp, SeniorityLevel.Senior, minNiveau: 6),
            Threshold(p.DotNet, s.DotNet, SeniorityLevel.Senior, minNiveau: 4),
            Threshold(p.DotNet, s.AspNet, SeniorityLevel.Senior, minNiveau: 4),
            Threshold(p.DotNet, s.EfCore, SeniorityLevel.Senior, minNiveau: 4),
            Threshold(p.DotNet, s.CleanCode, SeniorityLevel.Senior, minNiveau: 4));

        // Java thresholds
        db.SeniorityThresholds.AddRange(
            // Junior
            Threshold(p.Java, s.Java, SeniorityLevel.Junior, minNiveau: 2),
            Threshold(p.Java, s.SpringBoot, SeniorityLevel.Junior, minNiveau: 1),
            Threshold(p.Java, s.CleanCode, SeniorityLevel.Junior, minNiveau: 1),
            // Medior
            Threshold(p.Java, s.Java, SeniorityLevel.Medior, minNiveau: 4),
            Threshold(p.Java, s.SpringBoot, SeniorityLevel.Medior, minNiveau: 3),
            Threshold(p.Java, s.Hibernate, SeniorityLevel.Medior, minNiveau: 2),
            Threshold(p.Java, s.CleanCode, SeniorityLevel.Medior, minNiveau: 3),
            // Senior
            Threshold(p.Java, s.Java, SeniorityLevel.Senior, minNiveau: 6),
            Threshold(p.Java, s.SpringBoot, SeniorityLevel.Senior, minNiveau: 4),
            Threshold(p.Java, s.Hibernate, SeniorityLevel.Senior, minNiveau: 4),
            Threshold(p.Java, s.CleanCode, SeniorityLevel.Senior, minNiveau: 4));
    }

    // ────────────────────────────────────────────────────────
    // Helpers
    // ────────────────────────────────────────────────────────

    private static SkillEntity Skill(
        string name,
        SkillCategoryEntity category,
        int levelCount,
        string? description,
        string[] levels)
    {
        var skill = new SkillEntity
        {
            Name = name,
            Category = category,
            Description = description,
            LevelCount = levelCount,
        };
        for (var i = 0; i < levels.Length; i++)
            skill.Levels.Add(new SkillLevelEntity { Niveau = i + 1, Descriptor = levels[i] });
        return skill;
    }

    private static SkillEntity CheckboxSkill(string name, SkillCategoryEntity category, string? description = null)
        => new() { Name = name, Category = category, Description = description, LevelCount = 1 };

    private static SkillPrerequisiteEntity Prereq(SkillEntity skill, SkillEntity required, int requiredNiveau)
        => new() { Skill = skill, RequiredSkill = required, RequiredMinNiveau = requiredNiveau };

    private static SeniorityThresholdEntity Threshold(
        CompetenceCentreProfileEntity profile,
        SkillEntity skill,
        SeniorityLevel level,
        int minNiveau)
        => new() { Profile = profile, Skill = skill, SeniorityLevel = level, MinNiveau = minNiveau };

    // ────────────────────────────────────────────────────────
    // Value tuples for strongly typed grouping
    // ────────────────────────────────────────────────────────

    private sealed record Categories(
        SkillCategoryEntity Lang,
        SkillCategoryEntity Web,
        SkillCategoryEntity Data,
        SkillCategoryEntity Testing,
        SkillCategoryEntity Arch,
        SkillCategoryEntity Tooling);

    private sealed record Skills(
        // Shared
        SkillEntity CleanCode,
        SkillEntity Git,
        SkillEntity Docker,
        SkillEntity TestingFundamentals,
        SkillEntity CiCd,
        // .NET
        SkillEntity CSharp,
        SkillEntity DotNet,
        SkillEntity AspNet,
        SkillEntity EfCore,
        SkillEntity Linq,
        SkillEntity Di,
        SkillEntity XUnit,
        SkillEntity Azure,
        // Java
        SkillEntity Java,
        SkillEntity SpringBoot,
        SkillEntity Hibernate,
        SkillEntity Maven,
        SkillEntity Junit,
        SkillEntity JavaStreams,
        SkillEntity SpringSecurity,
        SkillEntity Aws);

    private sealed record Profiles(
        CompetenceCentreProfileEntity DotNet,
        CompetenceCentreProfileEntity Java);
}
