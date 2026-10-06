using System.Threading.Tasks;
using DotNetNuxt.StarterKit.Data;
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
    /// <remarks>
    /// Domain-agnostic. Could be broken out into separate reusable library.
    /// 
    /// However, that reusable library takes EF Core as a dependency. So needs to be the database
    /// library, not the hosting library.
    /// </remarks>
    /// <param name="builder">The WebApplicationBuilder used to configure services and the app.</param>
    /// <param name="logger">The logger to use for logging database setup information.</param>
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

        // Register database health check with "db" tag for the /health/db endpoint
        builder.Services.AddHealthChecks()
            .AddDbContextCheck<ApplicationDbContext>("database", tags: ["db"]);

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
