# Judge0 API Implementation Guide

## Overview
This guide provides detailed information on implementing Judge0 API for code execution in the Vector platform.

## Is This in Stage 2?

**YES** - Code execution is planned for **Week 2: Code Editor & Execution Environment** (Day 7-8) in Stage 2.

**Current Status**: 
- ✅ Week 1 (Question Bank) - Completed
- ⏳ Week 2 (Code Execution) - **Next to implement**

---

## What Do We Need to Implement Judge0?

### 1. Infrastructure Requirements

#### Docker Services
Add Judge0 to `docker-compose.yml`:
```yaml
services:
  judge0:
    image: judge0/judge0:1.13.0
    ports:
      - "2358:2358"  # Judge0 API port
    environment:
      - REDIS_HOST=redis
      - POSTGRES_HOST=postgres
      - POSTGRES_DB=judge0
      - POSTGRES_USER=judge0
      - POSTGRES_PASSWORD=judge0
      - JUDGE0_SECRET_KEY=your-secret-key-here
    depends_on:
      - redis
      - postgres
    volumes:
      - judge0_data:/judge0
```

**Note**: Judge0 requires its own PostgreSQL database. You can either:
- Use a separate PostgreSQL instance for Judge0
- Or use the same PostgreSQL with a different database name

#### Backend Dependencies
Add to `backend/Vector.Api/Vector.Api.csproj`:
```xml
<PackageReference Include="System.Net.Http.Json" Version="8.0.0" />
```

#### Configuration
Add to `appsettings.json`:
```json
{
  "Judge0": {
    "BaseUrl": "http://judge0:2358",
    "ApiKey": "your-api-key-if-using-cloud",
    "TimeoutSeconds": 10
  }
}
```

---

## Data Format Requirements

### 1. Submission Format (POST /submissions)

#### Request Body
```json
{
  "source_code": "def twoSum(nums, target):\n    # Your code here\n    pass",
  "language_id": 71,
  "stdin": "[1, 2, 3]\n7",
  "expected_output": "[0, 1]",
  "cpu_time_limit": 5,
  "memory_limit": 128000,
  "wall_time_limit": 10,
  "max_processes_and_or_threads": 60,
  "enable_per_process_and_thread_time_limit": false,
  "enable_per_process_and_thread_memory_limit": false,
  "max_file_size": 1024
}
```

#### Required Fields
- `source_code` (string): The code to execute
- `language_id` (integer): Language identifier (see Language IDs below)

#### Optional Fields
- `stdin` (string): Standard input for the program
- `expected_output` (string): Expected output (for validation)
- `cpu_time_limit` (number): CPU time limit in seconds (default: 5)
- `memory_limit` (number): Memory limit in bytes (default: 128000 = 128MB)
- `wall_time_limit` (number): Wall clock time limit in seconds
- `max_processes_and_or_threads` (number): Process/thread limit
- `max_file_size` (number): Maximum file size in bytes

### 2. Response Format (GET /submissions/{token})

#### Success Response
```json
{
  "stdout": "[0, 1]",
  "stderr": null,
  "compile_output": null,
  "message": null,
  "status": {
    "id": 3,
    "description": "Accepted"
  },
  "time": "0.001",
  "memory": 1024,
  "token": "abc123..."
}
```

#### Status IDs
- `1` - In Queue
- `2` - Processing
- `3` - Accepted
- `4` - Wrong Answer
- `5` - Time Limit Exceeded
- `6` - Compilation Error
- `7` - Runtime Error (SIGSEGV)
- `8` - Runtime Error (SIGXFSZ)
- `9` - Runtime Error (SIGFPE)
- `10` - Runtime Error (SIGABRT)
- `11` - Runtime Error (NZEC)
- `12` - Runtime Error (Other)
- `13` - Internal Error
- `14` - Exec Format Error

### 3. Language IDs

```csharp
public static class Judge0LanguageIds
{
    public const int JavaScript = 63;      // Node.js 12.14.0
    public const int Python = 71;          // Python 3.8.1
    public const int Java = 62;            // OpenJDK 13.0.1
    public const int Cpp = 54;              // GCC 9.2.0
    public const int CSharp = 51;           // Mono 6.6.0.161
    public const int Go = 60;               // Go 1.13.5
}
```

