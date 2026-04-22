param(
    [string]$NodeVersion = "22.15.0"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$webProjectDir = Join-Path $repoRoot "pms-web"

function Get-NvmExecutable {
    $candidates = @(
        (Join-Path $env:LOCALAPPDATA "nvm\nvm.exe"),
        (Join-Path $env:APPDATA "nvm\nvm.exe")
    )

    foreach ($candidate in $candidates) {
        if ($candidate -and (Test-Path $candidate)) {
            return $candidate
        }
    }

    return $null
}

$nvmExe = Get-NvmExecutable
if (-not $nvmExe) {
    throw "nvm.exe was not found. Install NVM for Windows or Node.js first."
}

$env:NVM_HOME = [Environment]::GetEnvironmentVariable("NVM_HOME", "Machine")
if (-not $env:NVM_HOME) {
    $env:NVM_HOME = [Environment]::GetEnvironmentVariable("NVM_HOME", "User")
}

$env:NVM_SYMLINK = [Environment]::GetEnvironmentVariable("NVM_SYMLINK", "Machine")
if (-not $env:NVM_SYMLINK) {
    $env:NVM_SYMLINK = [Environment]::GetEnvironmentVariable("NVM_SYMLINK", "User")
}

$env:Path = "$env:NVM_HOME;$env:NVM_SYMLINK;" + [Environment]::GetEnvironmentVariable("Path", "Machine") + ";" + [Environment]::GetEnvironmentVariable("Path", "User")

& $nvmExe use $NodeVersion | Out-Null

Set-Location $webProjectDir
npm run build
