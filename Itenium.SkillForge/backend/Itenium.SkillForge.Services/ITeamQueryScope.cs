namespace Itenium.SkillForge.Services;

/// <summary>
/// Provides the current user's team scope for query filtering.
/// </summary>
public interface ITeamQueryScope
{
    /// <summary>Gets whether the current user has backoffice access (bypasses team filters).</summary>
    bool IsBackOffice { get; }

    /// <summary>Gets the team IDs the current user belongs to.</summary>
    ICollection<int> TeamIds { get; }

    /// <summary>Returns true if the given teamId is accessible to the current user.</summary>
    bool CanAccessTeam(int teamId);
}
