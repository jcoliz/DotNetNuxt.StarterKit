<#
.SYNOPSIS
    Run selected test suites

.DESCRIPTION
    Builds the solution and runs unit and integration tests by default.
    Use -Unit, -Integration, or -Functional to run only that test suite.
    These switches cannot be combined.

    Projects are discovered beneath tests/Unit, tests/Integration, and
    tests/Functional, including dotted folders such as tests/Integration.Endpoints.
    Docker must be running when integration or functional tests are selected;
    the script checks this before building. Functional tests also require the
    application and any dependencies to already be running.
    Use -RunSettings to override the selected projects' default runsettings.

.EXAMPLE
    .\Run-Tests.ps1

    Builds the solution and runs all unit and integration tests.

.EXAMPLE
    .\Run-Tests.ps1 -Unit

    Runs only unit tests.

.EXAMPLE
    .\Run-Tests.ps1 -Integration

    Runs all integration test projects.

.EXAMPLE
    .\Run-Tests.ps1 -Functional

    Runs only functional tests against the running application.

.EXAMPLE
    .\Run-Tests.ps1 -Functional -RunSettings tests/Functional/runsettings/container.runsettings

    Runs functional tests with the Chromium container settings.
#>

[CmdletBinding(DefaultParameterSetName = "Default")]
param(
    [Parameter(Mandatory, ParameterSetName = "Unit")]
    [switch]$Unit,

    [Parameter(Mandatory, ParameterSetName = "Integration")]
    [switch]$Integration,

    [Parameter(Mandatory, ParameterSetName = "Functional")]
    [switch]$Functional,

    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string]$RunSettings
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path $PSScriptRoot -Parent
Push-Location $repoRoot

try {
    if ($RunSettings) {
        $RunSettings = (Resolve-Path -LiteralPath $RunSettings -ErrorAction Stop).Path
    }

    $suites = switch ($PSCmdlet.ParameterSetName) {
        "Unit" { "Unit" }
        "Integration" { "Integration" }
        "Functional" { "Functional" }
        default { "Unit"; "Integration" }
    }

    if ($suites -contains "Integration" -or $suites -contains "Functional") {
        Write-Host "Checking Docker is running..." -ForegroundColor Cyan
        Import-Module (Join-Path $PSScriptRoot "DockerUtilities.psm1")
        if (-not (Test-DockerRunning)) {
            throw "Docker is unavailable. Ensure Docker is installed and its daemon is running, then retry."
        }
    }

    $testProjects = @(
        foreach ($suite in $suites) {
            $projects = @(
                Get-ChildItem -Path (Join-Path $repoRoot "tests") -Directory |
                Where-Object { $_.Name -match "^$suite(\.|$)" } |
                Get-ChildItem -Recurse -Filter *.csproj -File |
                Sort-Object FullName
            )
            if ($projects.Count -eq 0) {
                throw "No $suite test projects found beneath tests."
            }
            foreach ($project in $projects) {
                [PSCustomObject]@{ Suite = $suite; Project = $project }
            }
        }
    )

    Write-Host "Running $($suites -join ' and ') tests..." -ForegroundColor Cyan

    Write-Host "`nBuilding solution..." -ForegroundColor Cyan
    dotnet build
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR Build failed" -ForegroundColor Red
        exit 1
    }
    Write-Host "OK Build succeeded" -ForegroundColor Green

    $results = @(
        foreach ($testProject in $testProjects) {
            Write-Host "`nRunning $($testProject.Suite) tests: $($testProject.Project.Name)..." -ForegroundColor Cyan
            $testArguments = @("test", $testProject.Project.FullName, "--no-build")
            if ($RunSettings) {
                $testArguments += @("--settings", $RunSettings)
            }
            $testOutput = dotnet @testArguments 2>&1
            $testExitCode = $LASTEXITCODE
            $testOutput | ForEach-Object { Write-Host $_ }

            $testCount = 0
            $duration = "N/A"
            if (($testOutput -join "`n") -match 'Total:\s+(\d+).*?Duration:\s+([^\r\n]+?)(?:\s+-\s+|\r?\n|$)') {
                $testCount = [int]$Matches[1]
                $duration = $Matches[2].Trim()
            }
            [PSCustomObject]@{
                Suite    = $testProject.Suite
                Project  = $testProject.Project.Name
                Count    = $testCount
                Duration = $duration
                ExitCode = $testExitCode
            }
        }
    )

    $totalTests = ($results | Measure-Object -Property Count -Sum).Sum

    Write-Host "`n==================== TEST SUMMARY ====================" -ForegroundColor Cyan
    foreach ($result in $results) {
        Write-Host "$($result.Suite): $($result.Project) - $($result.Count) tests in $($result.Duration)" -ForegroundColor White
    }
    Write-Host "======================================================" -ForegroundColor Cyan
    Write-Host "TOTAL: $totalTests tests" -ForegroundColor Cyan
    Write-Host "======================================================`n" -ForegroundColor Cyan

    $failedResults = @($results | Where-Object { $_.ExitCode -ne 0 })
    if ($failedResults.Count -gt 0) {
        foreach ($result in $failedResults) {
            Write-Host "WARNING $($result.Suite) tests failed: $($result.Project)" -ForegroundColor Yellow
        }
        exit 1
    }

    Write-Host "OK All selected tests passed" -ForegroundColor Green
}
finally {
    Pop-Location
}
