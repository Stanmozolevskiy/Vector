# Code Execution Service Research

## Overview
This document outlines research on how to run code and provide test cases for different programming languages in the Vector platform.

## Problem Statement
We need to:
1. Execute user-submitted code in multiple languages (JavaScript, Python, Java, C++, C#, Go)
2. Run test cases against the submitted code
3. Return execution results (output, errors, runtime, memory usage)
4. Ensure security and isolation for code execution

## Solution Options

### Option 1: Judge0 API (Recommended)
**Description**: Open-source online code execution system that supports 60+ programming languages.

**Pros**:
- Supports 60+ languages including all our target languages
- RESTful API - easy to integrate
- Docker-based deployment
- Built-in test case execution
- Returns execution results (stdout, stderr, time, memory)
- Free and open-source
- Active community and documentation

**Cons**:
- Requires self-hosting or using their cloud service (paid)
- Need to set up and maintain Docker containers
- Resource management needed for concurrent executions

**Setup**:
```bash
# Docker Compose setup
docker-compose up -d judge0
```

**API Example**:
```bash
POST /submissions
{
  "source_code": "print('Hello World')",
  "language_id": 71,  # Python
  "stdin": "test input",
  "expected_output": "Hello World"
}
```

**Documentation**: https://judge0.com/

---

### Option 2: Piston API
**Description**: High-performance code execution engine written in Rust, supports 50+ languages.

**Pros**:
- Very fast execution (Rust-based)
- Supports 50+ languages
- Simple REST API
- Docker-based
- Good for high-concurrency scenarios
- Free and open-source

**Cons**:
- Less mature than Judge0
- Smaller community
- Need to handle test cases manually

**Setup**:
```bash
docker run -d -p 2000:2000 ghcr.io/engineer-man/piston
```

**API Example**:
```bash
POST /api/v2/execute
{
  "language": "python",
  "version": "3.10.0",
  "files": [{
    "content": "print('Hello World')"
  }],
  "stdin": "test input"
}
```

**Documentation**: https://github.com/engineer-man/piston

---

### Option 3: Custom Docker-Based Solution
**Description**: Build our own code execution service using Docker containers.

**Pros**:
- Full control over execution environment
- Can customize security policies
- No external dependencies
- Can optimize for our specific use case

**Cons**:
- Significant development effort
- Need to handle language-specific compilation/execution
- Security hardening required
- Resource management complexity
- Maintenance burden

**Architecture**:
```
User Code → API → Docker Container (per language) → Execute → Return Results
```

**Implementation Considerations**:
- Use Docker with resource limits (CPU, memory, time)
- Implement timeout mechanisms
- Sandbox file system access
- Network isolation
- Language-specific execution scripts

---

### Option 4: Cloud-Based Services

#### AWS Lambda / Google Cloud Functions
**Pros**:
- Serverless, scalable
- Pay-per-execution
- Managed service

**Cons**:
- Limited language support
- Cold start latency
- Cost at scale
- Less control

#### CodeSandbox API / Replit API
**Pros**:
- Managed service
- Good language support
- Easy integration

**Cons**:
- Paid service
- External dependency
- Less control over execution environment

---

## Recommended Approach: Judge0

### Why Judge0?
1. **Comprehensive Language Support**: Supports all our target languages
2. **Test Case Execution**: Built-in support for running test cases
3. **Production Ready**: Used by many coding platforms
4. **Docker-Based**: Easy to integrate with our existing Docker setup
5. **Open Source**: Free to use and customize

### Integration Plan

#### 1. Backend Service
```csharp
// CodeExecutionService.cs
public class CodeExecutionService
{
    private readonly HttpClient _httpClient;
    private readonly string _judge0Url;
    
    public async Task<ExecutionResult> ExecuteCode(
        string sourceCode, 
        string language, 
        string stdin = null,
        string expectedOutput = null)
    {
        var languageId = GetLanguageId(language);
        var submission = new
        {
            source_code = sourceCode,
            language_id = languageId,
            stdin = stdin,
            expected_output = expectedOutput
        };
        
        // Submit code
        var response = await _httpClient.PostAsync(
            $"{_judge0Url}/submissions", 
            JsonContent.Create(submission)
        );
        
        // Get results
        var result = await GetSubmissionResult(submissionId);
        return MapToExecutionResult(result);
    }
}
```

#### 2. Language Mapping
```csharp
private int GetLanguageId(string language) => language.ToLower() switch
{
    "javascript" => 63,  // Node.js
    "python" => 71,      // Python 3
    "java" => 62,        // Java
    "cpp" => 54,         // C++17
    "csharp" => 51,      // C#
    "go" => 60,          // Go
    _ => throw new ArgumentException($"Unsupported language: {language}")
};
```

#### 3. Test Case Execution
```csharp
public async Task<TestResult[]> RunTestCases(
    string sourceCode, 
    string language, 
    QuestionTestCase[] testCases)
{
    var results = new List<TestResult>();
    
    foreach (var testCase in testCases)
    {
        var execution = await ExecuteCode(
            sourceCode, 
            language, 
            testCase.Input, 
            testCase.ExpectedOutput
        );
        
        results.Add(new TestResult
        {
            TestCaseId = testCase.Id,
            Passed = execution.Status == "Accepted",
            Output = execution.Stdout,
            Error = execution.Stderr,
            Runtime = execution.Time,
            Memory = execution.Memory
        });
    }
    
    return results.ToArray();
}
```

#### 4. Docker Compose Integration
```yaml
# docker-compose.yml
services:
  judge0:
    image: judge0/judge0:1.13.0
    ports:
      - "2358:2358"
    environment:
      - REDIS_HOST=redis
      - POSTGRES_HOST=postgres
      - POSTGRES_DB=judge0
      - POSTGRES_USER=judge0
      - POSTGRES_PASSWORD=judge0
    depends_on:
      - redis
      - postgres
```

### Security Considerations

1. **Resource Limits**:
   - CPU time limit (e.g., 5 seconds)
   - Memory limit (e.g., 128MB)
   - File size limits

2. **Isolation**:
   - Each execution in separate container
   - Network isolation
   - File system sandboxing

3. **Input Validation**:
   - Sanitize user code
   - Validate test case inputs
   - Prevent code injection

4. **Rate Limiting**:
   - Limit submissions per user
   - Prevent abuse

### Test Case Format

```json
{
  "testCases": [
    {
      "id": 1,
      "input": "[1, 2, 3]",
      "expectedOutput": "[1, 2, 3]",
      "isHidden": false
    },
    {
      "id": 2,
      "input": "[4, 5, 6]",
      "expectedOutput": "[4, 5, 6]",
      "isHidden": true
    }
  ]
}
```

### Execution Flow

```
1. User submits code → Frontend
2. Frontend sends to Backend API
3. Backend calls Judge0 API
4. Judge0 executes in Docker container
5. Judge0 returns results
6. Backend processes and formats results
7. Frontend displays results to user
```

## Alternative: Hybrid Approach

Use Judge0 for most languages, but implement custom execution for:
- **JavaScript**: Use Node.js directly (faster, no container overhead)
- **Python**: Use Python subprocess with sandboxing

This reduces dependency on Judge0 for common languages while maintaining support for all languages.

## Next Steps

1. **Phase 1**: Set up Judge0 in Docker
2. **Phase 2**: Create CodeExecutionService in backend
3. **Phase 3**: Integrate with QuestionService
4. **Phase 4**: Add frontend UI for code execution
5. **Phase 5**: Add test case execution and results display
6. **Phase 6**: Add security hardening and rate limiting

## Resources

- Judge0 Documentation: https://judge0.com/
- Judge0 GitHub: https://github.com/judge0/judge0
- Piston API: https://github.com/engineer-man/piston
- Docker Security Best Practices: https://docs.docker.com/engine/security/

