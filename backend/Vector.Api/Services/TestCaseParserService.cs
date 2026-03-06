using System.Text.Json;
using Vector.Api.Models;

namespace Vector.Api.Services;

/// <summary>
/// Service to parse and validate line-based testcase input
/// </summary>
public class TestCaseParserService
{
    /// <summary>
    /// Parse line-based testcase input into structured testcases
    /// </summary>
    public TestCaseParseResult ParseTestCases(string rawText, int parameterCount, string[]? parameterNames = null)
    {
        var result = new TestCaseParseResult
        {
            RawText = rawText,
            ParameterCount = parameterCount
        };

        if (string.IsNullOrWhiteSpace(rawText))
        {
            result.IsValid = false;
            result.Error = new TestCaseParseError
            {
                Type = "NO_TESTCASES",
                Message = "No testcases provided",
                LineNumber = null
            };
            return result;
        }

        // Split into lines and process
        var lines = rawText.Split('\n')
            .Select((line, index) => new { Line = line.TrimEnd(), Index = index + 1 })
            .ToList();

        // Filter non-empty lines (blank lines are ignored)
        var nonEmptyLines = lines
            .Where(l => !string.IsNullOrWhiteSpace(l.Line))
            .ToList();

        if (nonEmptyLines.Count == 0)
        {
            result.IsValid = false;
            result.Error = new TestCaseParseError
            {
                Type = "NO_TESTCASES",
                Message = "No testcases provided",
                LineNumber = null
            };
            return result;
        }

        // Validate: total lines must be divisible by parameter count
        if (nonEmptyLines.Count % parameterCount != 0)
        {
            var incompleteCaseStartLine = ((nonEmptyLines.Count / parameterCount) * parameterCount) + 1;
            result.IsValid = false;
            result.Error = new TestCaseParseError
            {
                Type = "INCOMPLETE_CASE",
                Message = $"Expected {parameterCount} lines per testcase. Found incomplete testcase at end (starting at line {incompleteCaseStartLine}).",
                LineNumber = incompleteCaseStartLine
            };
            return result;
        }

        // Parse each line
        var parsedValues = new List<object?>();
        var parseErrors = new List<TestCaseParseError>();

        foreach (var lineInfo in nonEmptyLines)
        {
            try
            {
                // Try JSON.parse first
                var jsonDoc = JsonDocument.Parse(lineInfo.Line);
                parsedValues.Add(ExtractJsonValue(jsonDoc.RootElement));
            }
            catch (JsonException)
            {
                // If JSON parse fails, treat as raw string
                parsedValues.Add(lineInfo.Line);
            }
            catch (Exception ex)
            {
                parseErrors.Add(new TestCaseParseError
                {
                    Type = "PARSE_ERROR",
                    Message = $"Invalid input on line {lineInfo.Index}: {ex.Message}",
                    LineNumber = lineInfo.Index
                });
            }
        }

        if (parseErrors.Any())
        {
            result.IsValid = false;
            result.Error = parseErrors.First();
            return result;
        }

        // Group into testcases
        var caseCount = nonEmptyLines.Count / parameterCount;
        var testCases = new List<ParsedTestCase>();

        for (int i = 0; i < caseCount; i++)
        {
            var startIndex = i * parameterCount;
            var inputs = parsedValues.Skip(startIndex).Take(parameterCount).ToArray();

            testCases.Add(new ParsedTestCase
            {
                CaseIndex = i + 1, // 1-based
                InputValues = inputs,
                ParameterNames = parameterNames ?? Enumerable.Range(0, parameterCount).Select(j => $"param{j + 1}").ToArray()
            });
        }

        result.IsValid = true;
        result.TestCases = testCases;
        return result;
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
}

public class TestCaseParseResult
{
    public bool IsValid { get; set; }
    public string RawText { get; set; } = string.Empty;
    public int ParameterCount { get; set; }
    public List<ParsedTestCase> TestCases { get; set; } = new();
    public TestCaseParseError? Error { get; set; }
}

public class ParsedTestCase
{
    public int CaseIndex { get; set; }
    public object?[] InputValues { get; set; } = Array.Empty<object>();
    public string[] ParameterNames { get; set; } = Array.Empty<string>();
}

public class TestCaseParseError
{
    public string Type { get; set; } = string.Empty; // INCOMPLETE_CASE, PARSE_ERROR, NO_TESTCASES
    public string Message { get; set; } = string.Empty;
    public int? LineNumber { get; set; }
}

