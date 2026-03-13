using Itenium.SkillForge.Entities.Coaching;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Itenium.SkillForge.Data.Configurations;

internal sealed class ReadinessFlagConfiguration : IEntityTypeConfiguration<ReadinessFlagEntity>
{
    public void Configure(EntityTypeBuilder<ReadinessFlagEntity> builder)
    {
        // Cascade delete: removing a goal removes its readiness flag — correct behaviour.
        builder.HasOne(x => x.Goal)
            .WithOne(x => x.ReadinessFlag)
            .HasForeignKey<ReadinessFlagEntity>(x => x.GoalId)
            .OnDelete(DeleteBehavior.Cascade);

        // Enforce "max one active flag per goal": only one row with DismissedAt IS NULL per GoalId.
        builder.HasIndex(x => x.GoalId)
            .IsUnique()
            .HasFilter("\"DismissedAt\" IS NULL");
    }
}
