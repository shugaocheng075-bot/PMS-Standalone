$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

function Invoke-Api {
    param(
        [string]$Method,
        [string]$Url,
        [string]$Body,
        [string]$Token
    )
    $headers = @{}
    if ($Token) { $headers['Authorization'] = "Bearer $Token" }
    try {
        $resp = Invoke-WebRequest -Method $Method -Uri $Url -Headers $headers -Body $Body -ContentType 'application/json' -UseBasicParsing -TimeoutSec 8
        return @{ code = [int]$resp.StatusCode; body = $resp.Content }
    } catch {
        $r = $_.Exception.Response
        if ($r) {
            $code = [int]$r.StatusCode
            $b = ''
            try {
                $reader = New-Object System.IO.StreamReader($r.GetResponseStream())
                $b = $reader.ReadToEnd()
            } catch {}
            return @{ code = $code; body = $b }
        }
        return @{ code = 0; body = $_.Exception.Message }
    }
}

$base = 'http://localhost:5111'

# Wait for API readiness
for ($i = 0; $i -lt 20; $i++) {
    $ping = Invoke-Api GET "$base/api/health" $null $null
    if ($ping.code -eq 200) { break }
    Start-Sleep -Milliseconds 500
}

$h = Invoke-Api GET "$base/api/health" $null $null
Write-Host "[public-exact] /api/health => $($h.code)"

$hx = Invoke-Api GET "$base/api/healthxxx" $null $null
Write-Host "[public-prefix-blocked] /api/healthxxx => $($hx.code) (expect 401)"

$login = Invoke-Api POST "$base/api/auth/login" '{"account":"admin","password":"123456"}' $null
Write-Host "[login] => $($login.code)"

$token = $null
try {
    $parsed = $login.body | ConvertFrom-Json
    if ($parsed -and $parsed.data -and $parsed.data.token) { $token = $parsed.data.token }
} catch {}

if (-not $token) {
    Write-Host "login failed body: $($login.body)"
    exit 1
}
Write-Host "[token] acquired (length=$($token.Length))"

$bkDownload = Invoke-Api GET "$base/api/system/backup/download" $null $token
Write-Host "[backup-admin] GET backup/download => $($bkDownload.code) (expect 200 for admin)"

$info = Invoke-Api GET "$base/api/system/info" $null $token
Write-Host "[system-info] GET /api/system/info => $($info.code)"

# Data import path whitelist: try to read a file outside Data dir
$bad = Invoke-Api POST "$base/api/admin/import/text-file" '{"filePath":"C:/Windows/win.ini"}' $token
Write-Host "[import-whitelist] /api/admin/import/text-file win.ini => $($bad.code) body=$($bad.body)"

$badMd = Invoke-Api POST "$base/api/admin/import/major-demand" '{"filePath":"../../../../etc/hosts"}' $token
Write-Host "[import-whitelist] /api/admin/import/major-demand traversal => $($badMd.code) body=$($badMd.body)"
