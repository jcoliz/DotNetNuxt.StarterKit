using DotNetNuxt.StarterKit.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DotNetNuxt.StarterKit.Data.MigrationsMain;

/// <summary>
/// Design-time factory used by EF Core tooling to construct an
/// <see cref="ApplicationDbContext"/> when adding migrations
/// </summary>
public class CatalogContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    /// <inheritdoc/>
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(string.Empty);

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
