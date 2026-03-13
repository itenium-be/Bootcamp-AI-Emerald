using Itenium.SkillForge.Entities.Resources;
using Itenium.SkillForge.Services.Resources;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.Data;

/// <summary>
/// EF Core implementation of <see cref="IResourceService"/>.
/// </summary>
internal sealed class ResourceService : IResourceService
{
    private readonly AppDbContext _db;

    public ResourceService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<ResourceDto>> GetResourcesAsync(
        int? skillId = null,
        ResourceType? type = null,
        int? fromNiveau = null,
        int? toNiveau = null,
        CancellationToken ct = default)
    {
        var query = _db.Resources.AsQueryable();

        if (skillId.HasValue)
            query = query.Where(r => r.SkillId == skillId.Value);

        if (type.HasValue)
            query = query.Where(r => r.Type == type.Value);

        if (fromNiveau.HasValue)
            query = query.Where(r => r.ToNiveau >= fromNiveau.Value);

        if (toNiveau.HasValue)
            query = query.Where(r => r.FromNiveau <= toNiveau.Value);

        return await query
            .OrderByDescending(r => r.AddedAt)
            .Select(r => new ResourceDto(
                r.Id,
                r.Title,
                r.Url,
                r.Type,
                r.SkillId,
                r.Skill.Name,
                r.FromNiveau,
                r.ToNiveau,
                r.AddedByUserId,
                r.AddedAt,
                r.Completions.Count,
                r.Ratings.Count(rt => rt.IsPositive),
                r.Ratings.Count(rt => !rt.IsPositive)))
            .ToListAsync(ct);
    }

    public async Task<ResourceDto> CreateResourceAsync(CreateResourceRequest request, string userId, CancellationToken ct = default)
    {
        var resource = new ResourceEntity
        {
            Title = request.Title,
            Url = request.Url,
            Type = request.Type,
            SkillId = request.SkillId,
            FromNiveau = request.FromNiveau,
            ToNiveau = request.ToNiveau,
            AddedByUserId = userId,
        };

        _db.Resources.Add(resource);
        await _db.SaveChangesAsync(ct);

        var dto = await _db.Resources
            .Where(r => r.Id == resource.Id)
            .Select(r => new ResourceDto(
                r.Id,
                r.Title,
                r.Url,
                r.Type,
                r.SkillId,
                r.Skill.Name,
                r.FromNiveau,
                r.ToNiveau,
                r.AddedByUserId,
                r.AddedAt,
                0,
                0,
                0))
            .FirstAsync(ct);

        return dto;
    }

    public async Task CompleteResourceAsync(int resourceId, string userId, CancellationToken ct = default)
    {
        var existing = await _db.ResourceCompletions
            .FirstOrDefaultAsync(c => c.ResourceId == resourceId && c.UserId == userId, ct);

        if (existing is not null)
        {
            existing.CompletedAt = DateTime.UtcNow;
        }
        else
        {
            _db.ResourceCompletions.Add(new ResourceCompletionEntity
            {
                ResourceId = resourceId,
                UserId = userId,
            });
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task RateResourceAsync(int resourceId, string userId, bool isPositive, CancellationToken ct = default)
    {
        var existing = await _db.ResourceRatings
            .FirstOrDefaultAsync(r => r.ResourceId == resourceId && r.UserId == userId, ct);

        if (existing is not null)
        {
            existing.IsPositive = isPositive;
        }
        else
        {
            _db.ResourceRatings.Add(new ResourceRatingEntity
            {
                ResourceId = resourceId,
                UserId = userId,
                IsPositive = isPositive,
            });
        }

        await _db.SaveChangesAsync(ct);
    }
}
