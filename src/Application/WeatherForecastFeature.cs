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
public class WeatherForecastFeature(IDataProvider dataProvider)
{
    private static readonly ActivitySource _activitySource = new(nameof(WeatherForecastFeature));

    /// <summary>
    /// List recent forecasts
    /// </summary>
    /// <returns>Recent forecasts</returns>
    public async Task<WeatherForecast[]> ListForecasts()
    {
        using var activity = _activitySource.StartActivity(nameof(ListForecasts), ActivityKind.Server);

        try
        {
            var yesterday = DateTime.UtcNow - TimeSpan.FromDays(1);
            var query = dataProvider.Get<WeatherForecast>().Where(x => x.Date >= yesterday).OrderBy(x => x.Date).Take(10);

            var forecasts = await dataProvider.ToListNoTrackingAsync(query);

            return [.. forecasts];
        }
        catch (Exception ex)
        {
            activity?.AddException(ex);
            throw;
        }
    }

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
