param(
    [string[]]$Ports = @('5111', '5173')
)

$ErrorActionPreference = "Stop"

$normalizedPorts = @(
    $Ports |
        ForEach-Object { $_ -split '[,;\s]+' } |
        Where-Object { $_ } |
        ForEach-Object { [int]$_ }
)

$Ports = if ($normalizedPorts.Count -gt 0) {
    $normalizedPorts
}
else {
    @(5111, 5173)
}

$targetPids = New-Object 'System.Collections.Generic.HashSet[int]'

foreach ($port in $Ports) {
    Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue |
        ForEach-Object {
            [void]$targetPids.Add([int]$_.OwningProcess)
        }
}

# Also stop the dedicated dev windows started by start-dev.ps1.
Get-Process -Name powershell, pwsh -ErrorAction SilentlyContinue |
    Where-Object { $_.MainWindowTitle -in @('PMS API', 'PMS Web') } |
    ForEach-Object {
        [void]$targetPids.Add([int]$_.Id)
    }

# Never stop the current script process.
[void]$targetPids.Remove($PID)

if ($targetPids.Count -eq 0) {
    Write-Host "No local dev services found on ports $($Ports -join ', ')."
    exit 0
}

$stopped = @()

foreach ($processId in $targetPids) {
    try {
        $proc = Get-Process -Id $processId -ErrorAction Stop
        Stop-Process -Id $processId -Force -ErrorAction Stop
        $stopped += "${processId} ($($proc.ProcessName))"
    }
    catch {
        Write-Warning "Failed to stop process ${processId}: $($_.Exception.Message)"
    }
}

if ($stopped.Count -gt 0) {
    Write-Host "Stopped local dev processes:"
    $stopped | ForEach-Object { Write-Host "  - $_" }
}
else {
    Write-Host "No processes were stopped."
}
