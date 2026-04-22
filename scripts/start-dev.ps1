param(
    [string]$NodeVersion = "22.15.0"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$apiProjectDir = Join-Path $repoRoot "PMS.API"
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

function Start-DevWindow {
    param(
        [string]$Title,
        [string]$Command
    )

    $escapedCommand = $Command.Replace('"', '""')
    Start-Process powershell.exe -ArgumentList @(
        '-NoExit',
        '-Command',
        "`$Host.UI.RawUI.WindowTitle = '$Title'; $escapedCommand"
    ) | Out-Null
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "dotnet was not found. Install .NET 8 SDK first."
}

$nvmExe = Get-NvmExecutable
if (-not $nvmExe) {
    throw "nvm.exe was not found. Install NVM for Windows or Node.js first."
}

$nodeBootstrap = @(
    '$env:NVM_HOME = [Environment]::GetEnvironmentVariable("NVM_HOME","Machine")',
    'if (-not $env:NVM_HOME) { $env:NVM_HOME = [Environment]::GetEnvironmentVariable("NVM_HOME","User") }',
    '$env:NVM_SYMLINK = [Environment]::GetEnvironmentVariable("NVM_SYMLINK","Machine")',
    'if (-not $env:NVM_SYMLINK) { $env:NVM_SYMLINK = [Environment]::GetEnvironmentVariable("NVM_SYMLINK","User") }',
    '$env:Path = "$env:NVM_HOME;$env:NVM_SYMLINK;" + [Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [Environment]::GetEnvironmentVariable("Path","User")',
    "& '$nvmExe' use $NodeVersion | Out-Null"
) -join '; '

$apiCommand = @(
    "Set-Location '$apiProjectDir'",
    "dotnet run --project .\\PMS.API.csproj --launch-profile http"
) -join '; '

$webCommand = @(
    $nodeBootstrap,
    "Set-Location '$webProjectDir'",
    "npm run dev"
) -join '; '

Start-DevWindow -Title 'PMS API' -Command $apiCommand
Start-DevWindow -Title 'PMS Web' -Command $webCommand

Write-Host 'Started two development windows:'
Write-Host '  - PMS API  -> http://localhost:5111'
Write-Host '  - PMS Web  -> http://localhost:5173'