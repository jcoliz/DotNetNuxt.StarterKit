using DotNetNuxt.StarterKit.Entities.Abstractions;
using DotNetNuxt.StarterKit.Entities.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace DotNetNuxt.StarterKit.Data;

/// <summary>
/// Entity Framework Core <see cref="DbContext"/> backing the Postgres implementation
/// of <see cref="IDataProvider"/>
/// </summary>
/// <param name="options">Provider and connection options for this context</param>
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options), IDataProvider
{
    #region Data

    /// <summary>
    /// Stored weather forecasts
    /// </summary>
    public DbSet<WeatherForecast> WeatherForecasts
    {
        get; set;
    }

    #endregion

    #region Model Building

    /// <summary>
    /// Configure the model and its indexes as the context is being created
    /// </summary>
    /// <param name="builder">Builder used to construct the model</param>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<WeatherForecast>()
            .HasIndex(x => x.Date);

        base.OnModelCreating(builder);
    }

    #endregion

    #region Query Builders

    /// <inheritdoc/>
    IQueryable<T> IDataProvider.Get<T>()
        => base.Set<T>();

    /// <inheritdoc/>
    IQueryable<TEntity> IDataProvider.GetIncluding<TEntity, TProperty>(Expression<Func<TEntity, TProperty>> navigationPropertyPath)
        => base.Set<TEntity>().Include(navigationPropertyPath);

    #endregion

    #region Modifiers

    /// <inheritdoc/>
    void IDataProvider.Add(object item)
        => base.Add(item);

    /// <inheritdoc/>
    void IDataProvider.AddRange(IEnumerable<object> items)
        => base.AddRange(items);

    /// <inheritdoc/>
    void IDataProvider.Update(object item)
        => base.Update(item);

    /// <inheritdoc/>
    void IDataProvider.UpdateRange(IEnumerable<object> items)
        => base.UpdateRange(items);

    /// <inheritdoc/>
    void IDataProvider.Remove(object item)
        => base.Remove(item);

    /// <inheritdoc/>
    void IDataProvider.RemoveRange(IEnumerable<object> items)
        => base.RemoveRange(items);

    /// <inheritdoc/>
    Task<int> IDataProvider.ClearAsync<T>()
        => throw new NotImplementedException();

    #endregion

    #region Query Runners

    /// <inheritdoc/>
    Task<List<T>> IDataProvider.ToListNoTrackingAsync<T>(IQueryable<T> query)
        => query.AsNoTracking().ToListAsync();

    /// <inheritdoc/>
    Task<List<T>> IDataProvider.ToListAsync<T>(IQueryable<T> query)
        => query.ToListAsync();

    /// <inheritdoc/>
    Task<int> IDataProvider.CountAsync<T>(IQueryable<T> query)
        => query.CountAsync();

    /// <inheritdoc/>
    Task<bool> IDataProvider.AnyAsync<T>(IQueryable<T> query)
        => query.AnyAsync();

    #endregion

    #region Bulk Operations

    /// <inheritdoc/>
    Task IDataProvider.BulkInsertAsync<T>(IList<T> items)
        => throw new NotImplementedException();

    /// <inheritdoc/>
    Task<int> IDataProvider.BulkDeleteAsync<T>(IQueryable<T> items)
        => throw new NotImplementedException();

    /// <inheritdoc/>
    Task<int> IDataProvider.BulkUpdateAsync<T>(IQueryable<T> items, T newValues, List<string> columns)
        => throw new NotImplementedException();

    #endregion
}
