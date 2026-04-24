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

function Assert-Equal {
    param(
        $Actual,
        $Expected,
        [string]$Message
    )

    if ($Actual -ne $Expected) {
        throw "$Message. expected=$Expected actual=$Actual"
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

$productName = 'workflow-product'
if (-not [string]::IsNullOrWhiteSpace([string]$project.productName)) {
    $productName = [string]$project.productName
}

$province = ''
if (-not [string]::IsNullOrWhiteSpace([string]$project.province)) {
    $province = [string]$project.province
}

$groupName = ''
if (-not [string]::IsNullOrWhiteSpace([string]$project.groupName)) {
    $groupName = [string]$project.groupName
}

$servicePerson = ''
if (-not [string]::IsNullOrWhiteSpace([string]$project.maintenancePersonName)) {
    $servicePerson = [string]$project.maintenancePersonName
}

$today = Get-Date
$startDate = $today.AddMonths(-11).ToString('yyyy-MM-dd')
$endDate = $today.AddMonths(1).ToString('yyyy-MM-dd')
$reportYear = $today.Year
$statusNotStarted = (-join ([char[]](0x672A, 0x5F00, 0x59CB)))
$statusWriting = (-join ([char[]](0x7F16, 0x5199, 0x4E2D)))
$statusSubmitted = (-join ([char[]](0x5DF2, 0x63D0, 0x4EA4)))
$statusCompleted = (-join ([char[]](0x5DF2, 0x5B8C, 0x6210)))

$createPayload = @{
    hospitalName = ([string]$project.hospitalName)
    productName = $productName
    opportunityNumber = "AR-$($today.ToString('yyyyMMddHHmmss'))"
    province = $province
    groupName = $groupName
    servicePerson = $servicePerson
    implementationStatus = 'workflow regression'
    maintenanceStartDate = $startDate
    maintenanceEndDate = $endDate
    reportYear = $reportYear
    remarks = 'annual workflow regression'
}
$createBody = $createPayload | ConvertTo-Json -Compress

$created = Invoke-JsonApi -Method POST -Url "$base/api/annual-reports" -Token $token -Body $createBody
$id = [int64]$created.data.id
if (-not $id) {
    throw 'create failed'
}

try {
    Assert-Equal $created.data.status $statusNotStarted 'create status mismatch'

    $start = Invoke-JsonApi -Method PATCH -Url "$base/api/annual-reports/$id/start" -Token $token
    Assert-Equal $start.data.status $statusWriting 'start status mismatch'

    $submit = Invoke-JsonApi -Method PATCH -Url "$base/api/annual-reports/$id/submit" -Token $token
    Assert-Equal $submit.data.status $statusSubmitted 'submit status mismatch'
    if (-not $submit.data.submitDate) {
        throw 'submit date missing after submit'
    }

    $complete = Invoke-JsonApi -Method PATCH -Url "$base/api/annual-reports/$id/complete" -Token $token
    Assert-Equal $complete.data.status $statusCompleted 'complete status mismatch'
    if (-not $complete.data.reviewer) {
        throw 'reviewer missing after complete'
    }
    if (-not $complete.data.reviewDate) {
        throw 'review date missing after complete'
    }

    $reopen = Invoke-JsonApi -Method PATCH -Url "$base/api/annual-reports/$id/reopen" -Token $token
    Assert-Equal $reopen.data.status $statusWriting 'reopen status mismatch'
    if ($reopen.data.submitDate) {
        throw 'submit date should be cleared after reopen'
    }
    if ($reopen.data.reviewer) {
        throw 'reviewer should be cleared after reopen'
    }
    if ($reopen.data.reviewDate) {
        throw 'review date should be cleared after reopen'
    }

    Write-Host "createdId=$id"
    Write-Host "createStatus=$($created.data.status)"
    Write-Host "startStatus=$($start.data.status)"
    Write-Host "submitStatus=$($submit.data.status) submitDate=$($submit.data.submitDate)"
    Write-Host "completeStatus=$($complete.data.status) reviewer=$($complete.data.reviewer) reviewDate=$($complete.data.reviewDate)"
    Write-Host "reopenStatus=$($reopen.data.status)"
}
finally {
    try {
        $null = Invoke-JsonApi -Method DELETE -Url "$base/api/annual-reports/$id" -Token $token
        Write-Host 'cleanup=deleted'
    } catch {
        Write-Host "cleanup=failed $_"
    }
}