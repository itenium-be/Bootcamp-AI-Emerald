using System.ComponentModel.DataAnnotations;

namespace Itenium.SkillForge.Entities.Skills;

public class SkillCategoryEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public required string Name { get; set; }

    public ICollection<SkillEntity> Skills { get; set; } = [];

    public override string ToString() => Name;
}
