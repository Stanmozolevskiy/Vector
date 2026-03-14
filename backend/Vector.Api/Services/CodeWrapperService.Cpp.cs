using System.Text.Json;
using Vector.Api.Models;

namespace Vector.Api.Services;

public partial class CodeWrapperService
{
    private string WrapCppCode(string userCode, string testCaseInput, InterviewQuestion? question)
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

                string decl;
                if (value.ValueKind == JsonValueKind.Array && ListNodeParamNames.IsListNodeParam(name))
                {
                    hasListNode = true;
                    decl = $"ListNode* {name} = {BuildCppListNodeFromArray(value)};";
                }
                else if (value.ValueKind == JsonValueKind.Array)
                {
                    var items = value.EnumerateArray().Select(v => FormatJsonValue(v));
                    decl = $"vector<int> {name} = {{{string.Join(", ", items)}}};";
                }
                else if (value.ValueKind == JsonValueKind.String)
                {
                    var escaped = value.GetString()?.Replace("\\", "\\\\").Replace("\"", "\\\"");
                    decl = $"string {name} = \"{escaped}\";";
                }
                else
                {
                    decl = $"int {name} = {FormatJsonValue(value)};";
                }

                paramDecls.Add(decl);
                callArgs.Add(name);
            }

            var methodName   = ExtractCppMethodName(userCode) ?? (hasListNode ? "addTwoNumbers" : "twoSum");
            var returnType   = ExtractCppReturnType(userCode);
            // Use __vec_sol for the Solution object to avoid colliding with common parameter
            // names such as "s" (used in string problems like Longest Palindromic Substring).
            var solutionCall = $"__vec_sol.{methodName}({string.Join(", ", callArgs)})";
            var listHelper   = hasListNode ? GetCppListToArrayHelper() : string.Empty;

            // Build result output based on return type so the output matches JSON-serialised
            // expected values stored in the database (e.g. "bab" not bab for strings).
            // Normalize away std:: namespace qualifier so that "std::vector<int>"
            // is treated the same as "vector<int>", and "std::string" the same as
            // "string", regardless of how the user qualified the return type.
            string resultOutput;
            var rtLower = returnType.ToLower().TrimEnd('*', ' ');
            if (rtLower.StartsWith("std::")) rtLower = rtLower[5..];
            if (rtLower == "listnode")
            {
                resultOutput =
                    "vector<int> __vec_res = __vec_listToArray(result);\n" +
                    "    cout << \"[\";\n" +
                    "    for(size_t i=0;i<__vec_res.size();i++){cout<<__vec_res[i];if(i<__vec_res.size()-1)cout<<\",\";}\n" +
                    "    cout << \"]\";";
            }
            else if (rtLower.StartsWith("vector"))
            {
                // vector<int> → JSON array  [1,2,3]
                resultOutput =
                    "cout << \"[\";\n" +
                    "    for(size_t __i=0;__i<result.size();__i++){cout<<result[__i];if(__i<result.size()-1)cout<<\",\";}\n" +
                    "    cout << \"]\";";
            }
            else if (rtLower == "bool")
            {
                resultOutput = "cout << (result ? \"true\" : \"false\");";
            }
            else if (rtLower == "string")
            {
                // Output as JSON string: surround with double-quotes.
                // Handles both 'string' and 'std::string' return types.
                resultOutput = "cout << '\"' << result << '\"';";
            }
            else
            {
                // int, long, double, etc. — plain numeric output
                resultOutput = "cout << result;";
            }

            var mainBody =
                $"Solution __vec_sol;\n" +
                $"    {string.Join("\n    ", paramDecls)}\n" +
                $"    auto result = {solutionCall};\n" +
                $"    {resultOutput}";

            return $@"#include <iostream>
#include <vector>
#include <string>
#include <algorithm>
#include <unordered_map>
#include <unordered_set>
#include <climits>
using namespace std;
{userCode}
{listHelper}
int main() {{
    {mainBody}
    return 0;
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

    private static string BuildCppListNodeFromArray(JsonElement arr)
    {
        var items = arr.EnumerateArray().ToList();
        if (items.Count == 0) return "nullptr";

        string inner = "nullptr";
        for (int i = items.Count - 1; i >= 0; i--)
            inner = $"new ListNode({FormatJsonValue(items[i])}, {inner})";
        return inner;
    }

    private static string GetCppListToArrayHelper() => @"
vector<int> __vec_listToArray(ListNode* head) {
    vector<int> arr;
    while (head) { arr.push_back(head->val); head = head->next; }
    return arr;
}
";
}
