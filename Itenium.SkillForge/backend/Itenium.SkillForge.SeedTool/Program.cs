using Itenium.SkillForge.Data;
using Microsoft.EntityFrameworkCore;

// Connection string: env var DATABASE_URL or first CLI argument
var connectionString =
    Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? (args.Length > 0 ? args[0] : null)
    ?? "Host=localhost;Port=5432;Database=skillforge;Username=skillforge;Password=skillforge";

var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>()
    .UseNpgsql(connectionString);

await using var db = new AppDbContext(optionsBuilder.Options);

Console.WriteLine("Applying migrations…");
await db.Database.MigrateAsync();

var dataDir = Path.Combine(AppContext.BaseDirectory, "data");

var catalogue = SkillCatalogueCsvParser.ParseAll(
    skillsCsv: await File.ReadAllTextAsync(Path.Combine(dataDir, "skills.csv")),
    levelsCsv: await File.ReadAllTextAsync(Path.Combine(dataDir, "levels.csv")),
    prerequisitesCsv: await File.ReadAllTextAsync(Path.Combine(dataDir, "prerequisites.csv")),
    profileSkillsCsv: await File.ReadAllTextAsync(Path.Combine(dataDir, "profile-skills.csv")),
    seniorityThresholdsCsv: await File.ReadAllTextAsync(Path.Combine(dataDir, "seniority-thresholds.csv")));

var importer = new SkillCatalogueImporter(db);
var result = await importer.ImportAsync(catalogue);

Console.WriteLine($"Import complete:");
Console.WriteLine($"  Categories : {result.CategoriesCreated} created");
Console.WriteLine($"  Skills     : {result.SkillsCreated} created");
Console.WriteLine($"  Levels     : {result.LevelsCreated} created");
Console.WriteLine($"  Prerequisites: {result.PrerequisitesCreated} created");
Console.WriteLine($"  Profiles   : {result.ProfilesCreated} created");
Console.WriteLine($"  ProfileSkills: {result.ProfileSkillsCreated} created");
Console.WriteLine($"  Thresholds : {result.ThresholdsCreated} created");
