namespace Itenium.SkillForge.WebApi.Controllers;

/// <summary>Request body for recording a skill validation for a consultant.</summary>
public sealed record ValidateSkillRequest(int SkillId, int NewNiveau, int? SessionId);
