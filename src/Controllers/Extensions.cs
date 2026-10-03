using DotNetNuxt.StarterKit.Controllers;
using DotNetNuxt.StarterKit.Controllers.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

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
        services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
        {
            // The exception middleware clears the endpoint, so prefer the original from the feature
            var endpoint = context.HttpContext.Features.Get<IExceptionHandlerFeature>()?.Endpoint
                ?? context.HttpContext.GetEndpoint();

            var operation = endpoint?.Metadata.GetMetadata<ProblemContextAttribute>();
            if (operation is not null)
            {
                context.ProblemDetails.Detail ??= operation.Message;
            }
        });
        services.AddExceptionHandler<ArgumentExceptionHandler>();

        return services.AddControllers();
    }

    /// <summary>
    /// Wire up test context and exception handling middleware into the request pipeline
    /// </summary>
    /// <param name="app">Target to add into</param>
    /// <returns>The same application builder, for chaining</returns>
    public static IApplicationBuilder UseWebApiServices(this IApplicationBuilder app)
    {
        app.UseMiddleware<TestCorrelationMiddleware>();
        app.UseExceptionHandler();

        return app;
    }
}
