using System.Text.Json;
using Vector.Api.Models;

namespace Vector.Api.Services;

public partial class CodeWrapperService
{
    private string WrapJavaCode(string userCode, string testCaseInput, InterviewQuestion? question)
    {
        try
        {
            // Judge0 compiles everything as Main.java which allows exactly one public class.
            // Make ListNode and Solution package-private so the wrapper's `public class Main`
            // can be the sole public class in the file.
            userCode = userCode
                .Replace("public class ListNode ",  "class ListNode ")
                .Replace("public class ListNode{",  "class ListNode{")
                .Replace("public class Solution ",  "class Solution ")
                .Replace("public class Solution{",  "class Solution{");

            var root = JsonDocument.Parse(testCaseInput).RootElement;

            var paramDecls  = new List<string>();
            var callArgs    = new List<string>();
            var hasListNode = false;

            foreach (var prop in root.EnumerateObject())
            {
                var name  = prop.Name;
                var value = prop.Value;

                string javaValue;
                string paramType;

                if (value.ValueKind == JsonValueKind.Array && ListNodeParamNames.IsListNodeParam(name))
                {
                    hasListNode = true;
                    javaValue   = BuildJavaListNodeFromArray(value);
                    paramType   = "ListNode";
                }
                else if (value.ValueKind == JsonValueKind.Array)
                {
                    var items = value.EnumerateArray().Select(v => FormatJsonValue(v));
                    javaValue = $"new int[]{{{string.Join(", ", items)}}}";
                    paramType = "int[]";
                }
                else if (value.ValueKind == JsonValueKind.String)
                {
                    javaValue = $"\"{value.GetString()?.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";
                    paramType = "String";
                }
                else
                {
                    javaValue = FormatJsonValue(value);
                    paramType = "Object";
                }

                paramDecls.Add($"{paramType} {name} = {javaValue};");
                callArgs.Add(name);
            }

            var methodName   = ExtractJavaMethodName(userCode) ?? "addTwoNumbers";
            var solutionCall = $"new Solution().{methodName}({string.Join(", ", callArgs)})";
            var listHelper   = hasListNode ? GetJavaListToArrayMethod() : string.Empty;

            // Output helpers: strings must be JSON-quoted ("bab"), int-arrays formatted as
            // [1,2,3], booleans as true/false — all matching the JSON-encoded expected values.
            var resultVar = hasListNode
                ? $"int[] __vec_result_arr = __vec_listToArray({solutionCall});\n" +
                  "        System.out.println(java.util.Arrays.toString(__vec_result_arr).replace(\" \", \"\"));"
                : $"Object __vec_result = {solutionCall};\n" +
                  "        if (__vec_result instanceof String) {\n" +
                  "            System.out.println(\"\\\"\" + __vec_result + \"\\\"\");\n" +
                  "        } else if (__vec_result instanceof int[]) {\n" +
                  "            System.out.println(java.util.Arrays.toString((int[])__vec_result).replace(\" \", \"\"));\n" +
                  "        } else {\n" +
                  "            System.out.println(__vec_result);\n" +
                  "        }";

            var mainBody = $"        {string.Join("\n        ", paramDecls)}\n        {resultVar}";

            return $@"{userCode}

public class Main {{
{listHelper}
    public static void main(String[] args) {{
{mainBody}
    }}
}}";
        }
        catch (JsonException)
        {
            return userCode;
        }
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private static string BuildJavaListNodeFromArray(JsonElement arr)
    {
        var items = arr.EnumerateArray().ToList();
        if (items.Count == 0) return "null";

        string inner = "null";
        for (int i = items.Count - 1; i >= 0; i--)
            inner = $"new ListNode({FormatJsonValue(items[i])}, {inner})";
        return inner;
    }

    private static string GetJavaListToArrayMethod() =>
        @"    static int[] __vec_listToArray(ListNode head) {
        java.util.List<Integer> list = new java.util.ArrayList<>();
        while (head != null) {
            list.add(head.val);
            head = head.next;
        }
        return list.stream().mapToInt(i -> i).toArray();
    }
";
}
