using Itenium.SkillForge.Entities.Goals;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Itenium.SkillForge.Data.Configurations;

internal sealed class GoalConfiguration : IEntityTypeConfiguration<GoalEntity>
{
    public void Configure(EntityTypeBuilder<GoalEntity> builder)
    {
        // Coach dashboard queries goals by consultant and by coach
        builder.HasIndex(x => x.ConsultantUserId);
        builder.HasIndex(x => x.CoachUserId);

        // Cascade is handled via ReadinessFlagConfiguration — noted here for clarity.
        // Deleting a Goal cascades to its ReadinessFlag (intended: flag has no meaning without goal).
    }
}
