using DotNetNuxt.StarterKit.Entities.Models;
using DotNetNuxt.StarterKit.Entities.Abstractions;
using System.Diagnostics;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Collections.Generic;

namespace DotNetNuxt.StarterKit.Application;

/// <summary>
/// Application logic to manage weathewr forecasts
/// </summary>
/// <param name="dataProvider">Where to retrieve/store data</param>
/// <param name="timeProvider">Where to retrieve the current date/time</param>
public class WeatherForecastFeature(IDataProvider dataProvider, TimeProvider timeProvider)
{
    private static readonly ActivitySource _activitySource = new(nameof(WeatherForecastFeature));
    private static readonly string[] Summaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

    /// <summary>
    /// List recent forecasts
    /// </summary>
    /// <param name="offset">Number of days from today to start listing forecasts</param>
    /// <param name="count">Number of forecasts to include</param>
    /// <returns>Recent forecasts</returns>
    public async Task<WeatherForecast[]> ListForecasts(int offset = 0, int count = 5)
    {
        using var activity = _activitySource.StartActivity(nameof(ListForecasts), ActivityKind.Server);

        try
        {
            var now = timeProvider.GetUtcNow();
            var start = new DateTimeOffset(now.Date, now.Offset).AddDays(offset);
            var requestedDates = Enumerable.Range(0, count).Select(day => start.AddDays(day)).ToArray();
            var requestedDateSet = requestedDates.ToHashSet();
            var query = dataProvider.Get<WeatherForecast>().Where(x => requestedDateSet.Contains(x.Date)).OrderBy(x => x.Date);

            var forecasts = await dataProvider.ToListNoTrackingAsync(query);
            var forecastByDate = forecasts.GroupBy(x => x.Date).ToDictionary(x => x.Key, x => x.First());
            var missing = requestedDates.Where(date => !forecastByDate.ContainsKey(date)).Select(CreateFakeForecast).ToArray();

            if (missing.Length > 0)
            {
                dataProvider.AddRange(missing);
                await dataProvider.SaveChangesAsync();

                foreach (var forecast in missing)
                {
                    forecastByDate[forecast.Date] = forecast;
                }
            }

            return [.. requestedDates.Select(date => forecastByDate[date])];
        }
        catch (Exception ex)
        {
            activity?.AddException(ex);
            throw;
        }
    }

    private static WeatherForecast CreateFakeForecast(DateTimeOffset date)
        => new()
        {
            Date = date,
            Summary = Summaries[date.DayOfYear % Summaries.Length],
            TemperatureF = 20 + (date.DayOfYear * 17 % 85)
        };

    /// <summary>
    /// Update stored forecasts to match supplied forecasts
    /// </summary>
    /// <remarks>
    /// Overwrites any forecasts with matching date.
    /// </remarks>
    /// <param name="incoming">New forecast values</param>
    /// <returns>Number of forecasts updated, added</returns>
    public async Task<(int,int)> UpdateForecasts(IEnumerable<WeatherForecast> incoming)
    {
        using var activity = _activitySource.StartActivity(nameof(UpdateForecasts), ActivityKind.Server);

        try
        {
            // Reduce incoming forecasts to one per date
            var reduced = incoming.GroupBy(x => x.Date).Select(x => x.First());

            // What dates are covered in the incoming forecasts?
            var dates = reduced.Select(x => x.Date).Distinct().ToHashSet();

            // Retrieve existing forecasts which match incoming dates
            var query = dataProvider.Get<WeatherForecast>().Where(x => dates.Contains(x.Date));
            var existing = await dataProvider.ToListNoTrackingAsync(query);
            var dict = existing.ToDictionary(x => x.Date, x => x);

            // Separate existing forecasts by whether or not we have it already
            var divided = reduced.GroupBy(x => dict.ContainsKey(x.Date)).ToDictionary(x => x.Key, x => x);

            // Update existing forecasts with new values
            static WeatherForecast makeUpdate(WeatherForecast updated, WeatherForecast old)
                => new() { Id = old.Id, Date = old.Date, Summary = updated.Summary, TemperatureF = updated.TemperatureF };

            int numUpdated = 0;
            if (divided.TryGetValue(true, out var updateMe))
            {
                var updated = updateMe.Select(x => makeUpdate(old: dict[x.Date], updated: x));
                dataProvider.UpdateRange(updated);
                numUpdated = updated.Count();
            }

            // Straight up add forecasts which we don't already have
            int numAdded = 0;
            if (divided.TryGetValue(false, out var addMe))
            {
                dataProvider.AddRange(addMe);
                numAdded = addMe.Count();
            }

            // Commit the changes
            await dataProvider.SaveChangesAsync();

            return (numUpdated, numAdded);
        }
        catch (Exception ex)
        {
            activity?.AddException(ex);
            throw;
        }
    }
}
