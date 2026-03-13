using System.Text.Json;
using Vector.Api.Models;

namespace Vector.Api.Services;

public partial class CodeWrapperService
{
    private string WrapJavaScriptCode(string userCode, string testCaseInput, InterviewQuestion? question)
    {
        try
        {
            var root = JsonDocument.Parse(testCaseInput).RootElement;

            var paramDecls  = new List<string>();
            var callArgs    = new List<string>();
            var hasListNode = false;

            foreach (var prop in root.EnumerateObject())
            {
                var name  = prop.Name;
                var value = prop.Value;

                string jsValue;
                if (value.ValueKind == JsonValueKind.Array && ListNodeParamNames.IsListNodeParam(name))
                {
                    hasListNode = true;
                    jsValue = BuildJsListNodeFromArray(value);
                }
                else if (value.ValueKind == JsonValueKind.Array)
                {
                    var items = value.EnumerateArray().Select(v => FormatJsonValue(v));
                    jsValue = $"[{string.Join(", ", items)}]";
                }
                else if (value.ValueKind == JsonValueKind.Object)
                {
                    var pairs = value.EnumerateObject()
                        .Select(p => $"\"{p.Name}\": {FormatJsonValue(p.Value)}");
                    jsValue = $"{{{string.Join(", ", pairs)}}}";
                }
                else
                {
                    jsValue = FormatJsonValue(value);
                }

                paramDecls.Add($"const {name} = {jsValue};");
                callArgs.Add(name);
            }

            var funcName     = ExtractFunctionName(userCode) ?? "twoSum";
            var listHelper   = hasListNode ? GetJsListToArrayHelper() : string.Empty;
            var resultOutput = hasListNode
                ? "console.log(JSON.stringify(__vec_listToArray(result)));"
                : "console.log(JSON.stringify(result));";

            return $@"{userCode}
{listHelper}
// Test case execution
{string.Join("\n", paramDecls)}
const result = {funcName}({string.Join(", ", callArgs)});
{resultOutput}";
        }
        catch (JsonException)
        {
            return $@"{userCode}

// Simple stdin execution
const input = `{testCaseInput}`;
console.log(input);";
        }
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    /// <summary>Builds a nested <c>new ListNode(v, new ListNode(…))</c> expression from a JSON array.</summary>
    private static string BuildJsListNodeFromArray(JsonElement arr)
    {
        var items = arr.EnumerateArray().ToList();
        if (items.Count == 0) return "null";

        string inner = "null";
        for (int i = items.Count - 1; i >= 0; i--)
            inner = $"new ListNode({FormatJsonValue(items[i])}, {inner})";
        return inner;
    }

    private static string GetJsListToArrayHelper() => @"
function __vec_listToArray(head) {
  const arr = [];
  while (head) { arr.push(head.val); head = head.next; }
  return arr;
}
";
}
