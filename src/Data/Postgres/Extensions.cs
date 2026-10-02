using Azure.Core;
using Azure.Identity;
using DotNetNuxt.StarterKit.Data;
using DotNetNuxt.StarterKit.Entities.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods to register the Postgres <see cref="ApplicationDbContext"/>
/// and handle environment-aware migration.
/// </summary>
public static class DatabaseServiceExtensions
{
    /// <summary>
    /// Register <see cref="ApplicationDbContext"/> with the Npgsql provider.
    /// In production, uses Entra managed-identity token auth;
    /// otherwise uses the connection string as-is (password from Aspire or config).
    /// </summary>
    public static IServiceCollection AddDatabase(
        this IServiceCollection services,
        string connectionString,
        IHostEnvironment environment,
        ILogger logger)
    {
        if (environment.IsProduction())
        {
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
            dataSourceBuilder.UsePeriodicPasswordProvider(async (_, ct) =>
            {
                try
                {
                    logger.LogDebug("Refreshing Entra managed identity token for PostgreSQL");
                    var credential = new ManagedIdentityCredential(ManagedIdentityId.SystemAssigned);
                    var token = await credential.GetTokenAsync(
                        new TokenRequestContext(
                            ["https://ossrdbms-aad.database.windows.net/.default"]), ct);
                    logger.LogDebug("Entra token refreshed, expires at {ExpiresOn}", token.ExpiresOn);
                    return token.Token;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to refresh Entra managed identity token for PostgreSQL");
                    throw;
                }
            }, TimeSpan.FromMinutes(20), TimeSpan.FromSeconds(10));

            var dataSource = dataSourceBuilder.Build();
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(dataSource));
            logger.LogInformation("Using Postgres with Entra managed identity auth");
        }
        else
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(connectionString));
            logger.LogInformation("Using Postgres with connection string auth");
        }

        // Expose the context through the storage-agnostic abstraction,
        // forwarding to the single per-scope ApplicationDbContext instance.
        services.AddScoped<IDataProvider>(sp => sp.GetRequiredService<ApplicationDbContext>());

        return services;
    }

    /// <summary>
    /// Auto-migrate the database when appropriate for the current environment.
    /// Production databases are migrated exclusively by pipeline scripts.
    /// </summary>
    public static void MigrateAsNeeded(
        this IServiceProvider services,
        IHostEnvironment environment)
    {
        if (!environment.IsProduction())
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.Migrate();
        }
    }
}
