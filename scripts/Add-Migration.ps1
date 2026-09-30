<#
.SYNOPSIS
Adds a new Entity Framework migration for PostgreSQL.

.DESCRIPTION
This script creates a new Entity Framework Core migration for the application.
It builds the solution and then uses dotnet ef to add a migration for the
Postgres data project.

.PARAMETER Name
The name of the migration to create. This is required and should describe the database
schema changes being made.

.EXAMPLE
.\Add-Migration.ps1 -Name "AddUserTable"
Creates a new migration named "AddUserTable" for Postgres.

.EXAMPLE
.\Add-Migration.ps1 -Name "AddIndexes"
Creates a new migration named "AddIndexes" for Postgres.

.NOTES
The migration files will be created in the .\Migrations\ directory of the Postgres project.
See the Migration README at ../src/Data/Postgres.MigrationsMain/README.md for more information.

.LINK
https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [string]
    $Name
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

    Write-Host "Adding migration '$Name' for provider 'Postgres'..." -ForegroundColor Cyan
    dotnet ef migrations add $Name -o .\Migrations\ -n "DotNetNuxt.StarterKit.Data.Postgres.Migrations" --project ".\src\Data\Postgres\" --startup-project ".\src\Data\Postgres.MigrationsMain\" --context ApplicationDbContext
    if ($LASTEXITCODE -ne 0) {
        throw "Migration creation failed with exit code $LASTEXITCODE"
    }

    Write-Host "Migration '$Name' created successfully" -ForegroundColor Green
}
catch {
    Write-Error "Failed to add migration: $_"
    Write-Error $_.ScriptStackTrace
    exit 1
}
finally {
    Pop-Location
}
