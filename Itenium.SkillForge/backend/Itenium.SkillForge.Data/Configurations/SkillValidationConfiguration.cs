using Itenium.SkillForge.Entities.Coaching;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Itenium.SkillForge.Data.Configurations;

internal sealed class SkillValidationConfiguration : IEntityTypeConfiguration<SkillValidationEntity>
{
    public void Configure(EntityTypeBuilder<SkillValidationEntity> builder)
    {
        // Roadmap and activity history query validations by consultant
        builder.HasIndex(x => x.ConsultantUserId);

        // Coach needs to look up validations they made
        builder.HasIndex(x => x.CoachUserId);
    }
}
