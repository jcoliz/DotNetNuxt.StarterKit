<#
.SYNOPSIS
Starts the Docker CI containers locally.

.DESCRIPTION
This script starts the Docker containers defined in docker-compose-ci.yml in detached mode
and waits for them to be ready. Once the containers are running, it automatically opens
the application in a browser at http://localhost:5400. This is useful for debugging CI
build issues locally and running functional tests locally.

.EXAMPLE
.\Start-Container.ps1
Starts the Docker containers and opens the application in a browser.

.EXAMPLE
.\Start-Container.ps1 -Build
Builds the containers first using Build-Container.ps1 if it exists, then starts the stack.

.NOTES
The containers run in detached mode (-d) and the script waits (--wait) for them to be ready.
Use Stop-Container.ps1 to stop the containers when finished.

.LINK
https://docs.docker.com/compose/
#>


[CmdletBinding()]
param(
    [switch]$Build
)

$ErrorActionPreference = "Stop"

Import-Module (Join-Path $PSScriptRoot "DockerUtilities.psm1") -Force

try {
    if (-not (Test-DockerRunning)) {
        Write-Error "Docker is not running. Please start Docker Desktop and try again."
        exit 1
    }

    if ($Build) {
        $buildScript = Join-Path $PSScriptRoot "Build-Container.ps1"
        if (Test-Path $buildScript) {
            Write-Host "Running Build-Container.ps1 before startup..." -ForegroundColor Cyan
            & $buildScript
            if ($LASTEXITCODE -ne 0) {
                throw "Build-Container.ps1 failed with exit code $LASTEXITCODE"
            }
        }
        else {
            Write-Warning "Build-Container.ps1 was not found at $buildScript; continuing without a build step."
        }
    }

    Write-Host "Starting Docker CI containers..." -ForegroundColor Cyan
    docker compose -f "$PSScriptRoot/../docker/docker-compose-ci.yml" up -d --wait
    if ($LASTEXITCODE -ne 0) {
        throw "Docker compose up failed with exit code $LASTEXITCODE"
    }

    Write-Host "OK Containers started successfully" -ForegroundColor Green
    Write-Host ""
    Write-Host "Opening application and dashboard..." -ForegroundColor Cyan
    Start-Process "http://localhost:18888"  # Aspire Dashboard
    Start-Process "http://localhost:5401/swagger"  # Backend API Inspector
#TODO:    Start-Process "http://localhost:5400"   # Frontend
}
catch {
    Write-Error "Failed to start containers: $_"
    Write-Error $_.ScriptStackTrace
    exit 1
}
