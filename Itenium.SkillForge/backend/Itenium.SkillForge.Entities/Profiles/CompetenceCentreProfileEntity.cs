using System.ComponentModel.DataAnnotations;

namespace Itenium.SkillForge.Entities.Profiles;

/// <summary>
/// One of the four competence centre profiles: .NET, Java, PO&amp;Analysis, QA.
/// Defines the subset of skills relevant for consultants in this centre.
/// </summary>
public class CompetenceCentreProfileEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public required string Name { get; set; }

    public ICollection<ProfileSkillEntity> ProfileSkills { get; set; } = [];

    public ICollection<SeniorityThresholdEntity> SeniorityThresholds { get; set; } = [];

    public override string ToString() => Name;
}
