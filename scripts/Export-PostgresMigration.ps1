<#
.SYNOPSIS
Generates an idempotent SQL script for the PostgreSQL Entity Framework migrations.

.DESCRIPTION
This script builds the Postgres migration project and then uses dotnet ef to
generate an idempotent SQL script describing all migrations. The resulting script
can be applied to any database regardless of which migrations it already has.

Ultimately this same command is intended to run in the CD pipeline. See
.azure/pipelines/steps/publish/publish-postgres-migration.yaml for the pipeline step.

.PARAMETER OutputPath
Path of the SQL script to generate. Defaults to .\out\postgres-migration.sql.

.EXAMPLE
.\Export-PostgresMigration.ps1
Generates the migration script at .\out\postgres-migration.sql.

.EXAMPLE
.\Export-PostgresMigration.ps1 -OutputPath ".\artifacts\migration.sql"
Generates the migration script at the given path.

.NOTES
See the Migration README at ../tools/Postgres.MigrationsMain/README.md for more information.

.LINK
https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory=$false)]
    [string]
    $OutputPath = ".\out\postgres-migration.sql"
)

$ErrorActionPreference = "Stop"

try {
    $Top = "$PSScriptRoot/.."
    Push-Location $Top

    Write-Host "Building solution..." -ForegroundColor Cyan
    dotnet build
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed with exit code $LASTEXITCODE"
    }

    Write-Host "Generating migration SQL script at '$OutputPath'..." -ForegroundColor Cyan
    dotnet ef migrations script -i -o $OutputPath --project ".\src\Data\Postgres\" --startup-project ".\tools\Postgres.MigrationsMain\" --context ApplicationDbContext
    if ($LASTEXITCODE -ne 0) {
        throw "Migration script generation failed with exit code $LASTEXITCODE"
    }

    Write-Host "Migration SQL script generated at '$OutputPath'" -ForegroundColor Green
}
catch {
    Write-Error "Failed to generate migration script: $_"
    Write-Error $_.ScriptStackTrace
    exit 1
}
finally {
    Pop-Location
}
