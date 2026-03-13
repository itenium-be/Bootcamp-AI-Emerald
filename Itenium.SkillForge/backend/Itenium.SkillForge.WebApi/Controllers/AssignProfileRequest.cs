namespace Itenium.SkillForge.WebApi.Controllers;

/// <summary>Request body for assigning (or clearing) a profile on a consultant.</summary>
public sealed record AssignProfileRequest(int? ProfileId);
