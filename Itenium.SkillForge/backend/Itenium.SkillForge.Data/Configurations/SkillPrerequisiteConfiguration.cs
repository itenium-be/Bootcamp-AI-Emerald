using Itenium.SkillForge.Entities.Skills;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Itenium.SkillForge.Data.Configurations;

internal sealed class SkillPrerequisiteConfiguration : IEntityTypeConfiguration<SkillPrerequisiteEntity>
{
    public void Configure(EntityTypeBuilder<SkillPrerequisiteEntity> builder)
    {
        builder.HasKey(x => new { x.SkillId, x.RequiredSkillId });

        builder.HasOne(x => x.Skill)
            .WithMany(x => x.Prerequisites)
            .HasForeignKey(x => x.SkillId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.RequiredSkill)
            .WithMany()
            .HasForeignKey(x => x.RequiredSkillId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
