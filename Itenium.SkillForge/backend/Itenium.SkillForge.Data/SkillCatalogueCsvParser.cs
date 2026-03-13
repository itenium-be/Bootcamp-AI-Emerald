using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Itenium.SkillForge.Services.Import;

namespace Itenium.SkillForge.Data;

/// <summary>
/// Parses CSV content strings into typed row collections.
/// All methods are pure (no DB interaction) and can be called in any order.
/// </summary>
internal static class SkillCatalogueCsvParser
{
    private static readonly CsvConfiguration Config = new(CultureInfo.InvariantCulture)
    {
        HasHeaderRecord = true,
        MissingFieldFound = null,
    };

    public static IReadOnlyList<SkillRow> ParseSkills(string csv) =>
        Parse<SkillRow>(csv);

    public static IReadOnlyList<SkillLevelRow> ParseLevels(string csv) =>
        Parse<SkillLevelRow>(csv);

    public static IReadOnlyList<SkillPrerequisiteRow> ParsePrerequisites(string csv) =>
        Parse<SkillPrerequisiteRow>(csv);

    public static IReadOnlyList<ProfileSkillRow> ParseProfileSkills(string csv) =>
        Parse<ProfileSkillRow>(csv);

    public static IReadOnlyList<SeniorityThresholdRow> ParseSeniorityThresholds(string csv) =>
        Parse<SeniorityThresholdRow>(csv);

    public static ParsedCatalogue ParseAll(
        string skillsCsv,
        string levelsCsv,
        string prerequisitesCsv,
        string profileSkillsCsv,
        string seniorityThresholdsCsv) => new(
            ParseSkills(skillsCsv),
            ParseLevels(levelsCsv),
            ParsePrerequisites(prerequisitesCsv),
            ParseProfileSkills(profileSkillsCsv),
            ParseSeniorityThresholds(seniorityThresholdsCsv));

    private static IReadOnlyList<T> Parse<T>(string csv)
    {
        using var reader = new StringReader(csv);
        using var csvReader = new CsvReader(reader, Config);
        return [.. csvReader.GetRecords<T>()];
    }
}
