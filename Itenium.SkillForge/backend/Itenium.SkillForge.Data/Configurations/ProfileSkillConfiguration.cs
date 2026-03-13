using Itenium.SkillForge.Entities.Profiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Itenium.SkillForge.Data.Configurations;

internal sealed class ProfileSkillConfiguration : IEntityTypeConfiguration<ProfileSkillEntity>
{
    public void Configure(EntityTypeBuilder<ProfileSkillEntity> builder)
    {
        builder.HasKey(x => new { x.ProfileId, x.SkillId });
    }
}
