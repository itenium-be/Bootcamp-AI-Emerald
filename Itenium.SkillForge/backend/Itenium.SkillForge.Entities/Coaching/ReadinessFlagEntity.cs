using System.ComponentModel.DataAnnotations;
using Itenium.SkillForge.Entities.Goals;

namespace Itenium.SkillForge.Entities.Coaching;

/// <summary>
/// A consultant signals they believe they have achieved a goal niveau.
/// Maximum one active flag per goal at any time.
/// Age = days since RaisedAt, computed at read time.
/// </summary>
public class ReadinessFlagEntity
{
    [Key]
    public int Id { get; set; }

    public int GoalId { get; set; }

    public GoalEntity Goal { get; set; } = null!;

    public DateTime RaisedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Set when a coach dismisses the flag after validating or declining.</summary>
    public DateTime? DismissedAt { get; set; }
}
