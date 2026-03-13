using System.Text.Json;
using Vector.Api.Models;

namespace Vector.Api.Services;

public partial class CodeWrapperService
{
    private string WrapPythonCode(string userCode, string testCaseInput, InterviewQuestion? question)
    {
        // Normalise line endings and replace tabs with 4 spaces to prevent
        // IndentationError caused by mixed whitespace.
        userCode = userCode
            .Replace("\r\n", "\n")
            .Replace("\r",   "\n")
            .Replace("\t",   "    ");

        try
        {
            var root = JsonDocument.Parse(testCaseInput).RootElement;

            var paramAssignments = new List<string>();
            var callArgs         = new List<string>();
            var hasListNode      = false;

            foreach (var prop in root.EnumerateObject())
            {
                var name  = prop.Name;
                var value = prop.Value;

                string pyValue;
                if (value.ValueKind == JsonValueKind.Array && ListNodeParamNames.IsListNodeParam(name))
                {
                    hasListNode = true;
                    pyValue = BuildPythonListNodeFromArray(value);
                }
                else
                {
                    pyValue = FormatJsonValueForPython(value);
                }

                paramAssignments.Add($"{name} = {pyValue}");
                callArgs.Add(name);
            }

            var funcName     = ExtractFunctionName(userCode) ?? (hasListNode ? "addTwoNumbers" : "twoSum");
            var listHelper   = hasListNode ? GetPythonListToArrayHelper() : string.Empty;
            var resultOutput = hasListNode
                ? "print(json.dumps(__vec_list_to_array(result)))"
                : "print(json.dumps(result))";

            return $@"import json
{userCode}
{listHelper}
# Test case execution
{string.Join("\n", paramAssignments)}
result = {funcName}({string.Join(", ", callArgs)})
{resultOutput}";
        }
        catch (JsonException)
        {
            return $@"{userCode}

# Simple stdin execution
input_data = '{testCaseInput}'
print(input_data)";
        }
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private static string BuildPythonListNodeFromArray(JsonElement arr)
    {
        var items = arr.EnumerateArray().ToList();
        if (items.Count == 0) return "None";

        string inner = "None";
        for (int i = items.Count - 1; i >= 0; i--)
            inner = $"ListNode({FormatJsonValueForPython(items[i])}, {inner})";
        return inner;
    }

    private static string GetPythonListToArrayHelper() => @"
def __vec_list_to_array(head):
    arr = []
    while head:
        arr.append(head.val)
        head = head.next
    return arr
";
}
