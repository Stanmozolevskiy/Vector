# Test script to verify code execution output
# This tests the /run endpoint to ensure output is captured correctly

$baseUrl = "http://localhost:5000/api"
$token = Read-Host "Enter your JWT token (or press Enter to skip auth)"

# Get a question ID (Two Sum)
Write-Host "`nGetting questions..." -ForegroundColor Cyan
$headers = @{
    "Content-Type" = "application/json"
}
if ($token) {
    $headers["Authorization"] = "Bearer $token"
}

try {
    $questionsResponse = Invoke-RestMethod -Uri "$baseUrl/question" -Method Get -Headers $headers
    $twoSumQuestion = $questionsResponse | Where-Object { $_.title -like "*Two Sum*" } | Select-Object -First 1
    
    if (-not $twoSumQuestion) {
        Write-Host "Could not find Two Sum question. Using first question." -ForegroundColor Yellow
        $twoSumQuestion = $questionsResponse | Select-Object -First 1
    }
    
    $questionId = $twoSumQuestion.id
    Write-Host "Using question: $($twoSumQuestion.title) (ID: $questionId)" -ForegroundColor Green
    
    # Test code execution
    Write-Host "`nTesting code execution..." -ForegroundColor Cyan
    $testCode = @{
        sourceCode = "var twoSum = function(nums, target) { return [0, 1]; };"
        language = "javascript"
    } | ConvertTo-Json
    
    Write-Host "Request body:" -ForegroundColor Yellow
    Write-Host $testCode -ForegroundColor Gray
    
    $response = Invoke-RestMethod -Uri "$baseUrl/CodeExecution/run/$questionId" -Method Post -Headers $headers -Body $testCode
    
    Write-Host "`nResponse:" -ForegroundColor Green
    $response | ConvertTo-Json -Depth 10 | Write-Host
    
    Write-Host "`n=== Analysis ===" -ForegroundColor Cyan
    if ($response -is [Array] -and $response.Count -gt 0) {
        $firstResult = $response[0]
        Write-Host "Test Case 1:" -ForegroundColor Yellow
        Write-Host "  Status: $($firstResult.status)" -ForegroundColor $(if ($firstResult.status -eq "Accepted") { "Green" } else { "Red" })
        Write-Host "  Output: $($firstResult.output)" -ForegroundColor $(if ($firstResult.output -and $firstResult.output -ne "(No output - code executed successfully)") { "Green" } else { "Red" })
        Write-Host "  Expected: $($firstResult.expectedOutput)" -ForegroundColor Gray
        Write-Host "  Passed: $($firstResult.passed)" -ForegroundColor $(if ($firstResult.passed) { "Green" } else { "Red" })
        
        if ($firstResult.output -eq "(No output - code executed successfully)") {
            Write-Host "`n❌ ISSUE: Output shows '(No output...)' instead of actual result!" -ForegroundColor Red
            Write-Host "   Expected to see: '[0,1]' or similar" -ForegroundColor Yellow
        } elseif ($firstResult.output) {
            Write-Host "`n✅ SUCCESS: Output is being captured!" -ForegroundColor Green
            Write-Host "   Output: $($firstResult.output)" -ForegroundColor Green
        } else {
            Write-Host "`n⚠️  WARNING: Output is empty" -ForegroundColor Yellow
        }
    } else {
        Write-Host "Unexpected response format" -ForegroundColor Red
        $response | ConvertTo-Json -Depth 10
    }
    
} catch {
    Write-Host "`nError: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.ErrorDetails) {
        Write-Host "Details: $($_.ErrorDetails.Message)" -ForegroundColor Red
    }
}

