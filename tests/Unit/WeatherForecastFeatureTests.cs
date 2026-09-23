using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNetNuxt.StarterKit.Application;
using DotNetNuxt.StarterKit.Entities.Abstractions;
using DotNetNuxt.StarterKit.Entities.Models;

namespace DotNetNuxt.Tests.Unit;

public class WeatherForecastFeatureTests
{
    [Test]
    public async Task ListForecasts_returns_recent_forecasts_in_date_order_and_limits_results()
    {
        var now = DateTimeOffset.UtcNow;
        var forecasts = Enumerable.Range(0, 12)
            .Select(index => new WeatherForecast
            {
                Date = now.AddHours(-index),
                Summary = $"Forecast {index}"
            })
            .Append(new WeatherForecast { Date = now.AddDays(-2), Summary = "Too old" })
            .Reverse()
            .ToList();
        var feature = new WeatherForecastFeature(new FakeDataProvider(forecasts));

        var result = await feature.ListForecasts();

        Assert.That(result, Has.Length.EqualTo(10));
        Assert.That(result.Select(forecast => forecast.Summary), Is.EqualTo(
            Enumerable.Range(2, 10).Reverse().Select(index => $"Forecast {index}")));
    }

    [Test]
    public async Task UpdateForecasts_updates_existing_records_adds_new_records_and_deduplicates_dates()
    {
        var existingDate = DateTimeOffset.UtcNow.AddHours(-1);
        var newDate = DateTimeOffset.UtcNow;
        var existing = new WeatherForecast { Id = 7, Date = existingDate, TemperatureF = 50, Summary = "Old" };
        var provider = new FakeDataProvider([existing]);
        var feature = new WeatherForecastFeature(provider);

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
