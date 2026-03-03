$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$bundle = Join-Path $root 'deploy-cn\bundle'
$apiOut = Join-Path $bundle 'api-publish'
$webOut = Join-Path $bundle 'web-dist'

if (Test-Path $bundle) {
    Remove-Item -Recurse -Force $bundle
}
New-Item -ItemType Directory -Path $apiOut | Out-Null
New-Item -ItemType Directory -Path $webOut | Out-Null

Write-Host '[1/4] Build frontend' -ForegroundColor Cyan
npm --prefix (Join-Path $root 'pms-web') run build

Write-Host '[2/4] Publish backend' -ForegroundColor Cyan
dotnet publish (Join-Path $root 'PMS.API\PMS.API.csproj') -c Release -o $apiOut

Write-Host '[3/4] Copy frontend dist' -ForegroundColor Cyan
Copy-Item -Recurse -Force (Join-Path $root 'pms-web\dist\*') $webOut

Write-Host '[4/4] Bundle ready' -ForegroundColor Green
Write-Host "Output: $bundle"
Write-Host 'Upload deploy-cn directory to ECS, then run deploy-on-ecs.sh as root/sudo user.'
