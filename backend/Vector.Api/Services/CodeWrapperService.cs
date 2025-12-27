using System.Text.Json;
using Vector.Api.Models;

namespace Vector.Api.Services;

/// <summary>
/// Service to wrap user code with test case execution logic
/// </summary>
public class CodeWrapperService
{
    /// <summary>
    /// Wraps user code to execute with test case inputs
    /// </summary>
    public string WrapCodeForExecution(string userCode, string language, string testCaseInput, InterviewQuestion? question = null)
    {
        return language.ToLower() switch
        {
            "javascript" or "js" or "nodejs" => WrapJavaScriptCode(userCode, testCaseInput, question),
            "python" or "python3" => WrapPythonCode(userCode, testCaseInput, question),
            "java" => WrapJavaCode(userCode, testCaseInput, question),
            "cpp" or "c++" => WrapCppCode(userCode, testCaseInput, question),
            "csharp" or "c#" => WrapCSharpCode(userCode, testCaseInput, question),
            "go" or "golang" => WrapGoCode(userCode, testCaseInput, question),
            _ => throw new ArgumentException($"Unsupported language for code wrapping: {language}")
        };
    }

    private string WrapJavaScriptCode(string userCode, string testCaseInput, InterviewQuestion? question)
    {
        // Parse the JSON input to extract parameters
        // Input format: {"nums": [2,7,11,15], "target": 9}
        try
        {
            var inputJson = JsonDocument.Parse(testCaseInput);
            var root = inputJson.RootElement;
            
            // Build parameter extraction code
            var paramExtractions = new List<string>();
            var functionCall = new List<string>();
            
            foreach (var property in root.EnumerateObject())
            {
                var paramName = property.Name;
                var value = property.Value;
                
                // Convert JSON value to JavaScript
                string jsValue;
                if (value.ValueKind == JsonValueKind.Array)
                {
                    var arrayValues = value.EnumerateArray().Select(v => FormatJsonValue(v));
                    jsValue = $"[{string.Join(", ", arrayValues)}]";
                }
                else if (value.ValueKind == JsonValueKind.Object)
                {
                    var objProps = value.EnumerateObject().Select(p => $"\"{p.Name}\": {FormatJsonValue(p.Value)}");
                    jsValue = $"{{{string.Join(", ", objProps)}}}";
                }
                else
                {
                    jsValue = FormatJsonValue(value);
                }
                
                paramExtractions.Add($"const {paramName} = {jsValue};");
                functionCall.Add(paramName);
            }
            
            var functionName = ExtractFunctionName(userCode) ?? "twoSum"; // Default to twoSum for Two Sum problem
            
            // Build the wrapped code
            // Important: console.log output goes to stdout in Judge0
            var wrappedCode = $@"{userCode}

// Test case execution
{string.Join("\n", paramExtractions)}
const result = {functionName}({string.Join(", ", functionCall)});
// Output result as JSON string (Judge0 captures console.log to stdout)
console.log(JSON.stringify(result));";
            
            return wrappedCode;
        }
        catch (JsonException)
        {
            // If input is not JSON, try to use it as-is (for simple stdin cases)
            return $@"{userCode}

// Simple stdin execution
const input = `{testCaseInput}`;
console.log(input);";
        }
    }

    private string WrapPythonCode(string userCode, string testCaseInput, InterviewQuestion? question)
    {
        try
        {
            var inputJson = JsonDocument.Parse(testCaseInput);
            var root = inputJson.RootElement;
            
            var paramExtractions = new List<string>();
            var functionCall = new List<string>();
            
            foreach (var property in root.EnumerateObject())
            {
                var paramName = property.Name;
                var value = property.Value;
                
                string pyValue = FormatJsonValueForPython(value);
                paramExtractions.Add($"{paramName} = {pyValue}");
                functionCall.Add(paramName);
            }
            
            var functionName = ExtractFunctionName(userCode) ?? "twoSum";
            
            var wrappedCode = $@"import json
{userCode}

# Test case execution
{string.Join("\n", paramExtractions)}
result = {functionName}({string.Join(", ", functionCall)})
print(json.dumps(result))";
            
            return wrappedCode;
        }
        catch (JsonException)
        {
            return $@"{userCode}

# Simple stdin execution
input_data = '{testCaseInput}'
print(input_data)";
        }
    }

    private string WrapJavaCode(string userCode, string testCaseInput, InterviewQuestion? question)
    {
        // Java is more complex - would need to parse and create a main method
        // For now, return user code as-is and let stdin handle it
        return userCode;
    }

    private string WrapCppCode(string userCode, string testCaseInput, InterviewQuestion? question)
    {
        // C++ is complex - would need to parse and create a main function
        // For now, return user code as-is
        return userCode;
    }

    private string WrapCSharpCode(string userCode, string testCaseInput, InterviewQuestion? question)
    {
        // C# is complex - would need to parse and create a Main method
        // For now, return user code as-is
        return userCode;
    }

    private string WrapGoCode(string userCode, string testCaseInput, InterviewQuestion? question)
    {
        // Go is complex - would need to parse and create a main function
        // For now, return user code as-is
        return userCode;
    }

