using Itenium.SkillForge.Entities.Resources;

namespace Itenium.SkillForge.Services.Resources;

/// <summary>
/// Manages the shared resource library (articles, videos, books, etc.).
/// </summary>
public interface IResourceService
{
    /// <summary>
    /// Returns resources, optionally filtered by skill, type, and niveau range.
    /// </summary>
    Task<IReadOnlyList<ResourceDto>> GetResourcesAsync(
        int? skillId = null,
        ResourceType? type = null,
        int? fromNiveau = null,
        int? toNiveau = null,
        CancellationToken ct = default);

    /// <summary>Creates a new resource contributed by the given user.</summary>
    Task<ResourceDto> CreateResourceAsync(CreateResourceRequest request, string userId, CancellationToken ct = default);

    /// <summary>
    /// Marks a resource as completed for the given user (idempotent: updates CompletedAt on repeat).
    /// </summary>
    Task CompleteResourceAsync(int resourceId, string userId, CancellationToken ct = default);

    /// <summary>Adds or updates a thumbs-up/down rating for the given user.</summary>
    Task RateResourceAsync(int resourceId, string userId, bool isPositive, CancellationToken ct = default);
}
