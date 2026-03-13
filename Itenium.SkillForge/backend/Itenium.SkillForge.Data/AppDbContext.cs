using Itenium.Forge.Security.OpenIddict;
using Itenium.SkillForge.Entities;
using Itenium.SkillForge.Entities.Coaching;
using Itenium.SkillForge.Entities.Consultants;
using Itenium.SkillForge.Entities.Goals;
using Itenium.SkillForge.Entities.Resources;
using Microsoft.EntityFrameworkCore;
using CatalogueSkillEntity = Itenium.SkillForge.Entities.Skills.SkillEntity;
using CatalogueSkillPrerequisiteEntity = Itenium.SkillForge.Entities.Skills.SkillPrerequisiteEntity;
using CatalogueSeniorityThresholdEntity = Itenium.SkillForge.Entities.Profiles.SeniorityThresholdEntity;
using CatalogueSkillCategoryEntity = Itenium.SkillForge.Entities.Skills.SkillCategoryEntity;
using CatalogueSkillLevelEntity = Itenium.SkillForge.Entities.Skills.SkillLevelEntity;
using CatalogueCompetenceCentreProfileEntity = Itenium.SkillForge.Entities.Profiles.CompetenceCentreProfileEntity;
using CatalogueProfileSkillEntity = Itenium.SkillForge.Entities.Profiles.ProfileSkillEntity;

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
    public DbSet<UserProfileEntity> UserProfiles => Set<UserProfileEntity>();

    // Skills (new catalogue-based)
    public DbSet<CatalogueSkillCategoryEntity> SkillCategories => Set<CatalogueSkillCategoryEntity>();
    public DbSet<CatalogueSkillEntity> Skills => Set<CatalogueSkillEntity>();
    public DbSet<CatalogueSkillLevelEntity> SkillLevels => Set<CatalogueSkillLevelEntity>();
    public DbSet<CatalogueSkillPrerequisiteEntity> SkillPrerequisites => Set<CatalogueSkillPrerequisiteEntity>();

    // Profiles
    public DbSet<CatalogueCompetenceCentreProfileEntity> CompetenceCentreProfiles => Set<CatalogueCompetenceCentreProfileEntity>();
    public DbSet<CatalogueProfileSkillEntity> ProfileSkills => Set<CatalogueProfileSkillEntity>();
    public DbSet<CatalogueSeniorityThresholdEntity> SeniorityThresholds => Set<CatalogueSeniorityThresholdEntity>();

    // Consultants
    public DbSet<ConsultantEntity> Consultants => Set<ConsultantEntity>();
    public DbSet<ConsultantSkillEntity> ConsultantSkills => Set<ConsultantSkillEntity>();

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
