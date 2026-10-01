<#
.SYNOPSIS
Stops the Docker CI containers locally.

.DESCRIPTION
This script stops and removes the Docker containers defined in docker-compose-ci.yml.
It cleans up all containers, networks, and volumes created by docker-compose up.
Use this script to stop containers started with Start-Container.ps1.

.EXAMPLE
.\Stop-Container.ps1
Stops and removes all Docker CI containers.

.NOTES
This command removes containers and networks but preserves volumes unless explicitly removed.

.LINK
https://docs.docker.com/compose/
#>

[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

Import-Module (Join-Path $PSScriptRoot "DockerUtilities.psm1") -Force

try {
    if (-not (Test-DockerRunning)) {
        Write-Error "Docker is not running. Please start Docker Desktop and try again."
        exit 1
    }

    Write-Host "Stopping Docker CI containers..." -ForegroundColor Cyan
    docker compose -f "$PSScriptRoot/../docker/docker-compose-ci.yml" down
    if ($LASTEXITCODE -ne 0) {
        throw "Docker compose down failed with exit code $LASTEXITCODE"
    }

    Write-Host "Containers stopped successfully" -ForegroundColor Green
}
catch {
    Write-Error "Failed to stop containers: $_"
    Write-Error $_.ScriptStackTrace
    exit 1
}
