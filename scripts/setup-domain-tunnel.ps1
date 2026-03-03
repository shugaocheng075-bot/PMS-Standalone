param(
    [string]$Domain = 'bjgoodwill.fun',
    [string]$ApiDomain = 'api.bjgoodwill.fun',
    [string]$TunnelName = 'pms-bjgoodwill',
    [string]$CloudflaredExe = 'D:\Projects\_bin\cloudflared.exe',
    [string]$RuntimeDir = 'D:\Projects\_runtime\cloudflared'
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path $CloudflaredExe)) {
    throw "未找到 cloudflared：$CloudflaredExe"
}

$homeCloudflared = Join-Path $env:USERPROFILE '.cloudflared'
$certPath = Join-Path $homeCloudflared 'cert.pem'
if (-not (Test-Path $certPath)) {
    Write-Host '未检测到 Cloudflare 凭据 cert.pem，先执行以下命令完成一次授权：' -ForegroundColor Yellow
    Write-Host "  & '$CloudflaredExe' tunnel login" -ForegroundColor Cyan
    exit 2
}

if (-not (Test-Path $RuntimeDir)) {
    New-Item -ItemType Directory -Path $RuntimeDir | Out-Null
}

Write-Host "检查隧道：$TunnelName" -ForegroundColor Cyan
$listJson = & $CloudflaredExe tunnel list --output json | Out-String
$tunnels = @()
if (-not [string]::IsNullOrWhiteSpace($listJson)) {
    $tunnels = $listJson | ConvertFrom-Json
}

$tunnel = $tunnels | Where-Object { $_.name -eq $TunnelName } | Select-Object -First 1
if (-not $tunnel) {
    Write-Host "创建隧道：$TunnelName" -ForegroundColor Cyan
    & $CloudflaredExe tunnel create $TunnelName | Out-Host
    $listJson = & $CloudflaredExe tunnel list --output json | Out-String
    $tunnels = $listJson | ConvertFrom-Json
    $tunnel = $tunnels | Where-Object { $_.name -eq $TunnelName } | Select-Object -First 1
}

if (-not $tunnel) {
    throw "隧道创建失败：$TunnelName"
}

$tunnelId = [string]$tunnel.id
$credentialsFile = Join-Path $homeCloudflared "$tunnelId.json"
if (-not (Test-Path $credentialsFile)) {
    throw "未找到隧道凭据文件：$credentialsFile"
}

Write-Host "绑定 DNS：$Domain / $ApiDomain" -ForegroundColor Cyan
& $CloudflaredExe tunnel route dns $TunnelName $Domain | Out-Host
& $CloudflaredExe tunnel route dns $TunnelName $ApiDomain | Out-Host

$configPath = Join-Path $RuntimeDir 'config.yml'
$config = @"
tunnel: $tunnelId
credentials-file: $credentialsFile

ingress:
  - hostname: $Domain
    path: ^/api/.*
    service: http://127.0.0.1:5111
  - hostname: $Domain
    service: http://127.0.0.1:5173
  - hostname: $ApiDomain
    service: http://127.0.0.1:5111
  - service: http_status:404
"@

Set-Content -Path $configPath -Value $config -Encoding UTF8

Write-Host ''
Write-Host 'Setup completed. Start command:' -ForegroundColor Green
Write-Host "  & '$CloudflaredExe' tunnel --config '$configPath' run $TunnelName" -ForegroundColor Cyan
Write-Host ''
Write-Host 'Endpoints:' -ForegroundColor Green
Write-Host "  Frontend: https://$Domain"
Write-Host "  API: https://$ApiDomain or https://$Domain/api/*"
