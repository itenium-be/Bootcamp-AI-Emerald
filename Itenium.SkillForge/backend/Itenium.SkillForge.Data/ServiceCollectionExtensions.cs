using Itenium.SkillForge.Services.Activity;
using Itenium.SkillForge.Services.Coaching;
using Itenium.SkillForge.Services.Goals;
using Itenium.SkillForge.Services.Import;
using Itenium.SkillForge.Services.Profiles;
using Itenium.SkillForge.Services.Resources;
using Itenium.SkillForge.Services.Roadmap;
using Itenium.SkillForge.Services.SkillCatalogue;
using Microsoft.Extensions.DependencyInjection;

namespace Itenium.SkillForge.Data;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all SkillForge infrastructure services (EF Core implementations).
    /// Call from Program.cs: <c>builder.Services.AddSkillForgeInfrastructure();</c>
    /// </summary>
    public static IServiceCollection AddSkillForgeInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<ISkillCatalogueService, SkillCatalogueService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<ISkillCatalogueImporter, SkillCatalogueImporter>();
        services.AddScoped<IRoadmapService, RoadmapService>();
        services.AddScoped<IGoalService, GoalService>();
        services.AddScoped<IResourceService, ResourceService>();
        services.AddScoped<IReadinessFlagService, ReadinessFlagService>();
        services.AddScoped<ISkillValidationService, SkillValidationService>();
        services.AddScoped<ICoachingSessionService, CoachingSessionService>();
        services.AddScoped<ICoachDashboardService, CoachDashboardService>();
        services.AddScoped<IActivityService, ActivityService>();
        return services;
    }
}
