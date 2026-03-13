namespace Itenium.SkillForge.Entities;

/// <summary>
/// Marker interface for entities that are scoped to a team.
/// Entities implementing this interface will have team-based query filters applied.
/// </summary>
public interface ITeamScoped
{
    int TeamId { get; }
}
