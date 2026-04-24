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
    Start-Sleep -Milliseconds 500
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

$payload = @{
    projectId = [int64]$project.id
    hospitalName = [string]$project.hospitalName
    productName = [string]$project.productName
    projectName = [string]$project.productName
    productCategory = ''
    issueCategory = 'api-regression'
    reporterName = ''
    severity = 'High'
    functionModule = 'sla-regression'
    description = 'Copilot repair workflow regression ticket'
    reportedAt = (Get-Date).ToString('o')
    actualWorkHours = $null
    content = ''
    resolution = ''
    attachmentImages = ''
    registrationStatus = ''
    status = 'pending'
    urgency = 'urgent'
} | ConvertTo-Json -Compress

$created = Invoke-JsonApi -Method POST -Url "$base/api/repair-records" -Token $token -Body $payload
$id = [int64]$created.data.id
if (-not $id) {
    throw 'create failed'
}

try {
    $acceptBody = @{ assigneeName = 'admin' } | ConvertTo-Json -Compress
    $resolveBody = @{ resolution = 'workflow regression resolved' } | ConvertTo-Json -Compress
    $reopenBody = @{ reason = 'verify reopen action' } | ConvertTo-Json -Compress
    $accept = Invoke-JsonApi -Method PATCH -Url "$base/api/repair-records/$id/accept" -Token $token -Body $acceptBody
    $resolve = Invoke-JsonApi -Method PATCH -Url "$base/api/repair-records/$id/resolve" -Token $token -Body $resolveBody
    $reopen = Invoke-JsonApi -Method PATCH -Url "$base/api/repair-records/$id/reopen" -Token $token -Body $reopenBody
    $detail = Invoke-JsonApi -Method GET -Url "$base/api/repair-records/$id" -Token $token
    $audit = Invoke-JsonApi -Method GET -Url "$base/api/audit-logs?page=1&size=20&module=repair" -Token $token
    $auditItems = @($audit.data.items | Where-Object { $_.target -eq "#$id" })

    Write-Host "createdId=$id"
    Write-Host "acceptStatus=$($accept.data.status) acceptedAt=$($accept.data.acceptedAt) slaDueAt=$($accept.data.slaDueAt)"
    Write-Host "resolveStatus=$($resolve.data.status) completedAt=$($resolve.data.completedAt)"
    Write-Host "reopenStatus=$($reopen.data.status) acceptedAt=$($reopen.data.acceptedAt) completedAt=$($reopen.data.completedAt) slaDueAt=$($reopen.data.slaDueAt)"
    Write-Host "detailStatus=$($detail.data.status)"
    Write-Host "auditCount=$($auditItems.Count)"
    foreach ($item in $auditItems) {
        Write-Host "audit=$($item.action)|$($item.target)|$($item.detail)"
    }
}
finally {
    try {
        $null = Invoke-JsonApi -Method DELETE -Url "$base/api/repair-records/$id" -Token $token
        Write-Host "cleanup=deleted"
    } catch {
        Write-Host "cleanup=failed $_"
    }
}