---

## How to Provide Test Cases

### Option 1: Single Submission with Expected Output

For each test case, submit code separately:

```csharp
public async Task<TestResult> RunTestCase(
    string sourceCode, 
    int languageId, 
    string input, 
    string expectedOutput)
{
    var submission = new
    {
        source_code = sourceCode,
        language_id = languageId,
        stdin = input,
        expected_output = expectedOutput,
        cpu_time_limit = 5,
        memory_limit = 128000
    };
    
    // Submit
    var submitResponse = await _httpClient.PostAsync(
        $"{_judge0Url}/submissions?base64_encoded=false&wait=true",
        JsonContent.Create(submission)
    );
    
    var result = await submitResponse.Content.ReadFromJsonAsync<Judge0Result>();
    
    return new TestResult
    {
        Passed = result.Status.Id == 3, // Accepted
        Output = result.Stdout,
        Error = result.Stderr,
        Runtime = decimal.Parse(result.Time ?? "0"),
        Memory = result.Memory ?? 0
    };
}
```

### Option 2: Batch Submissions

Submit multiple test cases at once:

```csharp
public async Task<TestResult[]> RunTestCases(
    string sourceCode, 
    int languageId, 
    List<TestCase> testCases)
{
    var tasks = testCases.Select(async testCase =>
    {
        var submission = new
        {
            source_code = sourceCode,
            language_id = languageId,
            stdin = testCase.Input,
            expected_output = testCase.ExpectedOutput,
            cpu_time_limit = 5,
            memory_limit = 128000
        };
        
        var response = await _httpClient.PostAsync(
            $"{_judge0Url}/submissions?base64_encoded=false&wait=true",
            JsonContent.Create(submission)
        );
        
        return await response.Content.ReadFromJsonAsync<Judge0Result>();
    });
    
    var results = await Task.WhenAll(tasks);
    
    return results.Select((r, i) => new TestResult
    {
        TestCaseId = testCases[i].Id,
        Passed = r.Status.Id == 3,
        Output = r.Stdout,
        Error = r.Stderr,
        Runtime = decimal.Parse(r.Time ?? "0"),
        Memory = r.Memory ?? 0
    }).ToArray();
}
```

### Option 3: Async Submission (Recommended for Production)

For better performance, use async submission:

```csharp
public async Task<string> SubmitCodeAsync(string sourceCode, int languageId, string stdin)
{
    var submission = new
    {
        source_code = sourceCode,
        language_id = languageId,
        stdin = stdin,
        cpu_time_limit = 5,
        memory_limit = 128000
    };
    
    var response = await _httpClient.PostAsync(
        $"{_judge0Url}/submissions?base64_encoded=false&wait=false",
        JsonContent.Create(submission)
    );
    
    var result = await response.Content.ReadFromJsonAsync<Judge0SubmissionResponse>();
    return result.Token; // Return token to poll for results
}

public async Task<Judge0Result> GetSubmissionResultAsync(string token)
{
    var response = await _httpClient.GetAsync(
        $"{_judge0Url}/submissions/{token}?base64_encoded=false"
    );
    
    return await response.Content.ReadFromJsonAsync<Judge0Result>();
}

// Poll until result is ready
public async Task<Judge0Result> WaitForResultAsync(string token, int maxWaitSeconds = 30)
{
    var startTime = DateTime.UtcNow;
    
    while (DateTime.UtcNow - startTime < TimeSpan.FromSeconds(maxWaitSeconds))
    {
        var result = await GetSubmissionResultAsync(token);
        
        // Status 1 = In Queue, Status 2 = Processing
        if (result.Status.Id != 1 && result.Status.Id != 2)
        {
            return result;
        }
        
        await Task.Delay(500); // Wait 500ms before polling again
    }
    
    throw new TimeoutException("Code execution timed out");
}
```

---

## Test Case Data Format

### From Database (QuestionTestCase Model)

