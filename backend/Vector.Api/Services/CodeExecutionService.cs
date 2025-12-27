using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Vector.Api.Data;
using Vector.Api.DTOs.CodeExecution;

namespace Vector.Api.Services;

/// <summary>
/// Service for executing code using Judge0 API
/// </summary>
public class CodeExecutionService : ICodeExecutionService
{
    private readonly HttpClient _httpClient;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CodeExecutionService> _logger;
    private readonly IConfiguration _configuration;
    private readonly Dictionary<string, int> _languageIdMap;
    private readonly CodeWrapperService _codeWrapper;
    private readonly TestCaseParserService _testCaseParser;

    public CodeExecutionService(
        IHttpClientFactory httpClientFactory,
        ApplicationDbContext context,
        ILogger<CodeExecutionService> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClientFactory.CreateClient(nameof(CodeExecutionService));
        _context = context;
        _logger = logger;
        _configuration = configuration;
        _codeWrapper = new CodeWrapperService();
        _testCaseParser = new TestCaseParserService();

        // Judge0 Official API Language ID mapping
        // Reference: https://ce.judge0.com/languages
        // Using Judge0 Official API (ce.judge0.com) - Free Tier
        // Language IDs for Judge0 Official API (may differ from self-hosted)
        _languageIdMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "python", 92 },      // Python 3.11.1 (Official API)
            { "python3", 92 },
            { "javascript", 93 },  // Node.js 18.15.0 (Official API)
            { "js", 93 },
            { "nodejs", 93 },
            { "java", 91 },        // Java 17.0.2 (Official API)
            { "cpp", 54 },         // C++ (GCC 9.2.0)
            { "c++", 54 },
            { "csharp", 51 },     // C# (Mono 6.6.0.161)
            { "c#", 51 },
            { "go", 60 },          // Go 1.19.5
            { "golang", 60 }
        };
    }

    /// <summary>
    /// Execute code with optional input
    /// </summary>
    public async Task<ExecutionResultDto> ExecuteCodeAsync(ExecutionRequestDto request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.SourceCode))
            {
                throw new ArgumentException("Source code cannot be empty.");
            }

            if (!_languageIdMap.TryGetValue(request.Language, out var languageId))
            {
                throw new ArgumentException($"Unsupported language: {request.Language}");
            }

            // Create Judge0 submission request
            // Judge0 API requires: source_code, language_id
            // Optional but recommended: stdin, cpu_time_limit, memory_limit
            var judge0Request = new
            {
                source_code = request.SourceCode,
                language_id = languageId,
                stdin = request.Stdin ?? string.Empty,
                cpu_time_limit = 5, // 5 seconds timeout
                memory_limit = 128 * 1024, // 128 MB memory limit (Judge0 expects KB)
                wall_time_limit = 10 // 10 seconds wall time limit
            };

            // Log request for debugging
            _logger.LogInformation("Submitting code to Judge0: LanguageId={LanguageId}, CodeLength={CodeLength}, BaseUrl={BaseUrl}", 
                languageId, request.SourceCode?.Length ?? 0, _httpClient.BaseAddress);

            // Serialize request manually to ensure correct format (snake_case for Judge0)
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = null // Preserve property names as-is (snake_case)
            };
            var jsonContent = JsonSerializer.Serialize(judge0Request, jsonOptions);
            _logger.LogInformation("Judge0 request JSON: {JsonContent}", jsonContent);
            
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            
            // Try synchronous execution first (wait=true)
            // If that fails, fall back to async with polling
            try
            {
                _logger.LogInformation("Attempting synchronous execution with wait=true");
                var submitResponse = await _httpClient.PostAsync("/submissions?base64_encoded=false&wait=true", content);
                
                if (submitResponse.IsSuccessStatusCode)
                {
                    var responseContent = await submitResponse.Content.ReadAsStringAsync();
                    _logger.LogInformation("Judge0 raw response: {ResponseContent}", responseContent);
                    
                    var result = await submitResponse.Content.ReadFromJsonAsync<Judge0ExecutionResult>();
                    if (result != null)
                    {
                        _logger.LogInformation("Synchronous execution response: Status={StatusId}, Description={Description}, Message={Message}, Stdout={Stdout}, Stderr={Stderr}", 
                            result.Status.Id, result.Status.Description, result.Message, result.Stdout, result.Stderr);
                        
                        // Return result regardless of status - Judge0 returns the final result even for errors
                        // Status 1 = In Queue, Status 2 = Processing (shouldn't happen with wait=true)
                        // Status 3+ = Final status (Accepted, Wrong Answer, Error, etc.)
                        if (result.Status.Id != 1 && result.Status.Id != 2)
                        {
                            _logger.LogInformation("Synchronous execution completed: Status={StatusId}, Stdout={Stdout}", result.Status.Id, result.Stdout);
                            return MapToExecutionResult(result);
                        }
                        // If still in queue/processing (unlikely with wait=true), fall through to async
                        _logger.LogWarning("Synchronous execution returned status {StatusId}, falling back to async", result.Status.Id);
                    }
                }
                else
                {
                    var errorContent = await submitResponse.Content.ReadAsStringAsync();
                    _logger.LogWarning("Synchronous execution failed with status {StatusCode}: {ErrorContent}", 
                        submitResponse.StatusCode, errorContent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Synchronous execution failed, falling back to async polling");
            }

            // Fall back to async submission with polling
            _logger.LogInformation("Using async submission with polling");
            var asyncSubmitResponse = await _httpClient.PostAsync("/submissions?base64_encoded=false&wait=false", content);
            
            if (!asyncSubmitResponse.IsSuccessStatusCode)
            {
                var errorContent = await asyncSubmitResponse.Content.ReadAsStringAsync();
                _logger.LogError("Judge0 API error: Status {StatusCode}, Response: {ErrorContent}", 
                    asyncSubmitResponse.StatusCode, errorContent);
                throw new HttpRequestException($"Judge0 API error: {asyncSubmitResponse.StatusCode}. {errorContent}");
            }

            var submitResult = await asyncSubmitResponse.Content.ReadFromJsonAsync<Judge0SubmissionResponse>();
            if (submitResult == null || string.IsNullOrEmpty(submitResult.Token))
            {
                throw new InvalidOperationException("Failed to submit code to Judge0.");
            }

            // Poll for result (with timeout)
            var token = submitResult.Token;
            _logger.LogInformation("Submitted code to Judge0, token: {Token}", token);
            var maxWaitTime = TimeSpan.FromSeconds(60);
            var pollInterval = TimeSpan.FromMilliseconds(1000);
            var elapsed = TimeSpan.Zero;
            var pollCount = 0;

            while (elapsed < maxWaitTime)
            {
                await Task.Delay(pollInterval);
                elapsed += pollInterval;
                pollCount++;

                var resultResponse = await _httpClient.GetAsync($"/submissions/{token}?base64_encoded=false");
                resultResponse.EnsureSuccessStatusCode();

                var resultContent = await resultResponse.Content.ReadAsStringAsync();
                _logger.LogInformation("Judge0 poll response for token {Token}, poll #{PollCount}: {ResponseContent}", token, pollCount, resultContent);
                
                var result = await resultResponse.Content.ReadFromJsonAsync<Judge0ExecutionResult>();
                if (result == null)
                {
                    _logger.LogWarning("Received null result from Judge0 for token {Token}, poll #{PollCount}", token, pollCount);
                    continue;
                }

                _logger.LogInformation("Judge0 status for token {Token}: StatusId={StatusId}, StatusDescription={StatusDescription}, Stdout={Stdout}, Stderr={Stderr}, Poll={PollCount}", 
                    token, result.Status.Id, result.Status?.Description ?? "Unknown", result.Stdout, result.Stderr, pollCount);

                // Status 1 = In Queue, Status 2 = Processing
                // Status 3 = Accepted, Status 4+ = Various errors
                if (result.Status.Id != 1 && result.Status.Id != 2)
                {
                    _logger.LogInformation("Code execution completed for token {Token}: Status={StatusId}, Poll={PollCount}", 
                        token, result.Status.Id, pollCount);
                    return MapToExecutionResult(result);
                }
            }
            
            _logger.LogWarning("Code execution timed out for token {Token} after {ElapsedSeconds} seconds, {PollCount} polls", 
                token, elapsed.TotalSeconds, pollCount);

            // Timeout - return timeout result
            return new ExecutionResultDto
            {
                Status = "Time Limit Exceeded",
                Output = string.Empty,
                Error = "Code execution timed out.",
                Runtime = 0,
                Memory = 0
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error communicating with Judge0 API");
            throw new InvalidOperationException("Code execution service is unavailable. Please try again later.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing code");
            throw;
        }
    }

    /// <summary>
    /// Run code against visible (non-hidden) test cases for a question (for "Run" button)
    /// </summary>
    public async Task<TestResultDto[]> RunCodeAsync(Guid questionId, ExecutionRequestDto request)
    {
        try
        {
            // Get question with test cases
            var question = await _context.InterviewQuestions
                .Include(q => q.TestCases)
                .FirstOrDefaultAsync(q => q.Id == questionId && q.IsActive);

            if (question == null)
            {
                throw new KeyNotFoundException("Question not found.");
            }

            // For Run: only use visible (non-hidden) test cases
            var testCases = question.TestCases
                .Where(tc => !tc.IsHidden)
                .OrderBy(tc => tc.TestCaseNumber)
                .ToList();

            if (testCases.Count == 0)
            {
                throw new InvalidOperationException("No visible test cases available for this question.");
            }

            var results = new List<TestResultDto>();

            // Execute code against each visible test case
            foreach (var testCase in testCases)
            {
                // Wrap user code to actually call the function with test case inputs
                var wrappedCode = _codeWrapper.WrapCodeForExecution(
                    request.SourceCode, 
                    request.Language, 
                    testCase.Input, 
                    question);

                _logger.LogInformation("Wrapped code for test case {TestCaseNumber}:\n{WrappedCode}", testCase.TestCaseNumber, wrappedCode);

                var executionRequest = new ExecutionRequestDto
                {
                    SourceCode = wrappedCode,
                    Language = request.Language,
                    Stdin = string.Empty // Input is now embedded in the wrapped code
                };

                var executionResult = await ExecuteCodeAsync(executionRequest);
                
                _logger.LogInformation("Execution result for test case {TestCaseNumber}: Status={Status}, Output={Output}, Error={Error}", 
                    testCase.TestCaseNumber, executionResult.Status, executionResult.Output, executionResult.Error);

                // Compare output with expected output (case-insensitive, trimmed)
                var actualOutput = executionResult.Output?.Trim() ?? string.Empty;
                var expectedOutput = testCase.ExpectedOutput?.Trim() ?? string.Empty;
                var passed = executionResult.Status == "Accepted" &&
                             string.Equals(actualOutput, expectedOutput, StringComparison.OrdinalIgnoreCase);

                var testResult = new TestResultDto
                {
                    TestCaseId = testCase.Id,
                    TestCaseNumber = testCase.TestCaseNumber,
                    Output = actualOutput,
                    ExpectedOutput = expectedOutput,
                    Input = testCase.Input,
                    Error = executionResult.Error,
                    Runtime = executionResult.Runtime,
                    Memory = executionResult.Memory,
                    Status = executionResult.Status,
                    Passed = passed
                };

                results.Add(testResult);
            }

            return results.ToArray();
        }
        catch (KeyNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running code for question {QuestionId}", questionId);
            throw;
        }
    }

    /// <summary>
    /// Validate code against all test cases for a question, including hidden ones (for "Submit" button)
    /// </summary>
    public async Task<TestResultDto[]> ValidateSolutionAsync(Guid questionId, ExecutionRequestDto request)
    {
        try
        {
            // Get question with test cases
            var question = await _context.InterviewQuestions
                .Include(q => q.TestCases)
                .FirstOrDefaultAsync(q => q.Id == questionId && q.IsActive);

            if (question == null)
            {
                throw new KeyNotFoundException("Question not found.");
            }

            // For Submit: use ALL test cases (including hidden)
            // For Run: only use non-hidden test cases (handled separately in controller)
            var testCases = question.TestCases
                .OrderBy(tc => tc.TestCaseNumber)
                .ToList();

            if (testCases.Count == 0)
            {
                throw new InvalidOperationException("No test cases available for this question.");
            }

            var results = new List<TestResultDto>();

            // Execute code against each test case
            foreach (var testCase in testCases)
            {
                // Wrap user code to actually call the function with test case inputs
                var wrappedCode = _codeWrapper.WrapCodeForExecution(
                    request.SourceCode, 
                    request.Language, 
                    testCase.Input, 
                    question);

                _logger.LogInformation("Wrapped code for test case {TestCaseNumber}:\n{WrappedCode}", testCase.TestCaseNumber, wrappedCode);

                var executionRequest = new ExecutionRequestDto
                {
                    SourceCode = wrappedCode,
                    Language = request.Language,
                    Stdin = string.Empty // Input is now embedded in the wrapped code
                };

                var executionResult = await ExecuteCodeAsync(executionRequest);
                
                _logger.LogInformation("Execution result for test case {TestCaseNumber}: Status={Status}, Output={Output}, Error={Error}", 
                    testCase.TestCaseNumber, executionResult.Status, executionResult.Output, executionResult.Error);

                // Separate stdout (all console.log/print) from actual output (last line = JSON.stringify(result))
                var fullStdout = executionResult.Output?.Trim() ?? string.Empty;
                string stdout = string.Empty;
                string? actualOutput = null;

                if (!string.IsNullOrWhiteSpace(fullStdout) && fullStdout != "(No output - code executed successfully)")
                {
                    // Split stdout into lines
                    var stdoutLines = fullStdout.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList();
                    
                    if (stdoutLines.Count > 0)
                    {
                        // Last line is the actual output (JSON.stringify(result))
                        var lastLine = stdoutLines.Last().Trim();
                        
                        // Try to parse last line as JSON (the actual return value)
                        try
                        {
                            var jsonDoc = JsonDocument.Parse(lastLine);
                            actualOutput = JsonSerializer.Serialize(ExtractJsonValue(jsonDoc.RootElement));
                            
                            // Everything before the last line is stdout (debug output)
                            if (stdoutLines.Count > 1)
                            {
                                stdout = string.Join("\n", stdoutLines.Take(stdoutLines.Count - 1));
                            }
                        }
                        catch
                        {
                            // If last line is not JSON, treat entire stdout as debug output
                            stdout = fullStdout;
                            actualOutput = null;
                        }
                    }
                }

                // Compare output with expected output (case-insensitive, trimmed)
                var expectedOutput = testCase.ExpectedOutput?.Trim() ?? string.Empty;
                var passed = executionResult.Status == "Accepted" &&
                             actualOutput != null &&
                             string.Equals(actualOutput.Trim(), expectedOutput, StringComparison.OrdinalIgnoreCase);

                var testResult = new TestResultDto
                {
                    TestCaseId = testCase.Id,
                    TestCaseNumber = testCase.TestCaseNumber,
                    Stdout = stdout, // Debug output (console.log/print before the result)
                    Output = actualOutput, // Actual return value (JSON parsed)
                    ExpectedOutput = expectedOutput, // Include expected for comparison display
                    Input = testCase.Input, // Include input for display
                    Error = executionResult.Error,
                    Runtime = executionResult.Runtime,
                    Memory = executionResult.Memory,
                    Status = executionResult.Status,
                    Passed = passed
                };

                results.Add(testResult);
            }

            return results.ToArray();
        }
        catch (KeyNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating solution for question {QuestionId}", questionId);
            throw;
        }
    }

    /// <summary>
    /// Run code with line-based testcase input (new UI format)
    /// </summary>
    public async Task<RunResultDto> RunCodeWithTestCasesAsync(Guid questionId, RunCodeWithTestCasesDto request)
    {
        try
        {
            // Get question
            var question = await _context.InterviewQuestions
                .Include(q => q.TestCases)
                .FirstOrDefaultAsync(q => q.Id == questionId && q.IsActive);

            if (question == null)
            {
                throw new KeyNotFoundException("Question not found.");
            }

            // Extract parameter names from user code
            var parameterNames = _codeWrapper.ExtractParameterNames(request.SourceCode, request.Language);
            var parameterCount = parameterNames.Length > 0 ? parameterNames.Length : 2; // Default to 2 for twoSum

            // Parse and validate testcases
            var parseResult = _testCaseParser.ParseTestCases(request.TestCaseText, parameterCount, parameterNames);

            if (!parseResult.IsValid)
            {
                return new RunResultDto
                {
                    Status = "INVALID_INPUT",
                    ValidationError = new TestCaseParseErrorDto
                    {
                        Type = parseResult.Error?.Type ?? "UNKNOWN",
                        Message = parseResult.Error?.Message ?? "Invalid testcase input",
                        LineNumber = parseResult.Error?.LineNumber
                    },
                    Cases = new List<CaseResultDto>()
                };
            }

            // Execute code against each parsed testcase
            var caseResults = new List<CaseResultDto>();
            decimal totalRuntime = 0;
            long maxMemory = 0;

            foreach (var parsedCase in parseResult.TestCases)
            {
                // Convert input values to JSON string for code wrapper
                var inputJson = new Dictionary<string, object>();
                for (int i = 0; i < parsedCase.ParameterNames.Length && i < parsedCase.InputValues.Length; i++)
                {
                    inputJson[parsedCase.ParameterNames[i]] = parsedCase.InputValues[i];
                }
                var testCaseInput = JsonSerializer.Serialize(inputJson);

                // Wrap code
                var wrappedCode = _codeWrapper.WrapCodeForExecution(
                    request.SourceCode,
                    request.Language,
                    testCaseInput,
                    question);

                // Execute
                var executionRequest = new ExecutionRequestDto
                {
                    SourceCode = wrappedCode,
                    Language = request.Language,
                    Stdin = string.Empty
                };

                var executionResult = await ExecuteCodeAsync(executionRequest);

                // Separate stdout (all console.log/print) from actual output (last line = JSON.stringify(result))
                var fullStdout = executionResult.Output?.Trim() ?? string.Empty;
                string stdout = string.Empty;
                string? actualOutput = null;

                if (!string.IsNullOrWhiteSpace(fullStdout) && fullStdout != "(No output - code executed successfully)")
                {
                    // Split stdout into lines
                    var stdoutLines = fullStdout.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList();
                    
                    if (stdoutLines.Count > 0)
                    {
                        // Last line is the actual output (JSON.stringify(result))
                        var lastLine = stdoutLines.Last().Trim();
                        
                        // Try to parse last line as JSON (the actual return value)
                        try
                        {
                            var jsonDoc = JsonDocument.Parse(lastLine);
                            actualOutput = JsonSerializer.Serialize(ExtractJsonValue(jsonDoc.RootElement));
                            
                            // Everything before the last line is stdout (debug output)
                            if (stdoutLines.Count > 1)
                            {
                                stdout = string.Join("\n", stdoutLines.Take(stdoutLines.Count - 1));
                            }
                        }
                        catch
                        {
                            // If last line is not JSON, treat entire stdout as debug output
                            stdout = fullStdout;
                            actualOutput = null;
                        }
                    }
                }

                // Try to match with question's test cases to get expected output
                string? expectedOutput = null;
                if (question?.TestCases != null && question.TestCases.Any())
                {
                    // Try to find matching test case by comparing input values
                    // This is a simple match - could be improved with better comparison logic
                    var matchingTestCase = question.TestCases
                        .Where(tc => !tc.IsHidden)
                        .OrderBy(tc => tc.TestCaseNumber)
                        .Skip(parsedCase.CaseIndex - 1)
                        .FirstOrDefault();
                    
                    if (matchingTestCase != null)
                    {
                        expectedOutput = matchingTestCase.ExpectedOutput?.Trim();
                    }
                }

                // Determine if passed (compare with expected if available)
                bool? passed = null;
                if (executionResult.Status == "Accepted")
                {
                    if (!string.IsNullOrWhiteSpace(expectedOutput) && actualOutput != null)
                    {
                        // Compare actual output with expected output
                        passed = string.Equals(actualOutput.Trim(), expectedOutput.Trim(), StringComparison.OrdinalIgnoreCase);
                    }
                    else if (!string.IsNullOrWhiteSpace(expectedOutput) && actualOutput == null)
                    {
                        // Expected output exists but actual output is null/undefined - failed
                        passed = false;
                    }
                    else if (actualOutput != null && string.IsNullOrWhiteSpace(expectedOutput))
                    {
                        // No expected output to compare, but we have actual output - assume passed if no error
                        passed = true;
                    }
                    else
                    {
                        // Both are null/empty - can't determine, mark as failed to be safe
                        passed = false;
                    }
                }
                else
                {
                    // Execution status is not "Accepted" - definitely failed
                    passed = false;
                }

                var caseResult = new CaseResultDto
                {
                    CaseIndex = parsedCase.CaseIndex,
                    InputValues = parsedCase.InputValues,
                    ParameterNames = parsedCase.ParameterNames,
                    Stdout = stdout, // Debug output (console.log/print before the result)
                    Output = actualOutput, // Actual return value (JSON parsed)
                    ExpectedOutput = expectedOutput, // Expected output from question test cases
                    Runtime = executionResult.Runtime,
                    Memory = executionResult.Memory,
                    Status = executionResult.Status,
                    Passed = passed,
                    Error = !string.IsNullOrWhiteSpace(executionResult.Error) ? new RuntimeErrorDto
                    {
                        Message = executionResult.Error,
                        Stack = null
                    } : null
                };

                caseResults.Add(caseResult);
                totalRuntime += executionResult.Runtime;
                maxMemory = Math.Max(maxMemory, executionResult.Memory);
            }

            // Determine overall status
            string overallStatus = "ACCEPTED";
            
            // Check for runtime errors first
            if (caseResults.Any(c => c.Error != null))
            {
                overallStatus = "RUNTIME_ERROR";
            }
            // Check if any test case failed (explicitly false or null when expected output exists)
            else if (caseResults.Any(c => c.Passed == false || (c.Passed == null && !string.IsNullOrWhiteSpace(c.ExpectedOutput))))
            {
                overallStatus = "WRONG_ANSWER";
            }
            // Only accept if ALL cases passed explicitly
            else if (caseResults.Count > 0 && caseResults.All(c => c.Passed == true))
            {
                overallStatus = "ACCEPTED";
            }
            // If no cases or all are null without expected output, default to wrong answer to be safe
            else if (caseResults.Count == 0 || caseResults.All(c => c.Passed == null))
            {
                overallStatus = "WRONG_ANSWER";
            }

            return new RunResultDto
            {
                Status = overallStatus,
                RuntimeMs = totalRuntime,
                MemoryMb = maxMemory / 1024.0m, // Convert KB to MB
                Cases = caseResults
            };
        }
        catch (KeyNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running code with testcases for question {QuestionId}", questionId);
            throw;
        }
    }

    private object? ExtractJsonValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.GetDecimal(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Array => element.EnumerateArray().Select(ExtractJsonValue).ToArray(),
            JsonValueKind.Object => element.EnumerateObject()
                .ToDictionary(p => p.Name, p => ExtractJsonValue(p.Value)),
            _ => element.GetRawText()
        };
    }

    /// <summary>
    /// Get list of supported programming languages
    /// Uses Judge0 Official API language IDs
    /// </summary>
    public async Task<List<SupportedLanguageDto>> GetSupportedLanguagesAsync()
    {
        var languages = new List<SupportedLanguageDto>
        {
            new() { Name = "Python 3", Value = "python", Judge0LanguageId = 92, Version = "3.11.1" },
            new() { Name = "JavaScript (Node.js)", Value = "javascript", Judge0LanguageId = 93, Version = "18.15.0" },
            new() { Name = "Java", Value = "java", Judge0LanguageId = 91, Version = "17.0.2" },
            new() { Name = "C++", Value = "cpp", Judge0LanguageId = 54, Version = "GCC 9.2.0" },
            new() { Name = "C#", Value = "csharp", Judge0LanguageId = 51, Version = ".NET 6.0.102" },
            new() { Name = "Go", Value = "go", Judge0LanguageId = 60, Version = "1.19.5" }
        };

        return await Task.FromResult(languages);
    }

    /// <summary>
    /// Map Judge0 execution result to ExecutionResultDto
    /// </summary>
    private ExecutionResultDto MapToExecutionResult(Judge0ExecutionResult result)
    {
        var status = result.Status.Id switch
        {
            3 => "Accepted",
            4 => "Wrong Answer",
            5 => "Time Limit Exceeded",
            6 => "Compilation Error",
            7 => "Runtime Error (SIGSEGV)",
            8 => "Runtime Error (SIGXFSZ)",
            9 => "Runtime Error (SIGFPE)",
            10 => "Runtime Error (SIGABRT)",
            11 => "Runtime Error (NZEC)",
            12 => "Runtime Error (Other)",
            13 => "Internal Error",
            14 => "Exec Format Error",
            _ => "Unknown Error"
        };

        // Judge0 returns time as string (seconds), convert to milliseconds
        decimal runtimeMs = 0;
        if (!string.IsNullOrEmpty(result.Time))
        {
            if (decimal.TryParse(result.Time, out var timeSeconds))
            {
                runtimeMs = timeSeconds * 1000; // Convert to milliseconds
            }
        }
        
        // Get stdout - this contains console.log/print output
        var output = result.Stdout ?? string.Empty;
        
        // Trim whitespace but keep the actual output
        output = output.Trim();
        
        // Only show "No output" message if there's truly no output AND no errors
        // Don't replace actual output with a message
        if (string.IsNullOrWhiteSpace(output) && result.Status.Id == 3 && string.IsNullOrWhiteSpace(result.Stderr))
        {
            output = "(No output - code executed successfully)";
        }
        
        _logger.LogInformation("MapToExecutionResult: StatusId={StatusId}, Stdout={Stdout}, Stderr={Stderr}, Output={Output}", 
            result.Status.Id, result.Stdout, result.Stderr, output);

        return new ExecutionResultDto
        {
            Status = status,
            Output = output,
            Error = result.Stderr ?? result.CompileOutput ?? result.Message ?? string.Empty,
            CompileOutput = result.CompileOutput,
            Runtime = runtimeMs,
            Memory = result.Memory ?? 0
        };
    }

    /// <summary>
    /// Judge0 submission response model
    /// </summary>
    private class Judge0SubmissionResponse
    {
        public string Token { get; set; } = string.Empty;
    }

    /// <summary>
    /// Judge0 execution result model
    /// Note: Judge0 Official API uses lowercase property names in JSON
    /// </summary>
    private class Judge0ExecutionResult
    {
        [System.Text.Json.Serialization.JsonPropertyName("status")]
        public Judge0Status Status { get; set; } = new();
        
        [System.Text.Json.Serialization.JsonPropertyName("stdout")]
        public string? Stdout { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("stderr")]
        public string? Stderr { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("compile_output")]
        public string? CompileOutput { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("message")]
        public string? Message { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("time")]
        public string? Time { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("memory")]
        public long? Memory { get; set; }
    }

    /// <summary>
    /// Judge0 status model
    /// </summary>
    private class Judge0Status
    {
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public int Id { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;
    }
}

