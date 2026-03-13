using System.Text.Json;

namespace Vector.Api.Services;

/// <summary>
/// Parameter names that indicate a linked-list (ListNode) type.
/// When a test-case JSON key matches one of these names, the array value is
/// converted into a ListNode chain instead of a plain array variable.
/// </summary>
internal static class ListNodeParamNames
{
    private static readonly HashSet<string> Names = new(StringComparer.OrdinalIgnoreCase)
        { "l1", "l2", "head", "list1", "list2", "list" };

    public static bool IsListNodeParam(string paramName) => Names.Contains(paramName);
}

public partial class CodeWrapperService
{
    /// <summary>
    /// Converts a <see cref="JsonElement"/> to its source-code literal in C-family
    /// languages (C++, Java, C#, JavaScript).
    /// </summary>
    private static string FormatJsonValue(JsonElement element) =>
        element.ValueKind switch
        {
            JsonValueKind.String => $"\"{element.GetString()}\"",
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True   => "true",
            JsonValueKind.False  => "false",
            JsonValueKind.Null   => "null",
            JsonValueKind.Array  => $"[{string.Join(", ", element.EnumerateArray().Select(FormatJsonValue))}]",
            JsonValueKind.Object => $"{{{string.Join(", ", element.EnumerateObject().Select(p => $"\"{p.Name}\": {FormatJsonValue(p.Value)}"))}}}",
            _                    => "null"
        };

    /// <summary>
    /// Converts a <see cref="JsonElement"/> to its Python literal equivalent
    /// (True / False / None instead of true / false / null).
    /// </summary>
    private static string FormatJsonValueForPython(JsonElement element) =>
        element.ValueKind switch
        {
            JsonValueKind.String => $"\"{element.GetString()}\"",
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True   => "True",
            JsonValueKind.False  => "False",
            JsonValueKind.Null   => "None",
            JsonValueKind.Array  => $"[{string.Join(", ", element.EnumerateArray().Select(FormatJsonValueForPython))}]",
            JsonValueKind.Object => $"{{{string.Join(", ", element.EnumerateObject().Select(p => $"\"{p.Name}\": {FormatJsonValueForPython(p.Value)}"))}}}",
            _                    => "None"
        };
}
