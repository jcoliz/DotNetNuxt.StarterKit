<#
.SYNOPSIS
    Setup development environment for new developers

.DESCRIPTION
    Prepares a freshly cloned repository for development by:
    - Verifying required tools are installed (.NET SDK, Node.js, pnpm)
    - Restoring .NET dependencies
    - Installing frontend npm packages
    - Building the solution
    - Running unit tests to verify setup

.EXAMPLE
    .\Setup-Development.ps1
    
    Runs the complete development environment setup process.

.NOTES
    Run this script after cloning the repository for the first time.
    Requires: .NET 10 SDK, Node.js 24+, npm (for pnpm installation), Docker (for Aspire Postgres)
#>

[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

# Ensure we're in the repository root
$repoRoot = Split-Path $PSScriptRoot -Parent
Push-Location $repoRoot

try {
    Write-Host "Setting up development environment..." -ForegroundColor Cyan

    # Check prerequisites
    Write-Host "`nChecking prerequisites..." -ForegroundColor Cyan

    # Check .NET SDK
    $requiredDotNetVersion = [System.Version]"10.0.100"
    try {
        $dotnetVersionString = dotnet --version
        $dotnetVersion = [System.Version]$dotnetVersionString
        if ($dotnetVersion -lt $requiredDotNetVersion) {
            Write-Host "WARNING .NET SDK version $dotnetVersionString is older than required $requiredDotNetVersion" -ForegroundColor Yellow
            Write-Host "        Download from: https://dotnet.microsoft.com/download" -ForegroundColor Yellow
        }
        Write-Host "OK .NET SDK: $dotnetVersionString" -ForegroundColor Green
    }
    catch {
        Write-Host "ERROR .NET SDK not found. Please install .NET 10 SDK" -ForegroundColor Red
        Write-Host "      Download from: https://dotnet.microsoft.com/download" -ForegroundColor Yellow
        exit 1
    }

    # Check Node.js
    $requiredNodeMajorVersion = 24
    try {
        $nodeVersion = node --version
        $nodeMajorVersion = [int]($nodeVersion -replace 'v(\d+)\..*', '$1')
        if ($nodeMajorVersion -lt $requiredNodeMajorVersion) {
            Write-Host "WARNING Node.js version $nodeVersion is older than required v$requiredNodeMajorVersion" -ForegroundColor Yellow
            Write-Host "        Download from: https://nodejs.org/" -ForegroundColor Yellow
        }
        Write-Host "OK Node.js: $nodeVersion" -ForegroundColor Green
    }
    catch {
        Write-Host "ERROR Node.js not found. Please install Node.js $requiredNodeMajorVersion+" -ForegroundColor Red
        Write-Host "      Download from: https://nodejs.org/" -ForegroundColor Yellow
        exit 1
    }

    # Check pnpm
    try {
        $pnpmVersion = pnpm --version
        Write-Host "OK pnpm: $pnpmVersion" -ForegroundColor Green
    }
    catch {
        Write-Host "WARNING pnpm not found. Installing..." -ForegroundColor Yellow
        npm install -g pnpm
    }

    # Check Docker (optional but recommended)
    try {
        docker --version | Out-Null
        Write-Host "OK Docker: $(docker --version)" -ForegroundColor Green
    }
    catch {
        Write-Host "WARNING Docker not found or not running" -ForegroundColor Yellow
        Write-Host "        Docker is required to run the AppHost (Postgres) and integration tests" -ForegroundColor Yellow
        Write-Host "        Download from: https://www.docker.com/products/docker-desktop" -ForegroundColor Yellow
    }

    # Restore .NET dependencies
    Write-Host "`nRestoring .NET dependencies..." -ForegroundColor Cyan
    dotnet restore
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR Failed to restore .NET dependencies" -ForegroundColor Red
        exit 1
    }
    Write-Host "OK .NET dependencies restored" -ForegroundColor Green

    # Install frontend dependencies
    Write-Host "`nInstalling frontend dependencies..." -ForegroundColor Cyan
    Push-Location src/FrontEnd.Nuxt
    try {
        pnpm install
        if ($LASTEXITCODE -ne 0) {
            Write-Host "ERROR Failed to install frontend dependencies" -ForegroundColor Red
            exit 1
        }
        Write-Host "OK Frontend dependencies installed" -ForegroundColor Green
    }
    finally {
        Pop-Location
    }

    # Build solution to verify everything works
    Write-Host "`nBuilding solution..." -ForegroundColor Cyan
    dotnet build
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR Build failed" -ForegroundColor Red
        exit 1
    }
    Write-Host "OK Solution built successfully" -ForegroundColor Green

    # Unit tests only; integration tests need Docker (Testcontainers)
    Write-Host "`nRunning unit tests..." -ForegroundColor Cyan
    dotnet test .\tests\Unit\DotNetNuxt.Tests.Unit.csproj --no-build
    if ($LASTEXITCODE -ne 0) {
        Write-Host "WARNING Some tests failed. Review test output above." -ForegroundColor Yellow
    }

    # Success message
    Write-Host "`nDevelopment environment setup complete!" -ForegroundColor Green
    Write-Host "`nNext steps:" -ForegroundColor Cyan
    Write-Host "  1. Run the application:"
    Write-Host "     ./scripts/Start-AppHost.ps1"
    Write-Host "`n  2. Or run in containers:"
    Write-Host "     ./scripts/Start-Container.ps1 -Build"
}
finally {
    Pop-Location
}