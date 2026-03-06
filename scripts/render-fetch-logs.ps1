# Fetch logs from Render via API
# Prereqs: Set RENDER_API_KEY and RENDER_OWNER_ID env vars
#   - API key: https://dashboard.render.com → Account Settings → API Keys
#   - Owner ID: from dashboard URL or GET https://api.render.com/v1/owners
# Usage: .\render-fetch-logs.ps1 -ServiceId "srv-xxx" [-Limit 50] [-Type "app"|"build"]

param(
    [Parameter(Mandatory=$true)]
    [string]$ServiceId,
    [int]$Limit = 50,
    [ValidateSet("app","build","request")]
    [string]$Type = "app"
)

$apiKey = $env:RENDER_API_KEY
$ownerId = $env:RENDER_OWNER_ID
if (-not $apiKey) {
    Write-Error "Set RENDER_API_KEY. Create at https://dashboard.render.com → Account Settings → API Keys"
    exit 1
}
if (-not $ownerId) {
    Write-Error "Set RENDER_OWNER_ID. Find in dashboard URL or run: curl -H 'Authorization: Bearer $env:RENDER_API_KEY' https://api.render.com/v1/owners"
    exit 1
}

$params = @{
    ownerId   = $ownerId
    resource  = $ServiceId
    type      = $Type
    limit     = [Math]::Min($Limit, 100)
    direction = "backward"
}
$query = ($params.GetEnumerator() | ForEach-Object { "$($_.Key)=$($_.Value)" }) -join "&"
$url = "https://api.render.com/v1/logs?$query"

$headers = @{
    "Authorization" = "Bearer $apiKey"
    "Accept"        = "application/json"
}

Write-Host "Fetching $Type logs for $ServiceId..." -ForegroundColor Cyan
$resp = Invoke-RestMethod -Uri $url -Headers $headers -Method Get
$resp.logs | ForEach-Object { "$($_.timestamp) $($_.message)" } | Write-Output
