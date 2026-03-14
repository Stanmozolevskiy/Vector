using System.Text.RegularExpressions;

namespace Vector.Api.Services;

public partial class CodeWrapperService
{
    // C++ keywords that can appear in function-call-like syntactic positions
    // (e.g. `else if (cond) {`) and must never be mistaken for method names.
    private static readonly HashSet<string> CppReservedKeywords = new(StringComparer.Ordinal)
    {
        "if", "else", "while", "for", "do", "switch", "case", "catch",
        "return", "new", "delete", "throw", "try", "static", "const",
        "virtual", "override", "sizeof", "decltype", "alignof", "noexcept"
    };

    // -------------------------------------------------------------------------
    // Generic function-name extraction (JavaScript / Python)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Extracts the solution function name from user code across multiple
    /// language patterns.  Dunder methods (__init__, __str__, …) are skipped.
    /// Returns <c>null</c> when no match is found.
    /// </summary>
    private static string? ExtractFunctionName(string code)
    {
        var patterns = new[]
        {
            // JS: var/let/const name = function(...) or arrow
            (@"(?:var|let|const)\s+(\w+)\s*=\s*(?:function|\(|=>)", false),
            // JS: function name(...)
            (@"function\s+(\w+)\s*\(", false),
            // Python: module-level def only (^def) to skip class methods
            (@"^def\s+(\w+)\s*\(", true),
            // Java / C#: public [static] ReturnType MethodName(  — [\w\[\]]+ handles int[], String[]
            (@"public\s+(?:static\s+)?[\w\[\]]+\s+(\w+)\s*\(", true),
        };

        foreach (var (pattern, skipDunder) in patterns)
        {
            var matches = Regex.Matches(code, pattern, RegexOptions.Multiline | RegexOptions.IgnoreCase);
            string? lastValid = null;
            foreach (Match m in matches)
            {
                if (!m.Success || m.Groups.Count <= 1) continue;
                var name = m.Groups[1].Value;
                if (skipDunder && name.StartsWith("__", StringComparison.Ordinal)
                               && name.EndsWith("__", StringComparison.Ordinal))
                    continue;
                lastValid = name;
            }
            if (lastValid != null) return lastValid;
        }
        return null;
    }

    // -------------------------------------------------------------------------
    // Method-name extractors (used internally by each language wrapper)
    // -------------------------------------------------------------------------

