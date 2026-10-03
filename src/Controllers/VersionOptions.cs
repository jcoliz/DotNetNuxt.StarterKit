namespace DotNetNuxt.StarterKit.Controllers;

/// <summary>
/// Application version reported by the version endpoint
/// </summary>
/// <remarks>
/// Domain-agnostic. Could be broken out into separate reusable library.
/// </remarks>
public record VersionOptions
{
    /// <summary>
    /// Application version, or null if unknown
    /// </summary>
    public string? Version { get; set; }
}
