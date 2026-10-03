<#
.SYNOPSIS
    Build and start containers, run functional tests, and stop containers.

.DESCRIPTION
    Uses Start-Container.ps1 -Build, Run-Tests.ps1 -Functional, and
    Stop-Container.ps1. Always attempts container cleanup and restores the
    original working directory, including when startup or tests fail.

.EXAMPLE
    .\Run-FunctionalTestsVsContainer.ps1
#>

[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path $PSScriptRoot -Parent
Push-Location $repoRoot

try {
    & (Join-Path $PSScriptRoot "Start-Container.ps1") -Build
    if ($LASTEXITCODE -ne 0) {
        throw "Start-Container.ps1 failed with exit code $LASTEXITCODE"
    }

    & (Join-Path $PSScriptRoot "Run-Tests.ps1") -Functional
    if ($LASTEXITCODE -ne 0) {
        throw "Run-Tests.ps1 failed with exit code $LASTEXITCODE"
    }
}
finally {
    try {
        & (Join-Path $PSScriptRoot "Stop-Container.ps1")
        if ($LASTEXITCODE -ne 0) {
            throw "Stop-Container.ps1 failed with exit code $LASTEXITCODE"
        }
    }
    finally {
        Pop-Location
    }
}
