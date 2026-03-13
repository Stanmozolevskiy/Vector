using System.Text.Json;
using System.Text.RegularExpressions;
using Vector.Api.Models;

namespace Vector.Api.Services;

public partial class CodeWrapperService
{
    private string WrapGoCode(string userCode, string testCaseInput, InterviewQuestion? question)
    {
        try
        {
            var root = JsonDocument.Parse(testCaseInput).RootElement;

            var varDecls    = new List<string>();
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
                    var items = value.EnumerateArray().Select(v => FormatJsonValue(v));
                    decl = $"\t{name} := __vec_arrayToList([]int{{{string.Join(", ", items)}}})";
                }
                else if (value.ValueKind == JsonValueKind.Array)
                {
                    var items = value.EnumerateArray().Select(v => FormatJsonValue(v));
                    decl = $"\t{name} := []int{{{string.Join(", ", items)}}}";
                }
                else if (value.ValueKind == JsonValueKind.String)
                {
                    var escaped = value.GetString()?.Replace("\\", "\\\\").Replace("\"", "\\\"");
                    decl = $"\t{name} := \"{escaped}\"";
                }
                else
                {
                    decl = $"\t{name} := {FormatJsonValue(value)}";
                }

                varDecls.Add(decl);
                callArgs.Add(name);
            }

            // Extract the solution function name from the user's code.
            var funcMatch = Regex.Match(userCode, @"func\s+(\w+)\s*\(", RegexOptions.IgnoreCase);
            var funcName  = funcMatch.Success ? funcMatch.Groups[1].Value : "twoSum";

            // Detect whether the function returns *ListNode.
            var returnsListNode = Regex.IsMatch(userCode,
                @"func\s+\w+[^{]*\)\s*\*ListNode",
                RegexOptions.IgnoreCase);

            hasListNode = hasListNode || returnsListNode;

            var listHelpers  = hasListNode ? GetGoListHelpers() : string.Empty;
            var resultOutput = returnsListNode
                ? "\tarr := __vec_listToArray(result)\n\tb, _ := json.Marshal(arr)\n\tfmt.Println(string(b))"
                : "\tb, _ := json.Marshal(result)\n\tfmt.Println(string(b))";

            var mainBody =
                string.Join("\n", varDecls) + "\n" +
                $"\tresult := {funcName}({string.Join(", ", callArgs)})\n" +
                resultOutput;

            return $@"package main

import (
	""encoding/json""
	""fmt""
)

{userCode}
{listHelpers}
func main() {{
{mainBody}
}}";
        }
        catch (JsonException)
        {
            // Fallback: wrap with a minimal valid program so Judge0 can at least compile.
            return $@"package main

import ""fmt""

{userCode}

func main() {{
	fmt.Println(""no test case provided"")
}}";
        }
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    /// <summary>
    /// Go helper functions for converting between int slices and ListNode chains.
    /// The Go template uses capitalised field names (Val / Next) matching idiomatic Go.
    /// </summary>
    private static string GetGoListHelpers() => @"
func __vec_arrayToList(arr []int) *ListNode {
	if len(arr) == 0 {
		return nil
	}
	head := &ListNode{Val: arr[0]}
	curr := head
	for _, v := range arr[1:] {
		curr.Next = &ListNode{Val: v}
		curr = curr.Next
	}
	return head
}

func __vec_listToArray(head *ListNode) []int {
	var arr []int
	for head != nil {
		arr = append(arr, head.Val)
		head = head.Next
	}
	return arr
}
";
}
