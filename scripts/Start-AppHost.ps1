<#
.SYNOPSIS
Starts the .NET Aspire AppHost with hot reload enabled.

.DESCRIPTION
This script starts the AppHost project using dotnet watch for hot reload during development.
The AppHost provisions Postgres as part of the local environment.

.EXAMPLE
.\Start-AppHost.ps1
Starts the AppHost with dotnet watch.

.NOTES
Requires .NET SDK to be installed and available in PATH.
The AppHost project must exist in the AppHost directory.
Docker must be running before starting the AppHost.
#>

[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

Import-Module (Join-Path $PSScriptRoot "DockerUtilities.psm1") -Force

try {
    $repoRoot = Split-Path $PSScriptRoot -Parent
    $appHostPath = "$repoRoot/src/AppHost"

    if (-not (Test-Path $appHostPath)) {
        throw "AppHost directory not found: $appHostPath"
    }

    Write-Host "Checking Docker is running..." -ForegroundColor Cyan
    if (-not (Test-DockerRunning)) {
        throw "Docker is not running. Please start Docker Desktop and try again."
    }
    Write-Host "OK Docker is running" -ForegroundColor Green

    Push-Location $repoRoot

    Write-Host "Starting AppHost with dotnet watch..." -ForegroundColor Cyan
    Write-Host "Project: $appHostPath" -ForegroundColor Yellow
    Write-Host "Database: Postgres" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Press Ctrl+C to stop" -ForegroundColor Gray
    Write-Host ""

    dotnet watch run --project .\src\AppHost\

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet watch failed with exit code $LASTEXITCODE"
    }
}
catch {
    Write-Error "Failed to start AppHost: $_"
    Write-Error $_.ScriptStackTrace
    exit 1
}
finally {
    Pop-Location
}
