using System.Reflection;
using Itenium.SkillForge.WebApi;
using Itenium.SkillForge.WebApi.Controllers;
using Microsoft.AspNetCore.Authorization;

namespace Itenium.SkillForge.WebApi.Tests;

[TestFixture]
public class ProfilesControllerAuthorizationTests
{
    [Test]
    public void GetProfiles_DoesNotRequireManagerOrBackofficePolicy()
    {
        var method = typeof(ProfilesController).GetMethod(nameof(ProfilesController.GetProfiles));
        var attr = method!.GetCustomAttribute<AuthorizeAttribute>();

        Assert.That(attr?.Policy, Is.Not.EqualTo(SkillForgePolicies.ManagerOrBackoffice));
    }

    [Test]
    public void GetProfileSkills_DoesNotRequireManagerOrBackofficePolicy()
    {
        var method = typeof(ProfilesController).GetMethod(nameof(ProfilesController.GetProfileSkills));
        var attr = method!.GetCustomAttribute<AuthorizeAttribute>();

        Assert.That(attr?.Policy, Is.Not.EqualTo(SkillForgePolicies.ManagerOrBackoffice));
    }

    [Test]
    public void GetSeniorityThresholds_DoesNotRequireManagerOrBackofficePolicy()
    {
        var method = typeof(ProfilesController).GetMethod(nameof(ProfilesController.GetSeniorityThresholds));
        var attr = method!.GetCustomAttribute<AuthorizeAttribute>();

        Assert.That(attr?.Policy, Is.Not.EqualTo(SkillForgePolicies.ManagerOrBackoffice));
    }

    [Test]
    public void AssignProfile_RequiresManagerOrBackofficePolicy()
    {
        var method = typeof(ConsultantsController).GetMethod(nameof(ConsultantsController.AssignProfile));
        var attr = method!.GetCustomAttribute<AuthorizeAttribute>();

        Assert.That(attr, Is.Not.Null);
        Assert.That(attr!.Policy, Is.EqualTo(SkillForgePolicies.ManagerOrBackoffice));
    }
}
