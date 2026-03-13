using Itenium.SkillForge.Entities.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Itenium.SkillForge.Data.Configurations;

internal sealed class ResourceCompletionConfiguration : IEntityTypeConfiguration<ResourceCompletionEntity>
{
    public void Configure(EntityTypeBuilder<ResourceCompletionEntity> builder)
    {
        // One completion record per user per resource — re-completing updates CompletedAt at the service layer.
        builder.HasIndex(x => new { x.ResourceId, x.UserId })
            .IsUnique();
    }
}
