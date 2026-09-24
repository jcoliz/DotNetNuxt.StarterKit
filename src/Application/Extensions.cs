using System;
using DotNetNuxt.StarterKit.Application;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions for configuring all application features
/// </summary>
public static class ApplicationFeaturesExtensions
{
    /// <summary>
    /// Add services for all application features
    /// </summary>
    /// <param name="services">Target to add into</param>
    /// <returns></returns>
    public static IServiceCollection AddApplicationFeatures(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<WeatherForecastFeature>();

        return services;
    }
}
