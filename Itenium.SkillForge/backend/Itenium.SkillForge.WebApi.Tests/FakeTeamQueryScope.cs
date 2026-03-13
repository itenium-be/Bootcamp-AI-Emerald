using Itenium.SkillForge.Services;

namespace Itenium.SkillForge.WebApi.Tests;

/// <summary>
/// Test double for <see cref="ITeamQueryScope"/>.
/// </summary>
internal sealed class FakeTeamQueryScope : ITeamQueryScope
{
    public FakeTeamQueryScope(bool isBackOffice = false, IList<int>? teamIds = null)
    {
        IsBackOffice = isBackOffice;
        TeamIds = teamIds ?? [];
    }

    public bool IsBackOffice { get; }
    public ICollection<int> TeamIds { get; }

    public bool CanAccessTeam(int teamId)
    {
        if (IsBackOffice) return true;
        return TeamIds.Contains(teamId);
    }
}
