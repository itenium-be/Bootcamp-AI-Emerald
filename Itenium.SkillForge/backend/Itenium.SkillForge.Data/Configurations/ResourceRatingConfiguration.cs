using Itenium.SkillForge.Entities.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Itenium.SkillForge.Data.Configurations;

internal sealed class ResourceRatingConfiguration : IEntityTypeConfiguration<ResourceRatingEntity>
{
    public void Configure(EntityTypeBuilder<ResourceRatingEntity> builder)
    {
        builder.HasKey(x => new { x.ResourceId, x.UserId });
    }
}
