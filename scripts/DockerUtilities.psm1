function Test-DockerRunning {
    [CmdletBinding()]
    param()

    try {
        $null = docker info 2>&1
        return ($LASTEXITCODE -eq 0)
    }
    catch {
        return $false
    }
}

Export-ModuleMember -Function Test-DockerRunning
