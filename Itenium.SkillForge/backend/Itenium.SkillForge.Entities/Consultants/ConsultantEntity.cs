using System.ComponentModel.DataAnnotations;
using Itenium.SkillForge.Entities.Profiles;

namespace Itenium.SkillForge.Entities.Consultants;

/// <summary>
/// SkillForge-specific data for a consultant user.
/// UserId references the identity user (ForgeUser) — one record per consultant.
/// </summary>
public class ConsultantEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(EntityConstants.UserIdMaxLength)]
    public required string UserId { get; set; }

    public int? ProfileId { get; set; }

    public CompetenceCentreProfileEntity? Profile { get; set; }

    /// <summary>Derived from ArchivedAt — no separate storage needed.</summary>
    public bool IsArchived => ArchivedAt.HasValue;

    public DateTime? ArchivedAt { get; set; }
}
