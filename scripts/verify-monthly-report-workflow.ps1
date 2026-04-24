$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

$base = 'http://localhost:5111'

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

$projects = Invoke-JsonApi -Method GET -Url "$base/api/projects?page=1&size=1" -Token $token
$project = $projects.data.items[0]
if (-not $project) {
    throw 'no project found'
}

$reportMonth = (Get-Date).ToString('yyyy-MM')
$createBody = @{
    hospitalName = [string]$project.hospitalName
    reportMonth = $reportMonth
    title = 'Copilot monthly workflow regression'
    content = 'phase-2 monthly workflow regression content'
    attachments = @()
} | ConvertTo-Json -Compress

$created = Invoke-JsonApi -Method POST -Url "$base/api/monthly-reports" -Token $token -Body $createBody
$id = [int64]$created.data.id
if (-not $id) {
    throw 'create failed'
}

try {
    $submit1 = Invoke-JsonApi -Method PATCH -Url "$base/api/monthly-reports/$id/submit" -Token $token

    $rejectBody = @{ rejectionReason = 'workflow regression reject' } | ConvertTo-Json -Compress
    $reject = Invoke-JsonApi -Method PATCH -Url "$base/api/monthly-reports/$id/reject" -Token $token -Body $rejectBody

    $updateBody = @{
        hospitalName = [string]$project.hospitalName
        reportMonth = $reportMonth
        title = 'Copilot monthly workflow regression updated'
        content = 'phase-2 monthly workflow regression updated content'
        attachments = @()
    } | ConvertTo-Json -Compress
    $updated = Invoke-JsonApi -Method PUT -Url "$base/api/monthly-reports/$id" -Token $token -Body $updateBody

    $submit2 = Invoke-JsonApi -Method PATCH -Url "$base/api/monthly-reports/$id/submit" -Token $token
    $approve = Invoke-JsonApi -Method PATCH -Url "$base/api/monthly-reports/$id/approve" -Token $token
    $detail = Invoke-JsonApi -Method GET -Url "$base/api/monthly-reports/$id" -Token $token

    Write-Host "createdId=$id"
    Write-Host "submit1Status=$($submit1.data.status)"
    Write-Host "rejectStatus=$($reject.data.status) rejectionReason=$($reject.data.rejectionReason) approvedBy=$($reject.data.approvedBy) approvedAt=$($reject.data.approvedAt)"
    Write-Host "updateStatus=$($updated.data.status) title=$($updated.data.title)"
    Write-Host "submit2Status=$($submit2.data.status)"
    Write-Host "approveStatus=$($approve.data.status) approvedBy=$($approve.data.approvedBy) approvedAt=$($approve.data.approvedAt)"
    Write-Host "detailStatus=$($detail.data.status)"
}
finally {
    try {
        $null = Invoke-JsonApi -Method DELETE -Url "$base/api/monthly-reports/$id" -Token $token
        Write-Host 'cleanup=deleted'
    } catch {
        Write-Host "cleanup=failed $_"
    }
}