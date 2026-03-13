using Itenium.SkillForge.Entities.Goals;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Itenium.SkillForge.Data.Configurations;

internal sealed class GoalResourceConfiguration : IEntityTypeConfiguration<GoalResourceEntity>
{
    public void Configure(EntityTypeBuilder<GoalResourceEntity> builder)
    {
        builder.HasKey(x => new { x.GoalId, x.ResourceId });
    }
}
