using Itenium.SkillForge.Entities.Consultants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Itenium.SkillForge.Data.Configurations;

internal sealed class ConsultantConfiguration : IEntityTypeConfiguration<ConsultantEntity>
{
    public void Configure(EntityTypeBuilder<ConsultantEntity> builder)
    {
        builder.HasIndex(x => x.UserId)
            .IsUnique();

        builder.HasQueryFilter(c => c.ArchivedAt == null);
    }
}
