using Microsoft.Extensions.DependencyInjection;

namespace DotNetNuxt.StarterKit.BackEnd.Startup;

public static class __SetupCors
{
    public static IServiceCollection AddCorsPolicy(this IServiceCollection services, string[] allowedOrigins)
    {
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins(allowedOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials()
                        // Expose content-disposition header for file downloads
                        // so the client can read the filename from the response
                        .WithExposedHeaders("content-disposition");
            });
        });

        return services;
    }
}
