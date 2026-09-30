using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Linq.Expressions;
using System;

namespace DotNetNuxt.StarterKit.Entities.Abstractions;

/// <summary>
/// Provider of underlying object data storage for our models
/// </summary>
/// <remarks>
/// Platform- and storage-agnostic interface for all data stored in the system.
/// Note that we actively use multiple different data stores based on context
/// </remarks>
public interface IDataProvider
{
    #region Query Builders

    /// <summary>
    /// Get a query of <typeparamref name="T"/> entities 
    /// </summary>
    /// <remarks>
    /// Returning a query allows us to further refine the query before executing it
    /// </remarks>
    /// <typeparam name="T">Which type of entities to get</typeparam>
    IQueryable<T> Get<T>() where T : class, IModel;

    /// <summary>
    /// Get a query of <typeparamref name="TEntity"/> entities, 
    /// including <typeparamref name="TProperty"/> navigation properties
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TProperty"></typeparam>
    /// <param name="navigationPropertyPath"></param>
    /// <returns></returns>
    IQueryable<TEntity> GetIncluding<TEntity, TProperty>(Expression<Func<TEntity, TProperty>> navigationPropertyPath) where TEntity : class, IModel;
//                => base.Set<TEntity>().Include(navigationPropertyPath);

    #endregion

    #region Modifiers

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
    /// Update an item
    /// </summary>
    /// <param name="item">Item to update</param>
    void Update(object item);
    
    /// <summary>
    /// Update a range of items
    /// </summary>
    /// <param name="items">Items to update</param>
    void UpdateRange(IEnumerable<object> items);
    
    /// <summary>
    /// Remove an item
    /// </summary>
    /// <param name="item">Item to remove</param>
    void Remove(object item);
    
    /// <summary>
    /// Remove items
    /// </summary>
    /// <param name="items">Items to remove</param>
    void RemoveRange(IEnumerable<object> items);

    /// <summary>
    /// Clear all items from a particular table
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Task<int> ClearAsync<T>() where T : IModel;

    /// <summary>
    /// Save changes previously made
    /// </summary>
    /// <remarks>
    /// This is only needed in the case where we made changes to tracked objects and
    /// did NOT call update on them. Should be rare.
    /// </remarks>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Query Runners

    /// <summary>
    /// Execute ToList query asynchronously, with no tracking
    /// </summary>
    /// <typeparam name="T">Type of entities being queried</typeparam>
    /// <param name="query">Query to execute</param>
    /// <returns>List of items</returns>
    Task<List<T>> ToListNoTrackingAsync<T>(IQueryable<T> query) where T : class;
    Task<List<T>> ToListAsync<T>(IQueryable<T> query);

    /// <summary>
    /// Execute Count query asynchronously
    /// </summary>
    /// <typeparam name="T">Type of entities being queried</typeparam>
    /// <param name="query">Query to execute</param>
    /// <returns>Number of items in <paramref name="query"/></returns>
    Task<int> CountAsync<T>(IQueryable<T> query) where T : IModel;

    /// <summary>
    /// Execute "Any" query asynchronously
    /// </summary>
    /// <typeparam name="T">Type of entities being queried</typeparam>
    /// <param name="query">Query to execute</param>
    /// <returns>Whether there are any items in <paramref name="query"/></returns>
    Task<bool> AnyAsync<T>(IQueryable<T> query) where T : IModel;

    #endregion

    #region Bulk Operations

    /// <summary>
    /// Insert many items en masse
    /// </summary>
    /// <remarks>
    /// This is much more efficient than doing it one at a time
    /// </remarks>
    /// <typeparam name="T">Type of items</typeparam>
    /// <param name="items">Items to be inserted</param>
    Task BulkInsertAsync<T>(IList<T> items) where T : IModel;

    /// <summary>
    /// Delete many items en masse
    /// </summary>
    /// <remarks>
    /// This is much more efficient than doing it one at a time
    /// </remarks>
    /// <typeparam name="T">Type of items</typeparam>
    /// <param name="items">Items to be deleted</param>
    /// <returns>Number of deleted items</returns>
    Task<int> BulkDeleteAsync<T>(IQueryable<T> items) where T : IModel;

    /// <summary>
    /// Update many items en masse
    /// </summary>
    /// <remarks>
    /// This is much more efficient than doing it one at a time
    /// </remarks>
    /// <typeparam name="T">Type of items</typeparam>
    /// <param name="items">Items to be updated</param>
    /// <param name="newValues">Type T object with affected fields set to new values</param>
    /// <param name="columns">List of affected fields</param>
    /// <returns>Number of affected items</returns>
    Task<int> BulkUpdateAsync<T>(IQueryable<T> items, T newValues, List<string> columns) where T : IModel;

    #endregion
}