    /// <summary>Returns the first public method name found in Java source code.</summary>
    private static string? ExtractJavaMethodName(string code)
    {
        // [\w\[\]]+ matches both plain types (int, void) and array types (int[], String[])
        var m = Regex.Match(code,
            @"public\s+(?:static\s+)?[\w\[\]]+\s+(\w+)\s*\([^)]*\)",
            RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value : null;
    }

    /// <summary>
    /// Returns the solution method name from C++ source code.
    /// Linked-list methods (ListNode* return) are preferred; otherwise the
    /// last method with a body is used.  Constructor calls using <c>new</c>
    /// are excluded by requiring a <c>{</c> immediately after the closing paren.
    /// </summary>
    /// <summary>
    /// Extracts the return type of the primary solution method in C++ code.
    /// Returns the raw type token (e.g. "string", "int", "vector", "bool", "ListNode").
    /// </summary>
    private static string ExtractCppReturnType(string code)
    {
        // Priority: ListNode* return
        var ln = Regex.Match(code, @"\bListNode\s*\*\s*\w+\s*\(", RegexOptions.IgnoreCase);
        if (ln.Success) return "ListNode*";

        // Any method definition — capture "ReturnType MethodName("
        var matches = Regex.Matches(code,
            @"([\w:<>]+(?:\s*[*&<>\[\]]+)?)\s+(\w+)\s*\([^)]*\)\s*\{",
            RegexOptions.IgnoreCase);

        for (int i = matches.Count - 1; i >= 0; i--)
        {
            var name = matches[i].Groups[2].Value;
            if (name is "ListNode" or "Solution" or "main") continue;
            if (name.StartsWith("~", StringComparison.Ordinal)) continue;
            if (CppReservedKeywords.Contains(name)) continue;
            return matches[i].Groups[1].Value.Trim();
        }
        return "int";
    }

    private static string? ExtractCppMethodName(string code)
    {
        // Priority 1: methods returning a ListNode pointer (linked-list problems)
        var listNodeMatch = Regex.Match(code,
            @"\w+\s*\*\s*(\w+)\s*\([^)]*ListNode",
            RegexOptions.IgnoreCase);
        if (listNodeMatch.Success) return listNodeMatch.Groups[1].Value;

        // Priority 2: any method whose signature is followed by '{' (real definition).
        // C++ constructors with initialiser lists (ListNode(int x) : val(x) {}) are
        // excluded because )\s*{ does NOT immediately follow the closing paren.
        var matches = Regex.Matches(code,
            @"(?:[\w:<>]+(?:\s*[*&<>\[\]]+)?)\s+(\w+)\s*\([^)]*\)\s*\{",
            RegexOptions.IgnoreCase);

        for (int i = matches.Count - 1; i >= 0; i--)
        {
            var name = matches[i].Groups[1].Value;
            if (name is "ListNode" or "Solution" or "main") continue;
            if (name.StartsWith("~", StringComparison.Ordinal)) continue;
            if (CppReservedKeywords.Contains(name)) continue;
            return name;
        }
        return null;
    }

    /// <summary>
    /// Returns the solution method name from C# source code.
    /// Finds the last <c>public [static] ReturnType MethodName(</c> pattern,
    /// skipping class names and program entry-points.
    /// </summary>
    private static string? ExtractCSharpMethodName(string code)
    {
        var matches = Regex.Matches(code,
            @"public\s+(?:static\s+)?[\w\[\]?<>]+\s+(\w+)\s*\(",
            RegexOptions.IgnoreCase);

        for (int i = matches.Count - 1; i >= 0; i--)
        {
            var name = matches[i].Groups[1].Value;
            if (name is "Solution" or "ListNode" or "Main" or "Program") continue;
            return name;
        }
        return null;
    }

    // -------------------------------------------------------------------------
    // Parameter-name extractors (dispatched from ExtractParameterNames)
    // -------------------------------------------------------------------------

    private static string[] ExtractJavaScriptParameters(string code)
    {
        var patterns = new[]
        {
            @"(?:var|let|const)\s+\w+\s*=\s*(?:function\s*)?\(([^)]*)\)",
            @"function\s+\w+\s*\(([^)]*)\)",
            @"\w+\s*=\s*\(([^)]*)\)\s*=>"
        };

        foreach (var pattern in patterns)
        {
            var m = Regex.Match(code, pattern, RegexOptions.IgnoreCase);
            if (m.Success && m.Groups.Count > 1)
            {
                return m.Groups[1].Value
                    .Split(',')
                    .Select(p => p.Trim())
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .ToArray();
            }
        }
        return Array.Empty<string>();
    }

    private static string[] ExtractPythonParameters(string code)
    {
        // Only match module-level defs (^def) to exclude __init__ and other class methods.
        var matches = Regex.Matches(code,
            @"^def\s+(\w+)\s*\(([^)]*)\)",
            RegexOptions.Multiline | RegexOptions.IgnoreCase);

        if (matches.Count == 0) return Array.Empty<string>();

        var paramsStr = matches[matches.Count - 1].Groups[2].Value;
        return paramsStr.Split(',')
            .Select(p => p.Trim().Split('=').First().Trim()) // strip default values
            .Where(p => !string.IsNullOrWhiteSpace(p) && p != "self")
            .ToArray();
    }

    private static string[] ExtractJavaParameters(string code)
    {
        // [\w\[\]]+ handles plain (int) and array (int[]) return types
        var m = Regex.Match(code,
            @"public\s+(?:static\s+)?[\w\[\]]+\s+\w+\s*\(([^)]*)\)",
            RegexOptions.IgnoreCase);

        if (!m.Success || m.Groups.Count <= 1) return Array.Empty<string>();

        return m.Groups[1].Value
            .Split(',')
            .Select(p => p.Trim().Split().LastOrDefault()?.Trim() ?? p.Trim())
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToArray();
    }

    private static string[] ExtractCppParameters(string code)
    {
        // The negative lookahead (?!new\b) prevents matching constructor calls
        // like `new ListNode(sum % 10)` inside the solution body.  Without it,
        // `.Split().LastOrDefault()` on "sum % 10" would yield "10" as the
        // parameter name, producing broken output such as `vector<int> 10 = …`.
        //
        // [\w:<>]+ (instead of \w+) handles template return types such as
        // vector<int>, std::string, unordered_map<int,int>, etc. where the
        // original \w+ stopped at '<' and matched nothing, leaving parameterNames
        // empty and causing the Run path to build an empty JSON object {}.
        //
        // \s*\{ at the end (matching the opening brace of the body) ensures we
        // only match *definitions*, not *calls* such as `ListNode dummy(0);`.
        // Without this, a common Merge Two Sorted Lists pattern like
        // `ListNode dummy(0);` would match with method="dummy", params="0",
        // causing the JSON to be built as {"0": [1,2,4]} and the wrapper to
        // emit `vector<int> 0 = …` which is illegal C++.
        //
        // Two capturing groups are used so we can inspect both the method name
        // (group 1) and the parameter list (group 2).
        var matches = Regex.Matches(code,
            @"(?!new\b)(?:[\w:<>]+(?:\s*[*&<>\[\]]+)?)\s+(\w+)\s*\(([^)]*)\)\s*\{",
            RegexOptions.IgnoreCase);

        // Iterate from the end – the solution method appears last (after struct/class defs).
        for (int i = matches.Count - 1; i >= 0; i--)
        {
            var methodName = matches[i].Groups[1].Value;
            var paramsStr  = matches[i].Groups[2].Value;

            // Skip struct constructors, well-known non-solution names, and C++ keywords
            // that appear in control-flow like `else if (cond) {`.
            if (methodName is "ListNode" or "Solution" or "main") continue;
            if (CppReservedKeywords.Contains(methodName)) continue;

            return paramsStr
                .Split(',')
                .Select(p => p.Trim().Split().LastOrDefault()?.Trim().TrimEnd('*', '&') ?? p.Trim())
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToArray();
        }
        return Array.Empty<string>();
    }

    private static string[] ExtractCSharpParameters(string code)
    {
        // [\w\[\]?<>]+ handles plain (int, void), array (int[], string[]),
        // and generic (List<int>, Dictionary<int,int>) return types — same
        // character class used in ExtractCSharpMethodName.
        var matches = Regex.Matches(code,
            @"public\s+[\w\[\]?<>]+\s+(\w+)\s*\(([^)]*)\)",
            RegexOptions.IgnoreCase);

        for (int i = matches.Count - 1; i >= 0; i--)
        {
            var methodName = matches[i].Groups[1].Value;
            if (methodName.StartsWith("__", StringComparison.Ordinal)
             && methodName.EndsWith("__", StringComparison.Ordinal))
                continue;

            return matches[i].Groups[2].Value
                .Split(',')
                .Select(p => p.Trim().Split().LastOrDefault()?.Trim() ?? p.Trim())
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToArray();
        }
        return Array.Empty<string>();
    }

    private static string[] ExtractGoParameters(string code)
    {
        var m = Regex.Match(code,
            @"func\s+\w+\s*\(([^)]*)\)",
            RegexOptions.IgnoreCase);

        if (!m.Success || m.Groups.Count <= 1) return Array.Empty<string>();

        // Go parameter syntax is "name type" (e.g. "l1 *ListNode, target int"),
        // so we take the FIRST word of each parameter, not the last.
        return m.Groups[1].Value
            .Split(',')
            .Select(p => p.Trim().Split().FirstOrDefault()?.Trim() ?? p.Trim())
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToArray();
    }
}
