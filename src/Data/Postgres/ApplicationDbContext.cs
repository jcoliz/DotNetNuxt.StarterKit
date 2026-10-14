using System.Linq.Expressions;
using DotNetNuxt.Abstractions;
using DotNetNuxt.StarterKit.Entities.Models;
using DotNetNuxt.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DotNetNuxt.StarterKit.Data;

/// <summary>
/// Entity Framework Core <see cref="DbContext"/> backing the Postgres implementation
/// of <see cref="IDataProvider"/>
/// </summary>
/// <param name="options">Provider and connection options for this context</param>
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : StandardDbContext(options)
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

    #region Bulk Operations

    /// <inheritdoc/>
    public override Task<int> BulkUpdateAsync<T>(IQueryable<T> items, T newValues, List<string> columns)
        => throw new NotImplementedException();

    #endregion
}
