using Itenium.SkillForge.Entities.Coaching;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Itenium.SkillForge.Data.Configurations;

internal sealed class CoachingSessionConfiguration : IEntityTypeConfiguration<CoachingSessionEntity>
{
    public void Configure(EntityTypeBuilder<CoachingSessionEntity> builder)
    {
        // Activity history queries sessions by coach+consultant pair
        builder.HasIndex(x => new { x.CoachUserId, x.ConsultantUserId });
    }
}
