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

$workDate = (Get-Date).ToString('yyyy-MM-dd')
$createBody = @{
    projectId = [int64]$project.id
    opportunityNumber = [string]$project.opportunityNumber
    hospitalName = [string]$project.hospitalName
    productName = [string]$project.productName
    workDate = $workDate
    hours = 4
    workType = '远程'
    implementationStatus = [string]$project.implementationStatus
    description = 'Copilot workhours workflow regression'
} | ConvertTo-Json -Compress

$created = Invoke-JsonApi -Method POST -Url "$base/api/workhours" -Token $token -Body $createBody
$id = [int64]$created.data.id
if (-not $id) {
    throw 'create failed'
}

try {
    $submit1 = Invoke-JsonApi -Method PATCH -Url "$base/api/workhours/$id/submit" -Token $token
    $reject = Invoke-JsonApi -Method PATCH -Url "$base/api/workhours/$id/reject" -Token $token

    $updateBody = @{
        projectId = [int64]$project.id
        opportunityNumber = [string]$project.opportunityNumber
        hospitalName = [string]$project.hospitalName
        productName = [string]$project.productName
        workDate = $workDate
        hours = 6
        workType = '驻场'
        implementationStatus = [string]$project.implementationStatus
        description = 'Copilot workhours workflow regression updated'
    } | ConvertTo-Json -Compress
    $updated = Invoke-JsonApi -Method PUT -Url "$base/api/workhours/$id" -Token $token -Body $updateBody

    $submit2 = Invoke-JsonApi -Method PATCH -Url "$base/api/workhours/$id/submit" -Token $token
    $confirm = Invoke-JsonApi -Method PATCH -Url "$base/api/workhours/$id/confirm" -Token $token
    $detail = Invoke-JsonApi -Method GET -Url "$base/api/workhours/$id" -Token $token

    Write-Host "createdId=$id"
    Write-Host "submit1Status=$($submit1.data.status)"
    Write-Host "rejectStatus=$($reject.data.status) confirmedBy=$($reject.data.confirmedBy) confirmedAt=$($reject.data.confirmedAt)"
    Write-Host "updateStatus=$($updated.data.status) hours=$($updated.data.hours) workType=$($updated.data.workType)"
    Write-Host "submit2Status=$($submit2.data.status)"
    Write-Host "confirmStatus=$($confirm.data.status) confirmedBy=$($confirm.data.confirmedBy) confirmedAt=$($confirm.data.confirmedAt)"
    Write-Host "detailStatus=$($detail.data.status)"
}
finally {
    try {
        $null = Invoke-JsonApi -Method DELETE -Url "$base/api/workhours/$id" -Token $token
        Write-Host 'cleanup=deleted'
    } catch {
        Write-Host "cleanup=failed $_"
    }
}