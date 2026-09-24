using System;
using System.Collections.Generic;
using System.Linq;
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

        var result = await feature.ListForecasts(offset: -1, count: 10);

        Assert.That(result, Has.Length.EqualTo(10));
        Assert.That(result.Select(forecast => forecast.Summary), Is.EqualTo(
            Enumerable.Range(0, 10).Select(index => $"Forecast {index}")));
    }

    [Test]
    public async Task ListForecasts_generates_and_stores_missing_forecasts()
    {
        var now = new DateTimeOffset(2026, 9, 23, 12, 0, 0, TimeSpan.Zero);
        var start = new DateTimeOffset(now.Date, now.Offset);
        var existing = new WeatherForecast { Date = start.AddDays(1), TemperatureF = 50, Summary = "Existing" };
        var outsideRequestedRange = new WeatherForecast { Date = start.AddDays(10), TemperatureF = 60, Summary = "Later" };
        var provider = new FakeDataProvider([existing, outsideRequestedRange]);
        var feature = new WeatherForecastFeature(provider, new FakeTimeProvider(now));

        var result = await feature.ListForecasts(count: 3);

        Assert.That(result, Has.Length.EqualTo(3));
        Assert.That(result.Select(forecast => forecast.Date), Is.EqualTo(new[] { start, start.AddDays(1), start.AddDays(2) }));
        Assert.That(result[1], Is.EqualTo(existing));
        Assert.That(provider.Added.Select(forecast => forecast.Date), Is.EqualTo(new[] { start, start.AddDays(2) }));
        Assert.That(provider.SaveChangesCallCount, Is.EqualTo(1));
    }

    [Test]
    public async Task UpdateForecasts_updates_existing_records_adds_new_records_and_deduplicates_dates()
    {
        var existingDate = DateTimeOffset.UtcNow.AddHours(-1);
        var newDate = DateTimeOffset.UtcNow;
        var existing = new WeatherForecast { Id = 7, Date = existingDate, TemperatureF = 50, Summary = "Old" };
        var provider = new FakeDataProvider([existing]);
        var feature = new WeatherForecastFeature(provider, new FakeTimeProvider(newDate));

        var result = await feature.UpdateForecasts([
            new WeatherForecast { Date = existingDate, TemperatureF = 65, Summary = "Updated" },
            new WeatherForecast { Date = newDate, TemperatureF = 70, Summary = "First new" },
            new WeatherForecast { Date = newDate, TemperatureF = 80, Summary = "Duplicate new" }
        ]);

        Assert.That(result, Is.EqualTo((1, 1)));
        Assert.That(provider.Updated, Has.Count.EqualTo(1));
        Assert.That(provider.Updated[0], Is.EqualTo(existing with { TemperatureF = 65, Summary = "Updated" }));
        Assert.That(provider.Added, Has.Count.EqualTo(1));
        Assert.That(provider.Added[0].Summary, Is.EqualTo("First new"));
        Assert.That(provider.SaveChangesCallCount, Is.EqualTo(1));
    }

    private sealed class FakeDataProvider(IEnumerable<WeatherForecast> forecasts) : IDataProvider
    {
        private readonly List<WeatherForecast> forecasts = forecasts.ToList();

        public List<WeatherForecast> Added { get; } = [];
        public List<WeatherForecast> Updated { get; } = [];
        public int SaveChangesCallCount { get; private set; }

        public IQueryable<TEntity> Get<TEntity>() where TEntity : class
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
    }
}
