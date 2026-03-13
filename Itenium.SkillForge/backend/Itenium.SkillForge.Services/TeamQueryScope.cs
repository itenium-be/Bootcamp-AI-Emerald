namespace Itenium.SkillForge.Services;

public sealed class TeamQueryScope : ITeamQueryScope
{
    private readonly ISkillForgeUser _user;

    public TeamQueryScope(ISkillForgeUser user)
    {
        _user = user;
    }

    public bool IsBackOffice => _user.IsBackOffice;
    public ICollection<int> TeamIds => _user.Teams;

    public bool CanAccessTeam(int teamId)
    {
        if (_user.IsBackOffice) return true;
        return _user.Teams.Contains(teamId);
    }
}
