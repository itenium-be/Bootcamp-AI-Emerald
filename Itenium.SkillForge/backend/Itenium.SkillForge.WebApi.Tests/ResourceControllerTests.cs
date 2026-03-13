using Itenium.SkillForge.Entities.Resources;
using Itenium.SkillForge.Entities.Skills;
using Itenium.SkillForge.Services;
using Itenium.SkillForge.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Itenium.SkillForge.WebApi.Tests;

[TestFixture]
public class ResourceControllerTests : DatabaseTestBase
{
    private ISkillForgeUser _user = null!;
    private ResourceController _sut = null!;
    private SkillCategoryEntity _category = null!;
    private SkillEntity _skill = null!;

    [SetUp]
    public async Task Setup()
    {
        _user = Substitute.For<ISkillForgeUser>();
        _sut = new ResourceController(Db, _user);

        _category = new SkillCategoryEntity { Name = "Test Category" };
        Db.SkillCategories.Add(_category);
        _skill = new SkillEntity { Name = "Clean Code", Category = _category, LevelCount = 3 };
        Db.Skills.Add(_skill);
        await Db.SaveChangesAsync();
    }

    private ResourceEntity CreateResource(string addedByUserId = "user-1", int? skillId = null) =>
        new()
        {
            Title = "Test Resource",
            Url = "https://example.com",
            Type = ResourceType.Article,
            SkillId = skillId ?? _skill.Id,
            FromNiveau = 0,
            ToNiveau = 3,
            AddedByUserId = addedByUserId,
        };

    [Test]
    public async Task GetResources_FiltersBySkillId()
    {
        var otherCategory = new SkillCategoryEntity { Name = "Other Cat" };
        Db.SkillCategories.Add(otherCategory);
        var otherSkill = new SkillEntity { Name = "Other Skill", Category = otherCategory, LevelCount = 2 };
        Db.Skills.Add(otherSkill);
        await Db.SaveChangesAsync();

        Db.Resources.Add(CreateResource(skillId: _skill.Id));
        Db.Resources.Add(CreateResource(skillId: otherSkill.Id));
        await Db.SaveChangesAsync();

        var result = await _sut.GetResources(_skill.Id);

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var resources = ok!.Value as List<ResourceEntity>;
        Assert.That(resources, Has.Count.EqualTo(1));
        Assert.That(resources![0].SkillId, Is.EqualTo(_skill.Id));
    }

    [Test]
    public async Task GetResources_ReturnsAllWhenNoFilter()
    {
        Db.Resources.Add(CreateResource());
        Db.Resources.Add(CreateResource());
        await Db.SaveChangesAsync();

        var result = await _sut.GetResources(null);

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var resources = ok!.Value as List<ResourceEntity>;
        Assert.That(resources, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task CreateResource_WhenAuthenticated_CreatesResource()
    {
        _user.UserId.Returns("user-1");

        var request = new CreateResourceRequest("New Book", "https://example.com", ResourceType.Book, _skill.Id, 0, 3);
        var result = await _sut.CreateResource(request);

        var created = result.Result as CreatedAtActionResult;
        Assert.That(created, Is.Not.Null);
        var resource = created!.Value as ResourceEntity;
        Assert.That(resource!.Title, Is.EqualTo("New Book"));
        Assert.That(resource.AddedByUserId, Is.EqualTo("user-1"));
    }

    [Test]
    public async Task DeleteResource_WhenOwner_DeletesResource()
    {
        _user.UserId.Returns("owner-1");
        _user.IsBackOffice.Returns(false);

        var resource = CreateResource(addedByUserId: "owner-1");
        Db.Resources.Add(resource);
        await Db.SaveChangesAsync();

        var result = await _sut.DeleteResource(resource.Id);

        Assert.That(result, Is.TypeOf<NoContentResult>());
        Assert.That(await Db.Resources.FindAsync(resource.Id), Is.Null);
    }

    [Test]
    public async Task DeleteResource_WhenNotOwner_ReturnsForbidden()
    {
        _user.UserId.Returns("other-user");
        _user.IsBackOffice.Returns(false);

        var resource = CreateResource(addedByUserId: "owner-1");
        Db.Resources.Add(resource);
        await Db.SaveChangesAsync();

        var result = await _sut.DeleteResource(resource.Id);

        Assert.That(result, Is.TypeOf<ForbidResult>());
    }

    [Test]
    public async Task MarkComplete_WhenUser_CreatesCompletion()
    {
        _user.UserId.Returns("user-1");

        var resource = CreateResource();
        Db.Resources.Add(resource);
        await Db.SaveChangesAsync();

        var result = await _sut.MarkComplete(resource.Id);

        Assert.That(result, Is.TypeOf<NoContentResult>());
        Assert.That(Db.ResourceCompletions.Any(rc => rc.ResourceId == resource.Id && rc.UserId == "user-1"), Is.True);
    }

    [Test]
    public async Task MarkComplete_WhenAlreadyCompleted_ReturnsBadRequest()
    {
        _user.UserId.Returns("user-1");

        var resource = CreateResource();
        Db.Resources.Add(resource);
        await Db.SaveChangesAsync();

        Db.ResourceCompletions.Add(new ResourceCompletionEntity { ResourceId = resource.Id, UserId = "user-1" });
        await Db.SaveChangesAsync();

        var result = await _sut.MarkComplete(resource.Id);

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task UnmarkComplete_WhenUser_RemovesCompletion()
    {
        _user.UserId.Returns("user-1");

        var resource = CreateResource();
        Db.Resources.Add(resource);
        await Db.SaveChangesAsync();

        Db.ResourceCompletions.Add(new ResourceCompletionEntity { ResourceId = resource.Id, UserId = "user-1" });
        await Db.SaveChangesAsync();

        var result = await _sut.UnmarkComplete(resource.Id);

        Assert.That(result, Is.TypeOf<NoContentResult>());
        Assert.That(Db.ResourceCompletions.Any(rc => rc.ResourceId == resource.Id && rc.UserId == "user-1"), Is.False);
    }

    [Test]
    public async Task RateResource_CreatesRating()
    {
        _user.UserId.Returns("user-1");

        var resource = CreateResource();
        Db.Resources.Add(resource);
        await Db.SaveChangesAsync();

        var result = await _sut.RateResource(resource.Id, new RateResourceRequest(true));

        Assert.That(result, Is.TypeOf<NoContentResult>());
        Assert.That(Db.ResourceRatings.Any(r => r.ResourceId == resource.Id && r.UserId == "user-1" && r.IsPositive), Is.True);
    }

    [Test]
    public async Task RateResource_WhenAlreadyRated_UpdatesRating()
    {
        _user.UserId.Returns("user-1");

        var resource = CreateResource();
        Db.Resources.Add(resource);
        await Db.SaveChangesAsync();

        Db.ResourceRatings.Add(new ResourceRatingEntity { ResourceId = resource.Id, UserId = "user-1", IsPositive = true });
        await Db.SaveChangesAsync();

        var result = await _sut.RateResource(resource.Id, new RateResourceRequest(false));

        Assert.That(result, Is.TypeOf<NoContentResult>());
        var rating = Db.ResourceRatings.First(r => r.ResourceId == resource.Id && r.UserId == "user-1");
        Assert.That(rating.IsPositive, Is.False);
    }
}
