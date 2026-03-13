using Itenium.SkillForge.Data;
using Itenium.SkillForge.Entities.Resources;
using Itenium.SkillForge.Entities.Skills;
using Itenium.SkillForge.Services.Resources;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.WebApi.Tests;

/// <summary>
/// Integration tests for <see cref="ResourceService"/>.
/// Issue #19 (Resource API).
/// </summary>
[TestFixture]
public class ResourceServiceTests : DatabaseTestBase
{
    private ResourceService _sut = null!;

    private const string UserId = "user-resource-001";

    [SetUp]
    public async Task SetUp()
    {
        await SkillCatalogueSeedData.Seed(Db);
        _sut = new ResourceService(Db);
    }

    // ── GetResourcesAsync ────────────────────────────────────────────────────

    [Test]
    public async Task GetResources_ReturnsAllResources()
    {
        var skill = await GetAnySkillAsync();
        await CreateResourceAsync(skill.Id, ResourceType.Article);
        await CreateResourceAsync(skill.Id, ResourceType.Video);

        var result = await _sut.GetResourcesAsync();

        Assert.That(result, Has.Count.GreaterThanOrEqualTo(2));
    }

    [Test]
    public async Task GetResources_FiltersBySkillId()
    {
        var skills = await Db.Skills.Take(2).ToListAsync();
        await CreateResourceAsync(skills[0].Id, ResourceType.Article);
        await CreateResourceAsync(skills[1].Id, ResourceType.Book);

        var result = await _sut.GetResourcesAsync(skillId: skills[0].Id);

        Assert.That(result, Has.Count.GreaterThanOrEqualTo(1));
        Assert.That(result.All(r => r.SkillId == skills[0].Id), Is.True);
    }

    [Test]
    public async Task GetResources_FiltersByType()
    {
        var skill = await GetAnySkillAsync();
        await CreateResourceAsync(skill.Id, ResourceType.Video);
        await CreateResourceAsync(skill.Id, ResourceType.Book);

        var result = await _sut.GetResourcesAsync(type: ResourceType.Video);

        Assert.That(result.All(r => r.Type == ResourceType.Video), Is.True);
    }

    [Test]
    public async Task GetResources_FiltersByNiveauRange()
    {
        var skill = await GetAnySkillAsync();
        await CreateResourceAsync(skill.Id, fromNiveau: 1, toNiveau: 3);
        await CreateResourceAsync(skill.Id, fromNiveau: 5, toNiveau: 7);

        var result = await _sut.GetResourcesAsync(fromNiveau: 1, toNiveau: 3);

        Assert.That(result.All(r => r.FromNiveau <= 3 && r.ToNiveau >= 1), Is.True);
    }

    [Test]
    public async Task GetResources_IncludesCompletionAndRatingCounts()
    {
        var skill = await GetAnySkillAsync();
        var resource = await CreateResourceAsync(skill.Id);
        Db.ResourceCompletions.Add(new ResourceCompletionEntity { ResourceId = resource.Id, UserId = UserId });
        Db.ResourceRatings.Add(new ResourceRatingEntity { ResourceId = resource.Id, UserId = UserId, IsPositive = true });
        await Db.SaveChangesAsync();

        var result = await _sut.GetResourcesAsync();
        var dto = result.First(r => r.Id == resource.Id);

        Assert.That(dto.CompletionCount, Is.EqualTo(1));
        Assert.That(dto.PositiveRatings, Is.EqualTo(1));
        Assert.That(dto.NegativeRatings, Is.EqualTo(0));
    }

    // ── CreateResourceAsync ──────────────────────────────────────────────────

    [Test]
    public async Task CreateResource_PersistsResource()
    {
        var skill = await GetAnySkillAsync();
        var request = new CreateResourceRequest(
            "Clean Code",
            "https://example.com/clean-code",
            ResourceType.Book,
            skill.Id,
            1,
            5);

        var result = await _sut.CreateResourceAsync(request, UserId);

        Assert.That(result.Title, Is.EqualTo("Clean Code"));
        Assert.That(result.Type, Is.EqualTo(ResourceType.Book));
        Assert.That(result.AddedByUserId, Is.EqualTo(UserId));
        Assert.That(result.SkillId, Is.EqualTo(skill.Id));

        var inDb = await Db.Resources.FindAsync(result.Id);
        Assert.That(inDb, Is.Not.Null);
    }

    // ── CompleteResourceAsync ────────────────────────────────────────────────

    [Test]
    public async Task CompleteResource_CreatesCompletion()
    {
        var skill = await GetAnySkillAsync();
        var resource = await CreateResourceAsync(skill.Id);

        await _sut.CompleteResourceAsync(resource.Id, UserId);

        var completion = await Db.ResourceCompletions
            .FirstOrDefaultAsync(c => c.ResourceId == resource.Id && c.UserId == UserId);
        Assert.That(completion, Is.Not.Null);
    }

    [Test]
    public async Task CompleteResource_IsIdempotent_UpdatesCompletedAt()
    {
        var skill = await GetAnySkillAsync();
        var resource = await CreateResourceAsync(skill.Id);
        await _sut.CompleteResourceAsync(resource.Id, UserId);
        var firstCompletion = await Db.ResourceCompletions
            .FirstAsync(c => c.ResourceId == resource.Id && c.UserId == UserId);
        var firstTime = firstCompletion.CompletedAt;

        await _sut.CompleteResourceAsync(resource.Id, UserId);

        var completionCount = await Db.ResourceCompletions
            .CountAsync(c => c.ResourceId == resource.Id && c.UserId == UserId);
        Assert.That(completionCount, Is.EqualTo(1));
    }

    // ── RateResourceAsync ────────────────────────────────────────────────────

    [Test]
    public async Task RateResource_CreatesRating()
    {
        var skill = await GetAnySkillAsync();
        var resource = await CreateResourceAsync(skill.Id);

        await _sut.RateResourceAsync(resource.Id, UserId, isPositive: true);

        var rating = await Db.ResourceRatings
            .FirstOrDefaultAsync(r => r.ResourceId == resource.Id && r.UserId == UserId);
        Assert.That(rating, Is.Not.Null);
        Assert.That(rating!.IsPositive, Is.True);
    }

    [Test]
    public async Task RateResource_UpdatesExistingRating()
    {
        var skill = await GetAnySkillAsync();
        var resource = await CreateResourceAsync(skill.Id);
        await _sut.RateResourceAsync(resource.Id, UserId, isPositive: true);

        await _sut.RateResourceAsync(resource.Id, UserId, isPositive: false);

        var ratingCount = await Db.ResourceRatings
            .CountAsync(r => r.ResourceId == resource.Id && r.UserId == UserId);
        var rating = await Db.ResourceRatings
            .FirstAsync(r => r.ResourceId == resource.Id && r.UserId == UserId);
        Assert.That(ratingCount, Is.EqualTo(1));
        Assert.That(rating.IsPositive, Is.False);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<SkillEntity> GetAnySkillAsync()
        => await Db.Skills.FirstAsync();

    private async Task<ResourceEntity> CreateResourceAsync(
        int skillId,
        ResourceType type = ResourceType.Article,
        int fromNiveau = 1,
        int toNiveau = 3)
    {
        var resource = new ResourceEntity
        {
            Title = $"Resource {Guid.NewGuid():N}",
            Url = "https://example.com",
            Type = type,
            SkillId = skillId,
            FromNiveau = fromNiveau,
            ToNiveau = toNiveau,
            AddedByUserId = UserId,
        };
        Db.Resources.Add(resource);
        await Db.SaveChangesAsync();
        return resource;
    }
}
