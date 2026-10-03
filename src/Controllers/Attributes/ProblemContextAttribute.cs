using System;

namespace DotNetNuxt.StarterKit.Controllers.Attributes;

/// <summary>
/// Names the operation an endpoint performs, so any problem response it produces
/// can say what was being attempted. Shown to users, so keep it friendly.
/// </summary>
/// <remarks>
/// Domain-agnostic. Could be broken out into separate reusable library.
/// </remarks>
/// <param name="message">User-facing description, e.g. "Failed to fetch weather forecasts"</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class ProblemContextAttribute(string message) : Attribute
{
    /// <summary>
    /// User-facing description of the failed operation
    /// </summary>
    public string Message { get; } = message;
}
