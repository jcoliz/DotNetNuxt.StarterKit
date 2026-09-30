using NSwag.Generation.Processors.Security;
using NSwag;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;

namespace DotNetNuxt.StarterKit.BackEnd.Startup;

public static class __SetupSwagger
{
    public static IServiceCollection AddSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddOpenApiDocument(options =>
        {
            options.Title = "Application Backend";
            options.Description = "Application boundary between .NET backend and Nuxt frontend.";
            options.AddSecurity("JWT", new OpenApiSecurityScheme
            {
                In = OpenApiSecurityApiKeyLocation.Header,
                Description = "Enter your token",
                Name = "Authorization",
                Type = OpenApiSecuritySchemeType.Http,
                BearerFormat = "JWT",
                Scheme = "Bearer"
            });
            options.OperationProcessors.Add(
                new AspNetCoreOperationSecurityScopeProcessor("JWT")
            );
        });
        return services;
    }

    public static WebApplication UseSwagger(this WebApplication app)
    {
        app.UseOpenApi();
        app.UseSwaggerUi();

        return app;
    }
}
