using Itenium.SkillForge.Entities;

namespace Itenium.SkillForge.Services;

public static class QueryScopeExtensions
{
    /// <summary>
    /// Applies team-based query scoping. BackOffice users see all teams.
    /// Regular users only see entities belonging to their teams.
    /// </summary>
    public static IQueryable<T> ApplyTeamScope<T>(this IQueryable<T> query, ITeamQueryScope scope)
        where T : ITeamScoped
    {
        if (scope.IsBackOffice)
            return query;

        return query.Where(e => scope.TeamIds.Contains(e.TeamId));
    }
}
