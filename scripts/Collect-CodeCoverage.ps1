<#
.SYNOPSIS
Runs unit tests and collects code coverage metrics.

.DESCRIPTION
This script executes unit tests with code coverage collection across only the application features.
It generates an HTML report showing coverage for the Application layer.

Coverage includes only the Application layer.
Functional tests are excluded as they test through the browser.

.EXAMPLE
.\Collect-CodeCoverage.ps1
Runs unit tests and generates a code coverage report.

.NOTES
Requires ReportGenerator to be installed globally:
    dotnet tool install -g dotnet-reportgenerator-globaltool

The report is generated in .\bin\coverage\ and opens automatically in your browser.

Coverage configuration is defined in Tests\Unit\coverlet.runsettings.

.LINK
https://github.com/danielpalme/ReportGenerator
#>

[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

function Run-TestsWithCoverage {
    param(
        [string]$TestProjectPath,
        [string]$TestName,
        [string]$SettingsPath
    )

    Write-Host "Running $TestName with code coverage..." -ForegroundColor Cyan
    Push-Location $TestProjectPath
    try {
        Remove-Item TestResults -Recurse -Force -ErrorAction SilentlyContinue
        dotnet test --no-build --collect:"XPlat Code Coverage" --settings:$SettingsPath
        if ($LASTEXITCODE -ne 0) {
            throw "$TestName execution failed with exit code $LASTEXITCODE"
        }
    }
    finally {
        Pop-Location
    }
}

try {
    if (-not (Get-Command reportgenerator -ErrorAction SilentlyContinue)) {
        throw "ReportGenerator is not installed or is not available on PATH. Install it with: dotnet tool install -g dotnet-reportgenerator-globaltool"
    }

    $repoRoot = Split-Path $PSScriptRoot -Parent
    $unitTestPath = "$repoRoot/Tests/Unit"
    $coverletSettingsPath = "$unitTestPath/coverlet.runsettings"
    $outputDir = "$unitTestPath/bin/coverage"

    # Verify test directories exist
    $testPaths = @{
        "Unit" = $unitTestPath
    }

    foreach ($testInfo in $testPaths.GetEnumerator()) {
        if (-not (Test-Path $testInfo.Value)) {
            throw "$($testInfo.Key) test directory not found: $($testInfo.Value)"
        }
    }

    if (-not (Test-Path $coverletSettingsPath)) {
        throw "Coverlet settings file not found: $coverletSettingsPath"
    }

    Write-Host "Collecting code coverage" -ForegroundColor Cyan
    Write-Host ""

    Write-Host "Cleaning up previous coverage results..." -ForegroundColor Cyan
    Remove-Item $outputDir -Recurse -Force -ErrorAction SilentlyContinue

    Write-Host "Cleaning solution..." -ForegroundColor Cyan
    Push-Location $repoRoot
    try {
        dotnet clean --nologo --verbosity quiet
        if ($LASTEXITCODE -ne 0) {
            throw "Clean failed with exit code $LASTEXITCODE"
        }

        Write-Host "Building solution..." -ForegroundColor Cyan
        dotnet build --nologo
        if ($LASTEXITCODE -ne 0) {
            throw "Build failed with exit code $LASTEXITCODE"
        }
    }
    finally {
        Pop-Location
    }

    Run-TestsWithCoverage -TestProjectPath $unitTestPath -TestName "unit tests" -SettingsPath $coverletSettingsPath

    Write-Host "`nGenerating coverage report from unit tests..." -ForegroundColor Cyan
    reportgenerator `
        -reports:"$unitTestPath/TestResults/*/coverage.cobertura.xml" `
        -targetdir:"$outputDir" `
        -filefilters:"-*.g.cs"
    if ($LASTEXITCODE -ne 0) {
        throw "Report generation failed with exit code $LASTEXITCODE"
    }

    Write-Host "`nOK Coverage report generated successfully" -ForegroundColor Green
    Write-Host "`nCoverage report available at:" -ForegroundColor Cyan
    Write-Host "  $outputDir/index.html" -ForegroundColor White
    Write-Host "`nOpening report in browser..." -ForegroundColor Cyan
    Start-Process "$outputDir/index.html"
}
catch {
    Write-Error "Failed to collect code coverage: $_"
    Write-Error $_.ScriptStackTrace
    exit 1
}
