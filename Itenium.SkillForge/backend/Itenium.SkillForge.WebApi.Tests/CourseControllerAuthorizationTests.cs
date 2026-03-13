using System.Reflection;
using Itenium.SkillForge.WebApi;
using Itenium.SkillForge.WebApi.Controllers;
using Microsoft.AspNetCore.Authorization;

namespace Itenium.SkillForge.WebApi.Tests;

[TestFixture]
public class CourseControllerAuthorizationTests
{
    [Test]
    public void CreateCourse_RequiresManagerOrBackofficePolicy()
    {
        var method = typeof(CourseController).GetMethod(nameof(CourseController.CreateCourse));
        var attr = method!.GetCustomAttribute<AuthorizeAttribute>();

        Assert.That(attr, Is.Not.Null);
        Assert.That(attr!.Policy, Is.EqualTo(SkillForgePolicies.ManagerOrBackoffice));
    }

    [Test]
    public void UpdateCourse_RequiresManagerOrBackofficePolicy()
    {
        var method = typeof(CourseController).GetMethod(nameof(CourseController.UpdateCourse));
        var attr = method!.GetCustomAttribute<AuthorizeAttribute>();

        Assert.That(attr, Is.Not.Null);
        Assert.That(attr!.Policy, Is.EqualTo(SkillForgePolicies.ManagerOrBackoffice));
    }

    [Test]
    public void DeleteCourse_RequiresManagerOrBackofficePolicy()
    {
        var method = typeof(CourseController).GetMethod(nameof(CourseController.DeleteCourse));
        var attr = method!.GetCustomAttribute<AuthorizeAttribute>();

        Assert.That(attr, Is.Not.Null);
        Assert.That(attr!.Policy, Is.EqualTo(SkillForgePolicies.ManagerOrBackoffice));
    }

    [Test]
    public void GetCourses_DoesNotRequireManagerOrBackofficePolicy()
    {
        var method = typeof(CourseController).GetMethod(nameof(CourseController.GetCourses));
        var attr = method!.GetCustomAttribute<AuthorizeAttribute>();

        Assert.That(attr?.Policy, Is.Not.EqualTo(SkillForgePolicies.ManagerOrBackoffice));
    }

    [Test]
    public void GetCourse_DoesNotRequireManagerOrBackofficePolicy()
    {
        var method = typeof(CourseController).GetMethod(nameof(CourseController.GetCourse));
        var attr = method!.GetCustomAttribute<AuthorizeAttribute>();

        Assert.That(attr?.Policy, Is.Not.EqualTo(SkillForgePolicies.ManagerOrBackoffice));
    }
}
