namespace Itenium.SkillForge.Services.Import;

/// <summary>
/// Aggregates all parsed rows from a catalogue import.
/// Passed from the parser to the importer for DB persistence.
/// </summary>
public sealed record ParsedCatalogue(
    IReadOnlyList<SkillRow> Skills,
    IReadOnlyList<SkillLevelRow> Levels,
    IReadOnlyList<SkillPrerequisiteRow> Prerequisites,
    IReadOnlyList<ProfileSkillRow> ProfileSkills,
    IReadOnlyList<SeniorityThresholdRow> SeniorityThresholds);