    private string FormatJsonValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => $"\"{element.GetString()}\"",
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => "null",
            JsonValueKind.Array => $"[{string.Join(", ", element.EnumerateArray().Select(FormatJsonValue))}]",
            JsonValueKind.Object => $"{{{string.Join(", ", element.EnumerateObject().Select(p => $"\"{p.Name}\": {FormatJsonValue(p.Value)}"))}}}",
            _ => "null"
        };
    }

    private string FormatJsonValueForPython(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => $"\"{element.GetString()}\"",
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => "True",
            JsonValueKind.False => "False",
            JsonValueKind.Null => "None",
            JsonValueKind.Array => $"[{string.Join(", ", element.EnumerateArray().Select(FormatJsonValueForPython))}]",
            JsonValueKind.Object => $"{{{string.Join(", ", element.EnumerateObject().Select(p => $"\"{p.Name}\": {FormatJsonValueForPython(p.Value)}"))}}}",
            _ => "None"
        };
    }

    private string? ExtractFunctionName(string code)
    {
        // Try to extract function name from common patterns
        // JavaScript: var twoSum = function(...) or function twoSum(...) or const twoSum = (...)
        var patterns = new[]
        {
            @"(?:var|let|const)\s+(\w+)\s*=\s*(?:function|\(|=>)",
            @"function\s+(\w+)\s*\(",
            @"def\s+(\w+)\s*\(",
            @"public\s+(?:static\s+)?\w+\s+(\w+)\s*\("
        };

        foreach (var pattern in patterns)
        {
            var match = System.Text.RegularExpressions.Regex.Match(code, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value;
            }
        }

        return null;
    }

    /// <summary>
    /// Extract parameter names from function signature
    /// </summary>
    public string[] ExtractParameterNames(string code, string language)
    {
        return language.ToLower() switch
        {
            "javascript" or "js" or "nodejs" => ExtractJavaScriptParameters(code),
            "python" or "python3" => ExtractPythonParameters(code),
            "java" => ExtractJavaParameters(code),
            "cpp" or "c++" => ExtractCppParameters(code),
            "csharp" or "c#" => ExtractCSharpParameters(code),
            "go" or "golang" => ExtractGoParameters(code),
            _ => Array.Empty<string>()
        };
    }

    private string[] ExtractJavaScriptParameters(string code)
    {
        // Match: function name(params) or var name = function(params) or const name = (params) =>
        var patterns = new[]
        {
            @"(?:var|let|const)\s+\w+\s*=\s*(?:function\s*)?\(([^)]*)\)",
            @"function\s+\w+\s*\(([^)]*)\)",
            @"\w+\s*=\s*\(([^)]*)\)\s*=>"
        };

        foreach (var pattern in patterns)
        {
            var match = System.Text.RegularExpressions.Regex.Match(code, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success && match.Groups.Count > 1)
            {
                var paramsStr = match.Groups[1].Value;
                return paramsStr.Split(',')
                    .Select(p => p.Trim())
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .ToArray();
            }
        }

        return Array.Empty<string>();
    }

    private string[] ExtractPythonParameters(string code)
    {
        // Match: def function_name(params):
        var match = System.Text.RegularExpressions.Regex.Match(code, @"def\s+\w+\s*\(([^)]*)\)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (match.Success && match.Groups.Count > 1)
        {
            var paramsStr = match.Groups[1].Value;
            return paramsStr.Split(',')
                .Select(p => p.Trim().Split('=').First().Trim()) // Remove default values
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToArray();
        }
        return Array.Empty<string>();
    }

    private string[] ExtractJavaParameters(string code)
    {
        // Match: public returnType methodName(params)
        var match = System.Text.RegularExpressions.Regex.Match(code, @"public\s+(?:static\s+)?\w+\s+\w+\s*\(([^)]*)\)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (match.Success && match.Groups.Count > 1)
        {
            var paramsStr = match.Groups[1].Value;
            return paramsStr.Split(',')
                .Select(p => p.Trim().Split().LastOrDefault()?.Trim() ?? p.Trim()) // Extract param name (last word)
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToArray();
        }
        return Array.Empty<string>();
    }

    private string[] ExtractCppParameters(string code)
    {
        // Match: returnType methodName(params)
        var match = System.Text.RegularExpressions.Regex.Match(code, @"\w+\s+\w+\s*\(([^)]*)\)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (match.Success && match.Groups.Count > 1)
        {
            var paramsStr = match.Groups[1].Value;
            return paramsStr.Split(',')
                .Select(p => p.Trim().Split().LastOrDefault()?.Trim() ?? p.Trim())
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToArray();
        }
        return Array.Empty<string>();
    }

    private string[] ExtractCSharpParameters(string code)
    {
        // Match: public returnType MethodName(params)
        var match = System.Text.RegularExpressions.Regex.Match(code, @"public\s+\w+\s+\w+\s*\(([^)]*)\)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (match.Success && match.Groups.Count > 1)
        {
            var paramsStr = match.Groups[1].Value;
            return paramsStr.Split(',')
                .Select(p => p.Trim().Split().LastOrDefault()?.Trim() ?? p.Trim())
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToArray();
        }
        return Array.Empty<string>();
    }

    private string[] ExtractGoParameters(string code)
    {
        // Match: func functionName(params) returnType
        var match = System.Text.RegularExpressions.Regex.Match(code, @"func\s+\w+\s*\(([^)]*)\)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (match.Success && match.Groups.Count > 1)
        {
            var paramsStr = match.Groups[1].Value;
            return paramsStr.Split(',')
                .Select(p => p.Trim().Split().LastOrDefault()?.Trim() ?? p.Trim())
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToArray();
        }
        return Array.Empty<string>();
    }
}

