using System;

namespace DotNetNuxt.StarterKit.Entities.Models;

public record WeatherForecast
{
    /// <summary>
    /// Database identifier
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// UTC date & time at which forecast is applicable
    /// </summary>
    public DateTimeOffset Date { get; init; }

    /// <summary>
    /// Forecasted temperature in F
    /// </summary>
    public int TemperatureF {  get; init; }

    /// <summary>
    /// Textual summary of the forecast
    /// </summary>
    public string? Summary { get; init; }

    /// <summary>
    /// Forecasted temperature in C
    /// </summary>
    public int TemperatureC => (int)((TemperatureF - 32) * 0.5556);
}
