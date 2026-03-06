# Test Code Execution Endpoints
# This script tests all code execution endpoints with example payloads

Write-Host "=== Code Execution Endpoint Tests ===" -ForegroundColor Cyan
Write-Host ""

# Step 1: Authenticate
Write-Host "Step 1: Authenticating..." -ForegroundColor Yellow
$loginBody = @{
    email = "admin@vector.com"
    password = "Admin123!"
} | ConvertTo-Json

try {
    $loginResponse = Invoke-WebRequest -Uri "http://localhost:5000/api/auth/login" `
        -Method POST `
        -Body $loginBody `
        -ContentType "application/json" `
        -UseBasicParsing
    
    $authData = $loginResponse.Content | ConvertFrom-Json
    $token = $authData.accessToken
    Write-Host "✓ Authentication successful" -ForegroundColor Green
    Write-Host ""
} catch {
    Write-Host "✗ Authentication failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Step 2: Test GET /api/codeexecution/languages
Write-Host "Step 2: Testing GET /api/codeexecution/languages" -ForegroundColor Yellow
Write-Host "Endpoint: GET http://localhost:5000/api/codeexecution/languages" -ForegroundColor Gray
try {
    $langResponse = Invoke-WebRequest -Uri "http://localhost:5000/api/codeexecution/languages" `
        -Method GET `
        -Headers @{ Authorization = "Bearer $token" } `
        -UseBasicParsing
    
    Write-Host "Status: $($langResponse.StatusCode)" -ForegroundColor Green
    $languages = $langResponse.Content | ConvertFrom-Json
    Write-Host "Languages returned: $($languages.Count)" -ForegroundColor Green
    Write-Host "First 3 languages:" -ForegroundColor Gray
    $languages | Select-Object -First 3 | Format-Table name, value, judge0LanguageId
    Write-Host ""
} catch {
    Write-Host "✗ Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
}

# Step 3: Test POST /api/codeexecution/execute (JavaScript)
Write-Host "Step 3: Testing POST /api/codeexecution/execute (JavaScript)" -ForegroundColor Yellow
Write-Host "Endpoint: POST http://localhost:5000/api/codeexecution/execute" -ForegroundColor Gray
$jsPayload = @{
    sourceCode = "console.log('Hello from JavaScript!');"
    language = "javascript"
    stdin = ""
} | ConvertTo-Json

Write-Host "Payload:" -ForegroundColor Gray
Write-Host $jsPayload -ForegroundColor DarkGray
Write-Host ""

try {
    $execResponse = Invoke-WebRequest -Uri "http://localhost:5000/api/codeexecution/execute" `
        -Method POST `
        -Body $jsPayload `
        -ContentType "application/json" `
        -Headers @{ Authorization = "Bearer $token" } `
        -UseBasicParsing `
        -TimeoutSec 30
    
    Write-Host "Status: $($execResponse.StatusCode)" -ForegroundColor Green
    $result = $execResponse.Content | ConvertFrom-Json
    Write-Host "Response:" -ForegroundColor Green
    Write-Host "  Status: $($result.status)" -ForegroundColor White
    Write-Host "  Output: $($result.output)" -ForegroundColor White
    Write-Host "  Error: $($result.error)" -ForegroundColor $(if ($result.error) { "Red" } else { "Gray" })
    Write-Host "  Runtime: $($result.runtime) ms" -ForegroundColor White
    Write-Host "  Memory: $($result.memory) KB" -ForegroundColor White
    Write-Host ""
} catch {
    Write-Host "✗ Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "Response: $responseBody" -ForegroundColor Red
    }
    Write-Host ""
}

# Step 4: Test POST /api/codeexecution/execute (Python)
Write-Host "Step 4: Testing POST /api/codeexecution/execute (Python)" -ForegroundColor Yellow
$pythonPayload = @{
    sourceCode = "print('Hello from Python!')"
    language = "python"
    stdin = ""
} | ConvertTo-Json

Write-Host "Payload:" -ForegroundColor Gray
Write-Host $pythonPayload -ForegroundColor DarkGray
Write-Host ""

try {
    $execResponse = Invoke-WebRequest -Uri "http://localhost:5000/api/codeexecution/execute" `
        -Method POST `
        -Body $pythonPayload `
        -ContentType "application/json" `
        -Headers @{ Authorization = "Bearer $token" } `
        -UseBasicParsing `
        -TimeoutSec 30
    
    Write-Host "Status: $($execResponse.StatusCode)" -ForegroundColor Green
    $result = $execResponse.Content | ConvertFrom-Json
    Write-Host "Response:" -ForegroundColor Green
    Write-Host "  Status: $($result.status)" -ForegroundColor White
    Write-Host "  Output: $($result.output)" -ForegroundColor White
    Write-Host "  Error: $($result.error)" -ForegroundColor $(if ($result.error) { "Red" } else { "Gray" })
    Write-Host "  Runtime: $($result.runtime) ms" -ForegroundColor White
    Write-Host "  Memory: $($result.memory) KB" -ForegroundColor White
    Write-Host ""
} catch {
    Write-Host "✗ Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "Response: $responseBody" -ForegroundColor Red
    }
    Write-Host ""
}

# Step 5: Test with stdin input
Write-Host "Step 5: Testing POST /api/codeexecution/execute (with stdin)" -ForegroundColor Yellow
$inputPayload = @{
    sourceCode = "const readline = require('readline'); const rl = readline.createInterface({ input: process.stdin }); rl.on('line', (line) => { console.log('You entered:', line); });"
    language = "javascript"
    stdin = "Test Input Data"
} | ConvertTo-Json

Write-Host "Payload:" -ForegroundColor Gray
Write-Host $inputPayload -ForegroundColor DarkGray
Write-Host ""

try {
    $execResponse = Invoke-WebRequest -Uri "http://localhost:5000/api/codeexecution/execute" `
        -Method POST `
        -Body $inputPayload `
        -ContentType "application/json" `
        -Headers @{ Authorization = "Bearer $token" } `
        -UseBasicParsing `
        -TimeoutSec 30
    
    Write-Host "Status: $($execResponse.StatusCode)" -ForegroundColor Green
    $result = $execResponse.Content | ConvertFrom-Json
    Write-Host "Response:" -ForegroundColor Green
    Write-Host "  Status: $($result.status)" -ForegroundColor White
    Write-Host "  Output: $($result.output)" -ForegroundColor White
    Write-Host "  Error: $($result.error)" -ForegroundColor $(if ($result.error) { "Red" } else { "Gray" })
    Write-Host ""
} catch {
    Write-Host "✗ Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
}

# Step 6: Test validate endpoint (if questions exist)
Write-Host "Step 6: Testing POST /api/codeexecution/validate/{questionId}" -ForegroundColor Yellow
Write-Host "First, getting a question ID..." -ForegroundColor Gray

try {
    $questionsResponse = Invoke-WebRequest -Uri "http://localhost:5000/api/questions?pageSize=1" `
        -Method GET `
        -Headers @{ Authorization = "Bearer $token" } `
        -UseBasicParsing
    
    $questions = $questionsResponse.Content | ConvertFrom-Json
    if ($questions -and $questions.Count -gt 0) {
        $questionId = $questions[0].id
        Write-Host "Found question ID: $questionId" -ForegroundColor Green
        
        $validatePayload = @{
            sourceCode = "var twoSum = function(nums, target) { return [0, 1]; };"
            language = "javascript"
        } | ConvertTo-Json
        
        Write-Host "Payload:" -ForegroundColor Gray
        Write-Host $validatePayload -ForegroundColor DarkGray
        Write-Host ""
        
        $validateResponse = Invoke-WebRequest -Uri "http://localhost:5000/api/codeexecution/validate/$questionId" `
            -Method POST `
            -Body $validatePayload `
            -ContentType "application/json" `
            -Headers @{ Authorization = "Bearer $token" } `
            -UseBasicParsing `
            -TimeoutSec 60
        
        Write-Host "Status: $($validateResponse.StatusCode)" -ForegroundColor Green
        $validateResults = $validateResponse.Content | ConvertFrom-Json
        Write-Host "Test cases returned: $($validateResults.Count)" -ForegroundColor Green
        if ($validateResults.Count -gt 0) {
            Write-Host "First test case result:" -ForegroundColor Gray
            $validateResults[0] | Format-List testCaseNumber, passed, status, output, error, runtime, memory
        }
        Write-Host ""
    } else {
        Write-Host "No questions found in database" -ForegroundColor Yellow
        Write-Host ""
    }
} catch {
    Write-Host "✗ Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
}

Write-Host "=== Test Summary ===" -ForegroundColor Cyan
Write-Host "All endpoint tests completed!" -ForegroundColor Green

