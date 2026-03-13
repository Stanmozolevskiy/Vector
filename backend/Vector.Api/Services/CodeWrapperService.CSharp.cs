using System.Text.Json;
using Vector.Api.Models;

namespace Vector.Api.Services;

public partial class CodeWrapperService
{
    private string WrapCSharpCode(string userCode, string testCaseInput, InterviewQuestion? question)
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

                string csValue;
                if (value.ValueKind == JsonValueKind.Array && ListNodeParamNames.IsListNodeParam(name))
                {
                    hasListNode = true;
                    csValue = BuildCSharpListNodeFromArray(value);
                }
                else if (value.ValueKind == JsonValueKind.Array)
                {
                    var items = value.EnumerateArray().Select(v => FormatJsonValue(v));
                    csValue = $"new int[] {{ {string.Join(", ", items)} }}";
                }
                else if (value.ValueKind == JsonValueKind.String)
                {
                    csValue = $"\"{value.GetString()?.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";
                }
                else
                {
                    csValue = FormatJsonValue(value);
                }

                paramDecls.Add($"var {name} = {csValue};");
                callArgs.Add(name);
            }

            var methodName   = ExtractCSharpMethodName(userCode) ?? "TwoSum";
            var solutionCall = $"new Solution().{methodName}({string.Join(", ", callArgs)})";
            var listHelper   = hasListNode ? GetCSharpListToArrayHelper() : string.Empty;
            // Mono (Judge0 C# runtime) does not ship System.Text.Json, so we emit
            // a manual type-dispatch block that JSON-encodes the result without
            // any external dependencies.
            const string csJsonHelper =
                "object __vec_out = (object)result;\n" +
                "            string __vec_json;\n" +
                "            if (__vec_out is string) __vec_json = \"\\\"\" + ((string)__vec_out).Replace(\"\\\\\", \"\\\\\\\\\").Replace(\"\\\"\", \"\\\\\\\"\") + \"\\\"\";\n" +
                "            else if (__vec_out is bool) __vec_json = (bool)__vec_out ? \"true\" : \"false\";\n" +
                "            else if (__vec_out is int[]) __vec_json = \"[\" + string.Join(\",\", (int[])__vec_out) + \"]\";\n" +
                "            else if (__vec_out is System.Collections.IList) { var __l = (System.Collections.IList)__vec_out; var __ls = new System.Collections.Generic.List<string>(); foreach (var __i in __l) __ls.Add(__i != null ? __i.ToString() : \"null\"); __vec_json = \"[\" + string.Join(\",\", __ls) + \"]\"; }\n" +
                "            else __vec_json = __vec_out != null ? __vec_out.ToString() : \"null\";\n" +
                "            Console.WriteLine(__vec_json);";

            var resultOutput = hasListNode
                ? "var __vec_arr = ListToArray(result);\n            Console.WriteLine(\"[\" + string.Join(\",\", __vec_arr) + \"]\");"
                : csJsonHelper;

            var mainBody =
                $"{string.Join("\n            ", paramDecls)}\n" +
                $"            var result = {solutionCall};\n" +
                $"            {resultOutput}";

            return $@"using System;
using System.Linq;
using System.Collections.Generic;
{userCode}
class Program
{{
{listHelper}
    static void Main()
    {{
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

    private static string BuildCSharpListNodeFromArray(JsonElement arr)
    {
        var items = arr.EnumerateArray().ToList();
        if (items.Count == 0) return "null";

        string inner = "null";
        for (int i = items.Count - 1; i >= 0; i--)
            inner = $"new ListNode({FormatJsonValue(items[i])}, {inner})";
        return inner;
    }

    private static string GetCSharpListToArrayHelper() =>
        @"    static int[] ListToArray(ListNode head) {
        var list = new List<int>();
        while (head != null) { list.Add(head.val); head = head.next; }
        return list.ToArray();
    }

";
}
