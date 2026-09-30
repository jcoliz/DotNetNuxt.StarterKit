namespace DotNetNuxt.StarterKit.Entities.Abstractions;

/// <summary>
/// Identifies an object as a model stored in the database
/// </summary>
public interface IModel
{
    /// <summary>
    /// Database identity for this record
    /// </summary>
    /// <remarks>
    /// By definition, all models stored in the database use an increasing
    /// int as their primary key, and clustered index. Use a GUID public-facing
    /// key with separate non-clustered index if we don't want to expose this
    /// ID to the public.
    /// </remarks>
    int Id { get; init; }
}