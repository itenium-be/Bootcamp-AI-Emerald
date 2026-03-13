using Itenium.Forge.Controllers;
using Itenium.Forge.HealthChecks;
using Itenium.Forge.Logging;
using Itenium.Forge.Security;
using Itenium.Forge.Security.OpenIddict;
using Itenium.Forge.Settings;
using Itenium.Forge.Swagger;
using Itenium.SkillForge.Data;
using Itenium.SkillForge.Services;
using Itenium.SkillForge.WebApi;
using Microsoft.EntityFrameworkCore;
using Serilog;

Log.Logger = LoggingExtensions.CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    var settings = builder.AddForgeSettings<SkillForgeSettings>();
    builder.AddForgeLogging();

    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    builder.AddForgeOpenIddict<AppDbContext>(options => options.UseNpgsql(connectionString));

    builder.Services.AddScoped<ISkillForgeUser, SkillForgeUser>();
    builder.Services.AddScoped<ITeamQueryScope, TeamQueryScope>();

    builder.AddForgeControllers();
    builder.AddForgeSwagger();
    builder.AddForgeHealthChecks();

    WebApplication app = builder.Build();

    // Apply migrations and seed data
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    await app.SeedOpenIddictDataAsync();
    await app.SeedDevelopmentData();

    app.UseForgeLogging();
    app.UseForgeSecurity();

    app.UseForgeControllers();
    if (app.Environment.IsDevelopment())
    {
        app.UseForgeSwagger();
    }

    app.UseForgeHealthChecks();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}

// Make Program class accessible for WebApplicationFactory in tests
public partial class Program
{
}
