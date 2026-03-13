using Vector.Api.Models;

namespace Vector.Api.Services;

/// <summary>
/// Wraps user-submitted code with test-case execution boilerplate so it can be
/// sent to the Judge0 execution engine.
///
/// The class is split across several partial files to keep each language's logic
/// self-contained and easy to maintain:
///   CodeWrapperService.Helpers.cs          – shared JSON helpers and ListNodeParamNames
///   CodeWrapperService.ParameterExtraction.cs – function/parameter name extractors
///   CodeWrapperService.JavaScript.cs       – JavaScript wrapping
///   CodeWrapperService.Python.cs           – Python wrapping
///   CodeWrapperService.Java.cs             – Java wrapping
///   CodeWrapperService.Cpp.cs              – C++ wrapping
///   CodeWrapperService.CSharp.cs           – C# wrapping
///   CodeWrapperService.Go.cs               – Go wrapping
///   CodeWrapperService.Sql.cs              – SQL wrapping
/// </summary>
public partial class CodeWrapperService
{
    /// <summary>
    /// Wraps <paramref name="userCode"/> with execution boilerplate for the given
    /// <paramref name="language"/> and <paramref name="testCaseInput"/> (JSON object).
    /// </summary>
    public string WrapCodeForExecution(
        string userCode,
        string language,
        string testCaseInput,
        InterviewQuestion? question = null)
    {
        return language.ToLower() switch
        {
            "javascript" or "js" or "nodejs" => WrapJavaScriptCode(userCode, testCaseInput, question),
            "python"     or "python3"         => WrapPythonCode(userCode, testCaseInput, question),
            "java"                            => WrapJavaCode(userCode, testCaseInput, question),
            "cpp"        or "c++"             => WrapCppCode(userCode, testCaseInput, question),
            "csharp"     or "c#"              => WrapCSharpCode(userCode, testCaseInput, question),
            "go"         or "golang"          => WrapGoCode(userCode, testCaseInput, question),
            "sql"        or "sqlite"          => WrapSqlCode(userCode, testCaseInput, question),
            _ => throw new ArgumentException($"Unsupported language for code wrapping: {language}")
        };
    }

    /// <summary>
    /// Extracts the parameter names from the solution function signature found in
    /// <paramref name="code"/> for the given <paramref name="language"/>.
    /// Returns an empty array when extraction fails or the language is SQL.
    /// </summary>
    public string[] ExtractParameterNames(string code, string language)
    {
        return language.ToLower() switch
        {
            "javascript" or "js" or "nodejs" => ExtractJavaScriptParameters(code),
            "python"     or "python3"         => ExtractPythonParameters(code),
            "java"                            => ExtractJavaParameters(code),
            "cpp"        or "c++"             => ExtractCppParameters(code),
            "csharp"     or "c#"              => ExtractCSharpParameters(code),
            "go"         or "golang"          => ExtractGoParameters(code),
            "sql"        or "sqlite"          => Array.Empty<string>(),
            _                                 => Array.Empty<string>()
        };
    }
}
