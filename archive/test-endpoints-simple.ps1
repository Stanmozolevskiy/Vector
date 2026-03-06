# Simple endpoint test
Write-Output "=== Testing Code Execution Endpoints ==="
Write-Output ""

# Login
Write-Output "1. Logging in..."
$login = @{ email = "admin@vector.com"; password = "Admin123!" } | ConvertTo-Json
try {
    $loginResp = Invoke-RestMethod -Uri "http://localhost:5000/api/auth/login" -Method POST -Body $login -ContentType "application/json"
    $token = $loginResp.accessToken
    Write-Output "✓ Login successful"
    Write-Output ""
} catch {
    Write-Output "✗ Login failed: $_"
    exit
}

# Test languages endpoint
Write-Output "2. Testing GET /api/codeexecution/languages"
try {
    $langs = Invoke-RestMethod -Uri "http://localhost:5000/api/codeexecution/languages" -Method GET -Headers @{ Authorization = "Bearer $token" }
    Write-Output "✓ Success - Found $($langs.Count) languages"
    Write-Output "First language: $($langs[0].name) (ID: $($langs[0].judge0LanguageId))"
    Write-Output ""
} catch {
    Write-Output "✗ Failed: $_"
    Write-Output ""
}

# Test execute endpoint - JavaScript
Write-Output "3. Testing POST /api/codeexecution/execute (JavaScript)"
$jsCode = @{
    sourceCode = "console.log('Hello World!');"
    language = "javascript"
    stdin = ""
} | ConvertTo-Json

try {
    $result = Invoke-RestMethod -Uri "http://localhost:5000/api/codeexecution/execute" -Method POST -Body $jsCode -ContentType "application/json" -Headers @{ Authorization = "Bearer $token" } -TimeoutSec 30
    Write-Output "✓ Success"
    Write-Output "  Status: $($result.status)"
    Write-Output "  Output: $($result.output)"
    Write-Output "  Runtime: $($result.runtime) ms"
    Write-Output "  Memory: $($result.memory) KB"
    if ($result.error) { Write-Output "  Error: $($result.error)" }
    Write-Output ""
} catch {
    Write-Output "✗ Failed: $_"
    if ($_.ErrorDetails) { Write-Output "  Details: $($_.ErrorDetails.Message)" }
    Write-Output ""
}

# Test execute endpoint - Python
Write-Output "4. Testing POST /api/codeexecution/execute (Python)"
$pyCode = @{
    sourceCode = "print('Hello from Python!')"
    language = "python"
    stdin = ""
} | ConvertTo-Json

try {
    $result = Invoke-RestMethod -Uri "http://localhost:5000/api/codeexecution/execute" -Method POST -Body $pyCode -ContentType "application/json" -Headers @{ Authorization = "Bearer $token" } -TimeoutSec 30
    Write-Output "✓ Success"
    Write-Output "  Status: $($result.status)"
    Write-Output "  Output: $($result.output)"
    Write-Output "  Runtime: $($result.runtime) ms"
    Write-Output "  Memory: $($result.memory) KB"
    if ($result.error) { Write-Output "  Error: $($result.error)" }
    Write-Output ""
} catch {
    Write-Output "✗ Failed: $_"
    if ($_.ErrorDetails) { Write-Output "  Details: $($_.ErrorDetails.Message)" }
    Write-Output ""
}

Write-Output "=== Tests Complete ==="

