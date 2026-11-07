using Azure.Core;
using Azure.Identity;
using DotNetNuxt.Abstractions;
using DotNetNuxt.StarterKit.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods to register the Postgres <see cref="ApplicationDbContext"/>
/// and handle environment-aware migration.
/// </summary>
public static partial class DatabaseServiceExtensions
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
            services.AddSingleton(
                _ => new ManagedIdentityCredential(ManagedIdentityId.SystemAssigned));

            services.AddSingleton(serviceProvider =>
            {
                var credential = serviceProvider.GetRequiredService<ManagedIdentityCredential>();
                var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
                dataSourceBuilder.UsePeriodicPasswordProvider(async (_, ct) =>
                {
                    try
                    {
                        LogRefreshingEntraToken(logger);
                        var token = await credential.GetTokenAsync(
                            new TokenRequestContext(
                                ["https://ossrdbms-aad.database.windows.net/.default"]), ct);
                        LogEntraTokenRefreshed(logger, token.ExpiresOn);
                        return token.Token;
                    }
                    catch (Exception ex)
                    {
                        LogFailedToRefreshEntraToken(logger, ex);
                        throw;
                    }
                }, TimeSpan.FromMinutes(20), TimeSpan.FromSeconds(10));

                return dataSourceBuilder.Build();
            });

            services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
                options.UseNpgsql(serviceProvider.GetRequiredService<NpgsqlDataSource>()));
            LogUsingEntraAuth(logger);
        }
        else
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(connectionString));
            LogUsingConnectionStringAuth(logger);
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

    [LoggerMessage(1, LogLevel.Debug, "Refreshing Entra managed identity token for PostgreSQL")]
    private static partial void LogRefreshingEntraToken(ILogger logger);

    [LoggerMessage(2, LogLevel.Debug, "Entra token refreshed, expires at {ExpiresOn}")]
    private static partial void LogEntraTokenRefreshed(ILogger logger, DateTimeOffset expiresOn);

    [LoggerMessage(3, LogLevel.Warning, "Failed to refresh Entra managed identity token for PostgreSQL")]
    private static partial void LogFailedToRefreshEntraToken(ILogger logger, Exception ex);

    [LoggerMessage(4, LogLevel.Information, "Using Postgres with Entra managed identity auth")]
    private static partial void LogUsingEntraAuth(ILogger logger);

    [LoggerMessage(5, LogLevel.Information, "Using Postgres with connection string auth")]
    private static partial void LogUsingConnectionStringAuth(ILogger logger);
}
