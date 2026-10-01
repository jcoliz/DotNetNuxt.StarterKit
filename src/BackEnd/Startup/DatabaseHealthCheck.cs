using Microsoft.Extensions.Diagnostics.HealthChecks;
using DotNetNuxt.StarterKit.Data;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace DotNetNuxt.StarterKit.BackEnd.Startup;

/// <summary>
/// Health check that verifies database connectivity.
/// Returns Healthy when the database is reachable, Unhealthy otherwise.
/// </summary>
/// <remarks>
/// Registered with the "db" tag so it can be exposed on a dedicated
/// /health/db endpoint without affecting the main /health probe.
/// </remarks>
public class DatabaseHealthCheck(ApplicationDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            if (await db.Database.CanConnectAsync(cancellationToken))
            {
                return HealthCheckResult.Healthy("Database is reachable");
            }

            return HealthCheckResult.Unhealthy("Database connection returned false");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database is unreachable", ex);
        }
    }
}
