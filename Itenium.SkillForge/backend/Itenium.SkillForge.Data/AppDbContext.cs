using Itenium.Forge.Security.OpenIddict;
using Itenium.SkillForge.Entities;
using Itenium.SkillForge.Entities.Coaching;
using Itenium.SkillForge.Entities.Consultants;
using Itenium.SkillForge.Entities.Goals;
using Itenium.SkillForge.Entities.Profiles;
using Itenium.SkillForge.Entities.Resources;
using Itenium.SkillForge.Entities.Skills;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.Data;

public class AppDbContext : ForgeIdentityDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    // Existing entities
    public DbSet<TeamEntity> Teams => Set<TeamEntity>();
    public DbSet<CourseEntity> Courses => Set<CourseEntity>();

    // Skills
    public DbSet<SkillCategoryEntity> SkillCategories => Set<SkillCategoryEntity>();
    public DbSet<SkillEntity> Skills => Set<SkillEntity>();
    public DbSet<SkillLevelEntity> SkillLevels => Set<SkillLevelEntity>();
    public DbSet<SkillPrerequisiteEntity> SkillPrerequisites => Set<SkillPrerequisiteEntity>();

    // Profiles
    public DbSet<CompetenceCentreProfileEntity> CompetenceCentreProfiles => Set<CompetenceCentreProfileEntity>();
    public DbSet<ProfileSkillEntity> ProfileSkills => Set<ProfileSkillEntity>();
    public DbSet<SeniorityThresholdEntity> SeniorityThresholds => Set<SeniorityThresholdEntity>();

    // Consultants
    public DbSet<ConsultantEntity> Consultants => Set<ConsultantEntity>();

    // Goals
    public DbSet<GoalEntity> Goals => Set<GoalEntity>();
    public DbSet<GoalResourceEntity> GoalResources => Set<GoalResourceEntity>();

    // Resources
    public DbSet<ResourceEntity> Resources => Set<ResourceEntity>();
    public DbSet<ResourceCompletionEntity> ResourceCompletions => Set<ResourceCompletionEntity>();
    public DbSet<ResourceRatingEntity> ResourceRatings => Set<ResourceRatingEntity>();

    // Coaching
    public DbSet<ReadinessFlagEntity> ReadinessFlags => Set<ReadinessFlagEntity>();
    public DbSet<CoachingSessionEntity> CoachingSessions => Set<CoachingSessionEntity>();
    public DbSet<SkillValidationEntity> SkillValidations => Set<SkillValidationEntity>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
