# Migrations Main for Data.Postgres

In order to run migrations, EF Core needs a "main" app to run. To keep this simple,
we dedicate a whole app, with very minimal things going on, so we don't have to worry
about configuration, secrets, etc, which have nothing to do with the migration.

See: https://erwinstaal.nl/posts/db-per-tenant-catalog-database-ef-core-migrations/

## Make a migration

After making changes to the `ApplicationDbContext`, we need to add a migration
to describe how those changes will show up in the database. Use the helper script
from the repository root:

```Powershell
.\scripts\Add-Migration.ps1 -Name "AddWeatherForecasts"
```

If you make a mistake and need to re-do it, be sure to remove the `ApplicationDbContextModelSnapshot.cs` file.

## Update database automatically

For Postgres database, no further action is needed. Application Main() is expected
to call `Database.Migrate()` at launch, to automatically apply the latest
migrations.

This is allowed for non-production environments. Production database migrations
are applied by deployment pipeline scripts.

## Generate a migrations SQL script to update database

Again, we don't normally need to migrate the database manually.
That said, if we want to see what the migrations script looks like, we can
create one using the helper script from the repository root:

```Powershell
.\scripts\Export-PostgresMigration.ps1
```

This is the same command the CD pipeline runs to publish the migration as an
artifact (see `.azure/pipelines/steps/publish/publish-postgres-migration.yaml`).

## Never EnsureCreated

Note that databases created with "EnsureCreated" can never be migrated. That's definitely not OK
for production, so "EnsureCreated" is explicitly forbidden for code headed to production.

Calling "Migrate" is allowed for non-production Postgres environments.
