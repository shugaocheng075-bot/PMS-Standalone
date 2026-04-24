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
        TimeoutSec = 20
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

$majorRowId = ''
$inspectionId = 0

try {
    $majorCreated = Invoke-JsonApi -Method POST -Url "$base/api/major-demands/rows" -Token $token
    $majorRowId = [string]$majorCreated.data.rowId
    if (-not $majorRowId) {
        throw 'major-demand create failed'
    }

    $majorAcceptBody = @{ owner = 'admin' } | ConvertTo-Json -Compress
    $majorCompleteBody = @{ note = 'workflow verification complete' } | ConvertTo-Json -Compress
    $majorReopenBody = @{ reason = 'workflow verification reopen' } | ConvertTo-Json -Compress

    $majorAccept = Invoke-JsonApi -Method POST -Url "$base/api/major-demands/$([uri]::EscapeDataString($majorRowId))/accept" -Token $token -Body $majorAcceptBody
    $majorComplete = Invoke-JsonApi -Method POST -Url "$base/api/major-demands/$([uri]::EscapeDataString($majorRowId))/complete" -Token $token -Body $majorCompleteBody
    $majorReopen = Invoke-JsonApi -Method POST -Url "$base/api/major-demands/$([uri]::EscapeDataString($majorRowId))/reopen" -Token $token -Body $majorReopenBody
    $majorSnapshot = Invoke-JsonApi -Method GET -Url "$base/api/major-demands" -Token $token
    $majorWorkflow = @($majorSnapshot.data.workflows | Where-Object { $_.rowId -eq $majorRowId })[0]

    Write-Host "majorRowId=$majorRowId"
    Write-Host "majorAcceptStatus=$($majorAccept.data.workflow.status) acceptedAt=$($majorAccept.data.workflow.acceptedAt) dueDate=$($majorAccept.data.workflow.dueDate)"
    Write-Host "majorCompleteStatus=$($majorComplete.data.workflow.status) completedAt=$($majorComplete.data.workflow.completedAt)"
    Write-Host "majorReopenStatus=$($majorReopen.data.workflow.status) acceptedAt=$($majorReopen.data.workflow.acceptedAt) completedAt=$($majorReopen.data.workflow.completedAt)"
    Write-Host "majorSnapshotStatus=$($majorWorkflow.status) logCount=$(@($majorWorkflow.logs).Count)"

    $inspectionPayload = @{
        hospitalName = 'Workflow Validation Hospital'
        productName = 'Workflow Validation Product'
        province = 'Zhejiang'
        hospitalLevel = 'Tier3'
        groupName = 'Validation'
        inspector = 'admin'
        planDate = (Get-Date).ToString('yyyy-MM-dd')
        actualDate = $null
        remarks = 'workflow validation plan'
    } | ConvertTo-Json -Compress

    $inspectionCreated = Invoke-JsonApi -Method POST -Url "$base/api/inspections" -Token $token -Body $inspectionPayload
    $inspectionId = [int]$inspectionCreated.data.id
    if (-not $inspectionId) {
        throw 'inspection create failed'
    }

    $inspectionStartBody = @{ inspector = 'admin' } | ConvertTo-Json -Compress
    $inspectionCompleteBody = @{ remarks = 'inspection workflow verified' } | ConvertTo-Json -Compress
    $inspectionReopenBody = @{ reason = 'reopen inspection workflow check' } | ConvertTo-Json -Compress

    $inspectionStart = Invoke-JsonApi -Method PATCH -Url "$base/api/inspections/$inspectionId/start" -Token $token -Body $inspectionStartBody
    $inspectionComplete = Invoke-JsonApi -Method PATCH -Url "$base/api/inspections/$inspectionId/complete" -Token $token -Body $inspectionCompleteBody
    $inspectionReopen = Invoke-JsonApi -Method PATCH -Url "$base/api/inspections/$inspectionId/reopen" -Token $token -Body $inspectionReopenBody
    $inspectionDetail = Invoke-JsonApi -Method GET -Url "$base/api/inspections?page=1&size=1000" -Token $token
    $inspectionRow = @($inspectionDetail.data.items | Where-Object { $_.id -eq $inspectionId })[0]
    $inspectionAudit = Invoke-JsonApi -Method GET -Url "$base/api/audit-logs?page=1&size=20&module=inspection" -Token $token
    $inspectionAuditItems = @($inspectionAudit.data.items | Where-Object { $_.target -eq "#$inspectionId" })

    Write-Host "inspectionId=$inspectionId"
    Write-Host "inspectionStartStatus=$($inspectionStart.data.status) startedAt=$($inspectionStart.data.startedAt) slaDueAt=$($inspectionStart.data.slaDueAt)"
    Write-Host "inspectionCompleteStatus=$($inspectionComplete.data.status) completedAt=$($inspectionComplete.data.completedAt) actualDate=$($inspectionComplete.data.actualDate)"
    Write-Host "inspectionReopenStatus=$($inspectionReopen.data.status) startedAt=$($inspectionReopen.data.startedAt) completedAt=$($inspectionReopen.data.completedAt) actualDate=$($inspectionReopen.data.actualDate)"
    Write-Host "inspectionSnapshotStatus=$($inspectionRow.status) auditCount=$($inspectionAuditItems.Count)"
    foreach ($item in $inspectionAuditItems) {
        Write-Host "inspectionAudit=$($item.action)|$($item.target)|$($item.detail)"
    }
}
finally {
    if ($majorRowId) {
        try {
            $deleteMajorBody = @{ rowIds = @($majorRowId) } | ConvertTo-Json -Compress
            $null = Invoke-JsonApi -Method POST -Url "$base/api/major-demands/rows/delete" -Token $token -Body $deleteMajorBody
            Write-Host 'majorCleanup=deleted'
        } catch {
            Write-Host "majorCleanup=failed $_"
        }
    }

    if ($inspectionId -gt 0) {
        try {
            $null = Invoke-JsonApi -Method DELETE -Url "$base/api/inspections/$inspectionId" -Token $token
            Write-Host 'inspectionCleanup=deleted'
        } catch {
            Write-Host "inspectionCleanup=failed $_"
        }
    }
}