```csharp
public class QuestionTestCase
{
    public Guid Id { get; set; }
    public Guid QuestionId { get; set; }
    public string Input { get; set; }        // JSON string: "[1, 2, 3]"
    public string ExpectedOutput { get; set; } // JSON string: "[0, 1]"
    public bool IsHidden { get; set; }
    public int TestCaseNumber { get; set; }
}
```

### Example Test Cases

**Test Case 1** (Visible to user):
```json
{
  "input": "[2, 7, 11, 15]\n9",
  "expectedOutput": "[0, 1]",
  "isHidden": false,
  "testCaseNumber": 1
}
```

**Test Case 2** (Hidden):
```json
{
  "input": "[3, 2, 4]\n6",
  "expectedOutput": "[1, 2]",
  "isHidden": true,
  "testCaseNumber": 2
}
```

### Converting Test Cases for Judge0

```csharp
public class TestCaseConverter
{
    public static string FormatInputForJudge0(string input)
    {
        // Input is stored as JSON string, convert to plain string for stdin
        // Example: "[1, 2, 3]\n7" -> "1 2 3\n7"
        return input.Replace("\"", "").Replace("[", "").Replace("]", "");
    }
    
    public static string FormatExpectedOutputForJudge0(string expectedOutput)
    {
        // Expected output is stored as JSON string
        // Example: "[0, 1]" -> "[0, 1]"
        return expectedOutput;
    }
}
```

---

## Complete Implementation Example

### 1. Backend Service

```csharp
// Services/ICodeExecutionService.cs
public interface ICodeExecutionService
{
    Task<ExecutionResult> ExecuteCodeAsync(
        string sourceCode, 
        string language, 
        string stdin = null);
    
    Task<TestResult[]> RunTestCasesAsync(
        string sourceCode, 
        string language, 
        Guid questionId);
}

// Services/CodeExecutionService.cs
public class CodeExecutionService : ICodeExecutionService
{
    private readonly HttpClient _httpClient;
    private readonly string _judge0Url;
    private readonly IQuestionService _questionService;
    
    public CodeExecutionService(
        HttpClient httpClient, 
        IConfiguration configuration,
        IQuestionService questionService)
    {
        _httpClient = httpClient;
        _judge0Url = configuration["Judge0:BaseUrl"];
        _questionService = questionService;
    }
    
    public async Task<ExecutionResult> ExecuteCodeAsync(
        string sourceCode, 
        string language, 
        string stdin = null)
    {
        var languageId = GetLanguageId(language);
        
        var submission = new
        {
            source_code = sourceCode,
            language_id = languageId,
            stdin = stdin ?? "",
            cpu_time_limit = 5,
            memory_limit = 128000
        };
        
        var response = await _httpClient.PostAsync(
            $"{_judge0Url}/submissions?base64_encoded=false&wait=true",
            JsonContent.Create(submission)
        );
        
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<Judge0Result>();
        
        return MapToExecutionResult(result);
    }
    
    public async Task<TestResult[]> RunTestCasesAsync(
        string sourceCode, 
        string language, 
        Guid questionId)
    {
        var testCases = await _questionService.GetTestCasesAsync(questionId, includeHidden: true);
        var languageId = GetLanguageId(language);
        
        var tasks = testCases.Select(async testCase =>
        {
            var submission = new
            {
                source_code = sourceCode,
                language_id = languageId,
                stdin = testCase.Input,
                expected_output = testCase.ExpectedOutput,
                cpu_time_limit = 5,
                memory_limit = 128000
            };
            
            var response = await _httpClient.PostAsync(
                $"{_judge0Url}/submissions?base64_encoded=false&wait=true",
                JsonContent.Create(submission)
            );
            
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<Judge0Result>();
            
            return new TestResult
            {
                TestCaseId = testCase.Id,
                TestCaseNumber = testCase.TestCaseNumber,
                Passed = result.Status.Id == 3, // Accepted
                Output = result.Stdout,
                Error = result.Stderr ?? result.CompileOutput,
                Runtime = decimal.Parse(result.Time ?? "0"),
                Memory = result.Memory ?? 0,
                Status = GetStatusDescription(result.Status.Id)
            };
        });
        
        return await Task.WhenAll(tasks);
    }
    
    private int GetLanguageId(string language) => language.ToLower() switch
    {
        "javascript" => 63,
        "python" => 71,
        "java" => 62,
        "cpp" => 54,
        "csharp" => 51,
        "go" => 60,
        _ => throw new ArgumentException($"Unsupported language: {language}")
    };
    
    private ExecutionResult MapToExecutionResult(Judge0Result result)
    {
        return new ExecutionResult
        {
            Output = result.Stdout,
            Error = result.Stderr ?? result.CompileOutput,
            Status = GetStatusDescription(result.Status.Id),
            Runtime = decimal.Parse(result.Time ?? "0"),
            Memory = result.Memory ?? 0
        };
    }
    
    private string GetStatusDescription(int statusId) => statusId switch
    {
        3 => "Accepted",
        4 => "Wrong Answer",
        5 => "Time Limit Exceeded",
        6 => "Compilation Error",
        7 => "Runtime Error",
        _ => "Error"
    };
}
```

