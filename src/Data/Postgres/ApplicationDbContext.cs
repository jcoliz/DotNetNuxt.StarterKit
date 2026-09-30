using DotNetNuxt.StarterKit.Entities.Abstractions;
using DotNetNuxt.StarterKit.Entities.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace DotNetNuxt.StarterKit.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options), IDataProvider
{
    #region Data

    public DbSet<WeatherForecast> WeatherForecasts
    {
        get; set;
    }

    #endregion

    #region Model Building

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<WeatherForecast>()
            .HasIndex(x => x.Date);

        base.OnModelCreating(builder);
    }

    #endregion

    #region Query Builders

    IQueryable<T> IDataProvider.Get<T>()
        => base.Set<T>();

    /// <summary>
    /// Get a query of <typeparamref name="TEntity"/> entities,
    /// including <typeparamref name="TProperty"/> navigation properties
    /// </summary>
    IQueryable<TEntity> IDataProvider.GetIncluding<TEntity, TProperty>(Expression<Func<TEntity, TProperty>> navigationPropertyPath)
        => base.Set<TEntity>().Include(navigationPropertyPath);

    #endregion

    #region Modifiers

    void IDataProvider.Add(object item)
        => base.Add(item);

    void IDataProvider.AddRange(IEnumerable<object> items)
        => base.AddRange(items);

    void IDataProvider.Update(object item)
        => base.Update(item);

    void IDataProvider.UpdateRange(IEnumerable<object> items)
        => base.UpdateRange(items);

    void IDataProvider.Remove(object item)
        => base.Remove(item);

    void IDataProvider.RemoveRange(IEnumerable<object> items)
        => base.RemoveRange(items);

    Task<int> IDataProvider.ClearAsync<T>()
        => throw new NotImplementedException();

    #endregion

    #region Query Runners

    Task<List<T>> IDataProvider.ToListNoTrackingAsync<T>(IQueryable<T> query)
        => query.AsNoTracking().ToListAsync();

    Task<List<T>> IDataProvider.ToListAsync<T>(IQueryable<T> query)
        => query.ToListAsync();

    Task<int> IDataProvider.CountAsync<T>(IQueryable<T> query)
        => query.CountAsync();

    Task<bool> IDataProvider.AnyAsync<T>(IQueryable<T> query)
        => query.AnyAsync();

    #endregion

    #region Bulk Operations

    Task IDataProvider.BulkInsertAsync<T>(IList<T> items)
        => throw new NotImplementedException();

    Task<int> IDataProvider.BulkDeleteAsync<T>(IQueryable<T> items)
        => throw new NotImplementedException();

    Task<int> IDataProvider.BulkUpdateAsync<T>(IQueryable<T> items, T newValues, List<string> columns)
        => throw new NotImplementedException();

    #endregion
}
