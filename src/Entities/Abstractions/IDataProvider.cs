using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace DotNetNuxt.StarterKit.Entities.Abstractions;

/// <summary>
/// Defines a services to provide data into the system
/// </summary>
public interface IDataProvider
{
    /// <summary>
    /// Retrieves a querable set of <typeparamref name="TEntity"/> objects
    /// </summary>
    /// <typeparam name="TEntity">The type of entity being operated on by this set.</typeparam>
    /// <returns>querable set of <typeparamref name="TEntity"/> objects</returns>
    IQueryable<TEntity> Get<TEntity>() where TEntity : class;

    /// <summary>
    /// Add an item
    /// </summary>
    /// <param name="item">Item to add</param>
    void Add(object item);

    /// <summary>
    /// Add a range of items
    /// </summary>
    /// <param name="items">Items to add</param>
    void AddRange(IEnumerable<object> items);

    /// <summary>
    /// Update a range of items
    /// </summary>
    /// <param name="items">Items to update</param>
    void UpdateRange(IEnumerable<object> items);

    /// <summary>
    /// Save changes previously made
    /// </summary>
    /// <remarks>
    /// This is only needed in the case where we made changes to tracked objects and
    /// did NOT call update on them. Should be rare.
    /// </remarks>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute ToList query asynchronously, with no tracking
    /// </summary>
    /// <typeparam name="T">Type of entities being queried</typeparam>
    /// <param name="query">Query to execute</param>
    /// <returns>List of items</returns>
    Task<List<T>> ToListNoTrackingAsync<T>(IQueryable<T> query) where T : class;
    Task<List<T>> ToListAsync<T>(IQueryable<T> query);
}
