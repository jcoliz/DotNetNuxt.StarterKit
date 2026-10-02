using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using DotNetNuxt.StarterKit.Application;
using DotNetNuxt.StarterKit.Entities.Abstractions;
using DotNetNuxt.StarterKit.Entities.Models;
using Microsoft.Extensions.Time.Testing;

namespace DotNetNuxt.Tests.Unit;

public class WeatherForecastFeatureTests
{
    [Test]
    public async Task ListForecasts_starts_at_offset_date_returns_in_date_order_and_limits_results()
    {
        // Given: a store holding an out-of-range forecast plus forecasts from the offset date onward
        var now = new DateTimeOffset(2026, 9, 23, 12, 0, 0, TimeSpan.Zero);
        var start = new DateTimeOffset(now.Date, now.Offset).AddDays(-1);
        var forecasts = Enumerable.Range(0, 12)
            .Select(index => new WeatherForecast
            {
                Date = start.AddDays(index),
                Summary = $"Forecast {index}"
            })
            .Append(new WeatherForecast { Date = start.AddDays(-1), Summary = "Too old" })
            .Reverse()
            .ToList();
        var feature = new WeatherForecastFeature(new FakeDataProvider(forecasts), new FakeTimeProvider(now));

        // When: listing forecasts starting one day back, capped at ten
        var result = await feature.ListForecasts(offset: -1, count: 10);

        // Then: exactly ten forecasts are returned in date order starting from the offset date
        Assert.That(result, Has.Count.EqualTo(10));
        Assert.That(result.Select(forecast => forecast.Summary), Is.EqualTo(
            Enumerable.Range(0, 10).Select(index => $"Forecast {index}")));
    }

    [Test]
    public async Task ListForecasts_rejects_offset_below_minimum()
    {
        // Given: a feature with an empty store
        var feature = new WeatherForecastFeature(new FakeDataProvider([]), new FakeTimeProvider(DateTimeOffset.UtcNow));

        // When: listing with an offset below the -30 minimum
        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => feature.ListForecasts(offset: -31, count: 1));

