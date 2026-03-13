using Itenium.SkillForge.Entities.Resources;

namespace Itenium.SkillForge.Entities.Goals;

/// <summary>
/// Links a resource from the library to a specific goal.
/// Composite PK: (GoalId, ResourceId) — configured via Fluent API in AppDbContext.
/// </summary>
public class GoalResourceEntity
{
    public int GoalId { get; set; }

    public GoalEntity Goal { get; set; } = null!;

    public int ResourceId { get; set; }

    public ResourceEntity Resource { get; set; } = null!;
}
