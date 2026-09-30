using DotNetNuxt.StarterKit.Controllers.Middleware;
using Microsoft.AspNetCore.Builder;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions for configuring controllers and their supporting middleware
/// </summary>
public static class ControllersExtensions
{
    /// <summary>
    /// Add services for controllers and exception handling middleware
    /// </summary>
    /// <param name="services">Target to add into</param>
    /// <returns>The same service collection, for chaining</returns>
    public static IServiceCollection AddControllersFeatures(this IServiceCollection services)
    {
        services.AddProblemDetails();
        services.AddExceptionHandler<ArgumentExceptionHandler>();

        return services;
    }

    /// <summary>
    /// Wire up exception handling middleware into the request pipeline
    /// </summary>
    /// <param name="app">Target to add into</param>
    /// <returns>The same application builder, for chaining</returns>
    public static IApplicationBuilder UseControllersFeatures(this IApplicationBuilder app)
    {
        app.UseExceptionHandler();

        return app;
    }
}