        // Then: the request is rejected for the offset argument
        Assert.That(exception.ParamName, Is.EqualTo("offset"));
    }

    [Test]
    public async Task ListForecasts_rejects_count_below_one()
    {
        // Given: a feature with an empty store
        var feature = new WeatherForecastFeature(new FakeDataProvider([]), new FakeTimeProvider(DateTimeOffset.UtcNow));

        // When: listing with a count below the minimum of one
        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => feature.ListForecasts(offset: 0, count: 0));

        // Then: the request is rejected for the count argument
        Assert.That(exception.ParamName, Is.EqualTo("count"));
    }

    [Test]
    public async Task ListForecasts_rejects_count_plus_offset_above_maximum()
    {
        // Given: a feature with an empty store
        var feature = new WeatherForecastFeature(new FakeDataProvider([]), new FakeTimeProvider(DateTimeOffset.UtcNow));

        // When: listing with count plus offset exceeding the 30 maximum
        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => feature.ListForecasts(offset: 25, count: 6));

        // Then: the request is rejected for the count argument
        Assert.That(exception.ParamName, Is.EqualTo("count"));
    }

    [Test]
    public async Task ListForecasts_allows_boundary_values()
    {
        // Given: a feature with an empty store
        var now = new DateTimeOffset(2026, 9, 23, 12, 0, 0, TimeSpan.Zero);
        var feature = new WeatherForecastFeature(new FakeDataProvider([]), new FakeTimeProvider(now));

        // When: listing at the extreme edges of the allowed range (offset -30, count + offset = 30)
        var result = await feature.ListForecasts(offset: -30, count: 60);

        // Then: the request succeeds and returns the full requested count
        Assert.That(result, Has.Count.EqualTo(60));
    }

    [Test]
    public async Task ListForecasts_generates_and_stores_missing_forecasts()
    {
        // Given: a store containing one forecast within the requested range and one outside it
        var now = new DateTimeOffset(2026, 9, 23, 12, 0, 0, TimeSpan.Zero);
        var start = new DateTimeOffset(now.Date, now.Offset);
        var existing = new WeatherForecast { Date = start.AddDays(1), TemperatureF = 50, Summary = "Existing" };
        var outsideRequestedRange = new WeatherForecast { Date = start.AddDays(10), TemperatureF = 60, Summary = "Later" };
        var provider = new FakeDataProvider([existing, outsideRequestedRange]);
        var feature = new WeatherForecastFeature(provider, new FakeTimeProvider(now));

        // When: listing three forecasts from today
        var result = await feature.ListForecasts(count: 3);

        // Then: three forecasts are returned in date order
        Assert.That(result, Has.Count.EqualTo(3));
        Assert.That(result.Select(forecast => forecast.Date), Is.EqualTo(new[] { start, start.AddDays(1), start.AddDays(2) }));

        // And: the existing in-range forecast is reused
        Assert.That(result.ElementAt(1), Is.EqualTo(existing));

        // And: only the two missing dates are generated and stored, with a single save
        Assert.That(provider.Added.Select(forecast => forecast.Date), Is.EqualTo(new[] { start, start.AddDays(2) }));
        Assert.That(provider.SaveChangesCallCount, Is.EqualTo(1));
    }

    [Test]
    public async Task UpdateForecasts_updates_existing_records_adds_new_records_and_deduplicates_dates()
    {
        // Given: a store holding one existing forecast
        var existingDate = DateTimeOffset.UtcNow.AddHours(-1);
        var newDate = DateTimeOffset.UtcNow;
        var existing = new WeatherForecast { Id = 7, Date = existingDate, TemperatureF = 50, Summary = "Old" };
        var provider = new FakeDataProvider([existing]);
        var feature = new WeatherForecastFeature(provider, new FakeTimeProvider(newDate));

        // When: updating with a change to the existing date plus two forecasts sharing a new date
        var result = await feature.UpdateForecasts([
            new WeatherForecast { Date = existingDate, TemperatureF = 65, Summary = "Updated" },
            new WeatherForecast { Date = newDate, TemperatureF = 70, Summary = "First new" },
            new WeatherForecast { Date = newDate, TemperatureF = 80, Summary = "Duplicate new" }
        ]);

        // Then: exactly one record is updated and one added
        Assert.That(result, Is.EqualTo((1, 1)));

        // And: the existing record is updated with the incoming values
        Assert.That(provider.Updated, Has.Count.EqualTo(1));
        Assert.That(provider.Updated[0], Is.EqualTo(existing with { TemperatureF = 65, Summary = "Updated" }));

        // And: the duplicate new date is collapsed to the first occurrence
        Assert.That(provider.Added, Has.Count.EqualTo(1));
        Assert.That(provider.Added[0].Summary, Is.EqualTo("First new"));

        // And: the changes are committed in a single save
        Assert.That(provider.SaveChangesCallCount, Is.EqualTo(1));
    }

    private sealed class FakeDataProvider(IEnumerable<WeatherForecast> forecasts) : IDataProvider
    {
        private readonly List<WeatherForecast> forecasts = forecasts.ToList();

        public List<WeatherForecast> Added { get; } = [];
        public List<WeatherForecast> Updated { get; } = [];
        public int SaveChangesCallCount { get; private set; }

        public IQueryable<TEntity> Get<TEntity>() where TEntity : class, IModel
            => forecasts.OfType<TEntity>().AsQueryable();

        public void Add(object item) => Added.Add((WeatherForecast)item);

        public void AddRange(IEnumerable<object> items) => Added.AddRange(items.Cast<WeatherForecast>());

        public void UpdateRange(IEnumerable<object> items) => Updated.AddRange(items.Cast<WeatherForecast>());

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCallCount++;
            return Task.FromResult(1);
        }

        public Task<List<T>> ToListNoTrackingAsync<T>(IQueryable<T> query) where T : class
            => Task.FromResult(query.ToList());

        public Task<List<T>> ToListAsync<T>(IQueryable<T> query)
            => Task.FromResult(query.ToList());

        IQueryable<TEntity> IDataProvider.GetIncluding<TEntity, TProperty>(Expression<Func<TEntity, TProperty>> navigationPropertyPath)
        {
            throw new NotImplementedException();
        }

        public void Update(object item)
        {
            throw new NotImplementedException();
        }

        public void Remove(object item)
        {
            throw new NotImplementedException();
        }

        public void RemoveRange(IEnumerable<object> items)
        {
            throw new NotImplementedException();
        }

        public Task<int> ClearAsync<T>() where T : IModel
        {
            throw new NotImplementedException();
        }

        public Task<int> CountAsync<T>(IQueryable<T> query) where T : IModel
        {
            throw new NotImplementedException();
        }

        public Task<bool> AnyAsync<T>(IQueryable<T> query) where T : IModel
        {
            throw new NotImplementedException();
        }

        public Task BulkInsertAsync<T>(IList<T> items) where T : IModel
        {
            throw new NotImplementedException();
        }

        public Task<int> BulkDeleteAsync<T>(IQueryable<T> items) where T : IModel
        {
            throw new NotImplementedException();
        }

        public Task<int> BulkUpdateAsync<T>(IQueryable<T> items, T newValues, List<string> columns) where T : IModel
        {
            throw new NotImplementedException();
        }
    }
}
