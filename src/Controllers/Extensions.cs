using DotNetNuxt.StarterKit.Controllers;
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
    /// <param name="version">Application version reported by the version endpoint</param>
    /// <returns>The MVC builder, so callers can chain further MVC configuration</returns>
    public static IMvcBuilder AddWebApiServices(this IServiceCollection services, string? version)
    {
        services.Configure<VersionOptions>(options => options.Version = version);
        services.AddProblemDetails();
        services.AddExceptionHandler<ArgumentExceptionHandler>();

        return services.AddControllers();
    }

    /// <summary>
    /// Wire up exception handling middleware into the request pipeline
    /// </summary>
    /// <param name="app">Target to add into</param>
    /// <returns>The same application builder, for chaining</returns>
    public static IApplicationBuilder UseWebApiServices(this IApplicationBuilder app)
    {
        app.UseExceptionHandler();

        return app;
    }
}
