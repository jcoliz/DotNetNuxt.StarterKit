using System.Threading.Tasks;
using DotNetNuxt.StarterKit.Data;
using DotNetNuxt.StarterKit.Entities.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DotNetNuxt.StarterKit.BackEnd.Startup;

public static class __SetupDatabase
{
    /// <summary>
    /// Add correct flavor of database depending on project configuration.
    /// Returns true if database was configured, false if no connection string was found.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="logger"></param>
    /// <returns>True if database services were registered; false if no connection string was found</returns>
    public static bool SetupDatabase(this WebApplicationBuilder builder, ILogger logger)
    {
        var databaseAssemblyName = typeof(ApplicationDbContext).Assembly.GetName().Name!;
        var connectionString =
            builder.Configuration.GetConnectionString("DefaultConnection") ??
            builder.Configuration.GetConnectionString(databaseAssemblyName);

        if (string.IsNullOrEmpty(connectionString))
        {
            logger.LogError("No database connection string found. Database services will not be available. " +
                "Expected ConnectionStrings:DefaultConnection or ConnectionStrings:{AssemblyName}", databaseAssemblyName);
            return false;
        }

        builder.Services.AddDatabase(connectionString, builder.Environment, logger);

        builder.Services.AddScoped<IDataProvider, ApplicationDbContext>();

        // TODO: Register database health check with "db" tag for the /health/db endpoint
        //builder.Services.AddHealthChecks()
        //    .AddCheck<DatabaseHealthCheck>("database", tags: ["db"]);

        return true;
    }

    public static async Task PrepareDatabaseAsync(this WebApplication app)
    {
        app.Services.MigrateAsNeeded(app.Environment);

        // TODO: Add seeding as required
        //var scope = app.Services.CreateScope();

        //var seeder = scope.ServiceProvider.GetRequiredService<SeedIdentity>();
        //await seeder.SeedAsync();

        //var listsFeature = scope.ServiceProvider.GetRequiredService<ListsFeature>();
        //await listsFeature.SeedAsync();
    }
}
