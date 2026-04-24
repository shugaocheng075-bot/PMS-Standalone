$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

$base = 'http://localhost:5111'
$stagePending = ([string][char]0x672A) + ([char]0x53D1)
$stageEmailSent = ([string][char]0x5DF2) + ([char]0x53D1) + ([char]0x90AE) + ([char]0x4EF6)
$stageInProgress = ([string][char]0x4EA4) + ([char]0x63A5) + ([char]0x4E2D)
$stageCompleted = ([string][char]0x5DF2) + ([char]0x4EA4) + ([char]0x63A5)

function Invoke-JsonApi {
    param(
        [string]$Method,
        [string]$Url,
        [string]$Token,
        [string]$Body = ''
    )

    $headers = @{}
    if ($Token) {
        $headers['Authorization'] = "Bearer $Token"
    }

    $params = @{
        Method = $Method
        Uri = $Url
        Headers = $headers
        TimeoutSec = 15
        UseBasicParsing = $true
    }

    if ($Body) {
        $params['ContentType'] = 'application/json'
        $params['Body'] = $Body
    }

    try {
        $response = Invoke-WebRequest @params
        return $response.Content | ConvertFrom-Json
    } catch {
        if ($_.Exception.Response) {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $content = $reader.ReadToEnd()
            throw "HTTP $([int]$_.Exception.Response.StatusCode): $content"
        }
        throw
    }
}

for ($i = 0; $i -lt 20; $i++) {
    try {
        $ready = Invoke-WebRequest -Uri "$base/swagger/index.html" -TimeoutSec 5 -UseBasicParsing
        if ($ready.StatusCode -eq 200) {
            break
        }
    } catch {}
}

$loginBody = @{ account = 'admin'; password = '123456' } | ConvertTo-Json -Compress
$login = Invoke-JsonApi -Method POST -Url "$base/api/auth/login" -Body $loginBody
$token = $login.data.accessToken
if (-not $token) {
    throw 'login failed'
}

$items = (Invoke-JsonApi -Method GET -Url "$base/api/handovers?page=1&size=100000" -Token $token).data.items
if (-not $items -or $items.Count -eq 0) {
    throw 'no handover rows found'
}

$target = $items | Where-Object { $_.stage -eq $stagePending } | Select-Object -First 1
if (-not $target) {
    $target = $items | Where-Object { $_.stage -eq $stageEmailSent } | Select-Object -First 1
}
if (-not $target) {
    $target = $items | Where-Object { $_.stage -eq $stageInProgress } | Select-Object -First 1
}
if (-not $target) {
    $target = $items | Where-Object { $_.stage -eq $stageCompleted } | Select-Object -First 1
}
if (-not $target) {
    throw 'no usable handover row found'
}

$id = [int64]$target.id
$originalStage = [string]$target.stage

Write-Host "selectedId=$id originalStage=$originalStage"

if ($originalStage -eq $stagePending) {
    $send = Invoke-JsonApi -Method PATCH -Url "$base/api/handovers/$id/send-email" -Token $token
    $start = Invoke-JsonApi -Method PATCH -Url "$base/api/handovers/$id/start" -Token $token
    $complete = Invoke-JsonApi -Method PATCH -Url "$base/api/handovers/$id/complete" -Token $token
    $rollback1 = Invoke-JsonApi -Method PATCH -Url "$base/api/handovers/$id/rollback" -Token $token
    $rollback2 = Invoke-JsonApi -Method PATCH -Url "$base/api/handovers/$id/rollback" -Token $token
    $rollback3 = Invoke-JsonApi -Method PATCH -Url "$base/api/handovers/$id/rollback" -Token $token

    Write-Host "sendEmailStage=$($send.data.stage) emailSentDate=$($send.data.emailSentDate)"
    Write-Host "startStage=$($start.data.stage) startedAt=$($start.data.startedAt)"
    Write-Host "completeStage=$($complete.data.stage) completedAt=$($complete.data.completedAt)"
    Write-Host "rollback1Stage=$($rollback1.data.stage)"
    Write-Host "rollback2Stage=$($rollback2.data.stage)"
    Write-Host "rollback3Stage=$($rollback3.data.stage)"
} elseif ($originalStage -eq $stageEmailSent) {
    $start = Invoke-JsonApi -Method PATCH -Url "$base/api/handovers/$id/start" -Token $token
    $complete = Invoke-JsonApi -Method PATCH -Url "$base/api/handovers/$id/complete" -Token $token
    $rollback1 = Invoke-JsonApi -Method PATCH -Url "$base/api/handovers/$id/rollback" -Token $token
    $rollback2 = Invoke-JsonApi -Method PATCH -Url "$base/api/handovers/$id/rollback" -Token $token

    Write-Host "startStage=$($start.data.stage) startedAt=$($start.data.startedAt)"
    Write-Host "completeStage=$($complete.data.stage) completedAt=$($complete.data.completedAt)"
    Write-Host "rollback1Stage=$($rollback1.data.stage)"
    Write-Host "rollback2Stage=$($rollback2.data.stage)"
} elseif ($originalStage -eq $stageInProgress) {
    $complete = Invoke-JsonApi -Method PATCH -Url "$base/api/handovers/$id/complete" -Token $token
    $rollback = Invoke-JsonApi -Method PATCH -Url "$base/api/handovers/$id/rollback" -Token $token

    Write-Host "completeStage=$($complete.data.stage) completedAt=$($complete.data.completedAt)"
    Write-Host "rollbackStage=$($rollback.data.stage)"
} else {
    $rollback = Invoke-JsonApi -Method PATCH -Url "$base/api/handovers/$id/rollback" -Token $token
    $complete = Invoke-JsonApi -Method PATCH -Url "$base/api/handovers/$id/complete" -Token $token

    Write-Host "rollbackStage=$($rollback.data.stage) startedAt=$($rollback.data.startedAt)"
    Write-Host "completeStage=$($complete.data.stage) completedAt=$($complete.data.completedAt)"
}

$detail = Invoke-JsonApi -Method GET -Url "$base/api/handovers/$id" -Token $token
Write-Host "finalStage=$($detail.data.stage)"

if ([string]$detail.data.stage -ne $originalStage) {
    throw "stage restore mismatch: expected $originalStage actual $($detail.data.stage)"
}