### 2. DTOs

```csharp
// DTOs/CodeExecution/ExecutionRequestDto.cs
public class ExecutionRequestDto
{
    public string SourceCode { get; set; }
    public string Language { get; set; }
    public string? Stdin { get; set; }
}

// DTOs/CodeExecution/ExecutionResultDto.cs
public class ExecutionResultDto
{
    public string Output { get; set; }
    public string? Error { get; set; }
    public string Status { get; set; }
    public decimal Runtime { get; set; }
    public long Memory { get; set; }
}

// DTOs/CodeExecution/TestResultDto.cs
public class TestResultDto
{
    public Guid TestCaseId { get; set; }
    public int TestCaseNumber { get; set; }
    public bool Passed { get; set; }
    public string? Output { get; set; }
    public string? Error { get; set; }
    public decimal Runtime { get; set; }
    public long Memory { get; set; }
    public string Status { get; set; }
}
```

### 3. Controller

```csharp
// Controllers/CodeExecutionController.cs
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CodeExecutionController : ControllerBase
{
    private readonly ICodeExecutionService _codeExecutionService;
    
    public CodeExecutionController(ICodeExecutionService codeExecutionService)
    {
        _codeExecutionService = codeExecutionService;
    }
    
    [HttpPost("execute")]
    public async Task<ActionResult<ExecutionResultDto>> ExecuteCode(
        [FromBody] ExecutionRequestDto request)
    {
        var result = await _codeExecutionService.ExecuteCodeAsync(
            request.SourceCode,
            request.Language,
            request.Stdin
        );
        
        return Ok(result);
    }
    
    [HttpPost("validate/{questionId}")]
    public async Task<ActionResult<TestResultDto[]>> ValidateSolution(
        Guid questionId,
        [FromBody] ExecutionRequestDto request)
    {
        var results = await _codeExecutionService.RunTestCasesAsync(
            request.SourceCode,
            request.Language,
            questionId
        );
        
        return Ok(results);
    }
}
```

### 4. Register Service in Program.cs

```csharp
// Program.cs
builder.Services.AddHttpClient<ICodeExecutionService, CodeExecutionService>(client =>
{
    var judge0Url = builder.Configuration["Judge0:BaseUrl"];
    client.BaseAddress = new Uri(judge0Url);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddScoped<ICodeExecutionService, CodeExecutionService>();
```

---

## Summary

### What You Need:
1. ✅ Docker Compose configuration for Judge0
2. ✅ Backend service (`CodeExecutionService`)
3. ✅ Controller endpoints
4. ✅ DTOs for requests/responses
5. ✅ Integration with existing `QuestionService` for test cases

### Data Format:
- **Submission**: JSON with `source_code`, `language_id`, `stdin`, `expected_output`
- **Response**: JSON with `stdout`, `stderr`, `status`, `time`, `memory`

### Test Cases:
- Stored in database as `QuestionTestCase` with `Input` and `ExpectedOutput`
- Converted to Judge0 format when executing
- Can run multiple test cases in parallel

### Stage 2 Timeline:
- **Week 2, Day 7-8**: Code Execution Service implementation
- This is the next major feature to implement after completing Week 1

