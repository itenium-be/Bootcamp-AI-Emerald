using Itenium.SkillForge.Entities.Resources;

namespace Itenium.SkillForge.Services.Resources;

public record ResourceDto(
    int Id,
    string Title,
    string Url,
    ResourceType Type,
    int SkillId,
    string SkillName,
    int FromNiveau,
    int ToNiveau,
    string AddedByUserId,
    DateTime AddedAt,
    int CompletionCount,
    int PositiveRatings,
    int NegativeRatings);

public record CreateResourceRequest(
    string Title,
    string Url,
    ResourceType Type,
    int SkillId,
    int FromNiveau,
    int ToNiveau);
