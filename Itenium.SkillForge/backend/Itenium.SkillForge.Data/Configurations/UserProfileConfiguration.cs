using Itenium.Forge.Security.OpenIddict;
using Itenium.SkillForge.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Itenium.SkillForge.Data.Configurations;

internal sealed class UserProfileConfiguration : IEntityTypeConfiguration<UserProfileEntity>
{
    public void Configure(EntityTypeBuilder<UserProfileEntity> builder)
    {
        builder.HasKey(x => x.UserId);
        builder.HasOne<ForgeUser>()
            .WithOne()
            .HasForeignKey<UserProfileEntity>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
