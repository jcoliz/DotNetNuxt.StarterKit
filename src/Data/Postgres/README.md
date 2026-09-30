# Postgres Data Provider

Implements the `IDataProvider` interface using Postgres.

## Migrations

After changing the `ApplicationDbContext`, add a migration using the helper script
from the repository root:

```Powershell
.\scripts\Add-Migration.ps1 -Name "AddWeatherForecasts"
```

See [Postgres.MigrationsMain](../../../tools/Postgres.MigrationsMain/README.md) for details on
how migrations are built and applied.
