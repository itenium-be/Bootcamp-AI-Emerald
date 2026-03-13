using Itenium.SkillForge.Services.Profiles;
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
        return services;
    }
}
