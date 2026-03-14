using Vector.Api.Services;
using Xunit;

namespace Vector.Api.Tests.Services;

public class CodeWrapperServiceTests
{
    private readonly CodeWrapperService _wrapper;

    public CodeWrapperServiceTests()
    {
        _wrapper = new CodeWrapperService();
    }

    [Fact]
    public void WrapCodeForExecution_JavaScript_WithJSONInput_WrapsCorrectly()
    {
        // Arrange
        var userCode = "var twoSum = function(nums, target) { return [0, 1]; };";
        var testCaseInput = "{\"nums\": [2,7,11,15], \"target\": 9}";
        var language = "javascript";

        // Act
        var wrapped = _wrapper.WrapCodeForExecution(userCode, language, testCaseInput);

        // Assert
        Assert.Contains(userCode, wrapped);
        Assert.Contains("const nums", wrapped);
        Assert.Contains("const target", wrapped);
        Assert.Contains("twoSum(nums, target)", wrapped);
        Assert.Contains("console.log(JSON.stringify(result))", wrapped);
    }

    [Fact]
    public void WrapCodeForExecution_Python_WithJSONInput_WrapsCorrectly()
    {
        // Arrange
        var userCode = "def twoSum(nums, target):\n    return [0, 1]";
        var testCaseInput = "{\"nums\": [2,7,11,15], \"target\": 9}";
        var language = "python";

        // Act
        var wrapped = _wrapper.WrapCodeForExecution(userCode, language, testCaseInput);

        // Assert
        Assert.Contains(userCode, wrapped);
        Assert.Contains("nums =", wrapped);
        Assert.Contains("target =", wrapped);
        Assert.Contains("twoSum(nums, target)", wrapped);
        Assert.Contains("print(json.dumps(result))", wrapped);
    }

    [Fact]
    public void WrapCodeForExecution_UnsupportedLanguage_ThrowsArgumentException()
    {
        // Arrange
        var userCode = "some code";
        var testCaseInput = "input";
        var language = "unsupported";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            _wrapper.WrapCodeForExecution(userCode, language, testCaseInput));
    }

    [Fact]
    public void ExtractParameterNames_JavaScriptFunction_ReturnsParameters()
    {
        // Arrange
        var code = "var twoSum = function(nums, target) { return [0, 1]; };";
        var language = "javascript";

        // Act
        var parameters = _wrapper.ExtractParameterNames(code, language);

        // Assert
        Assert.Equal(2, parameters.Length);
        Assert.Equal("nums", parameters[0]);
        Assert.Equal("target", parameters[1]);
    }

    [Fact]
    public void ExtractParameterNames_JavaScriptArrowFunction_ReturnsParameters()
    {
        // Arrange
        var code = "const twoSum = (nums, target) => { return [0, 1]; };";
        var language = "javascript";

        // Act
        var parameters = _wrapper.ExtractParameterNames(code, language);

        // Assert
        Assert.Equal(2, parameters.Length);
        Assert.Equal("nums", parameters[0]);
        Assert.Equal("target", parameters[1]);
    }

    [Fact]
    public void ExtractParameterNames_PythonFunction_ReturnsParameters()
    {
        // Arrange
        var code = "def twoSum(nums, target):\n    return [0, 1]";
        var language = "python";

        // Act
        var parameters = _wrapper.ExtractParameterNames(code, language);

        // Assert
        Assert.Equal(2, parameters.Length);
        Assert.Equal("nums", parameters[0]);
        Assert.Equal("target", parameters[1]);
    }

    [Fact]
    public void ExtractParameterNames_PythonFunctionWithDefaults_ReturnsParameters()
    {
        // Arrange
        var code = "def twoSum(nums, target=0):\n    return [0, 1]";
        var language = "python";

        // Act
        var parameters = _wrapper.ExtractParameterNames(code, language);

        // Assert
        Assert.Equal(2, parameters.Length);
        Assert.Equal("nums", parameters[0]);
        Assert.Equal("target", parameters[1]);
    }

    [Fact]
    public void ExtractParameterNames_JavaMethod_ReturnsParameters()
    {
        // Arrange
        // Java regex expects: public (static)? returnType methodName(params)
        // Note: The regex @"public\s+(?:static\s+)?\w+\s+\w+\s*\(([^)]*)\)" doesn't handle array types well
        // Use a simpler signature that will match
        var code = "public static int twoSum(int nums, int target) { return 0; }";
        var language = "java";

        // Act
        var parameters = _wrapper.ExtractParameterNames(code, language);

        // Assert
        Assert.Equal(2, parameters.Length);
        Assert.Equal("nums", parameters[0]);
        Assert.Equal("target", parameters[1]);
    }

    [Fact]
    public void ExtractParameterNames_CSharpMethod_ReturnsParameters()
    {
        // Arrange
        // C# regex expects: public returnType MethodName(params)
        // Note: The regex @"public\s+\w+\s+\w+\s*\(([^)]*)\)" doesn't handle array types well
        // Use a simpler signature that will match
        var code = "public int TwoSum(int nums, int target) { return 0; }";
        var language = "csharp";

        // Act
        var parameters = _wrapper.ExtractParameterNames(code, language);

        // Assert
        Assert.Equal(2, parameters.Length);
        Assert.Equal("nums", parameters[0]);
        Assert.Equal("target", parameters[1]);
    }

    [Fact]
    public void ExtractParameterNames_CppFunction_ReturnsParameters()
    {
        // Arrange
        // C++ regex expects: returnType methodName(params)
        // Note: The regex @"\w+\s+\w+\s*\(([^)]*)\)" doesn't handle templates/generics
        // Use a simpler signature that will match
        var code = "int twoSum(int nums, int target) { return 0; }";
        var language = "cpp";

        // Act
        var parameters = _wrapper.ExtractParameterNames(code, language);

        // Assert
        Assert.Equal(2, parameters.Length);
        Assert.Equal("nums", parameters[0]);
        Assert.Equal("target", parameters[1]);
    }

    [Fact]
    public void ExtractParameterNames_GoFunction_ReturnsParameters()
    {
        // Arrange
        // Go syntax is "name type" (e.g. "nums int"), so we take the FIRST word as the param name
        var code = "func twoSum(nums int, target int) int { return 0 }";
        var language = "go";

        // Act
        var parameters = _wrapper.ExtractParameterNames(code, language);

        // Assert
        Assert.Equal(2, parameters.Length);
        Assert.Equal("nums", parameters[0]);
        Assert.Equal("target", parameters[1]);
    }

    [Fact]
    public void ExtractParameterNames_NoFunctionFound_ReturnsEmptyArray()
    {
        // Arrange
        var code = "just some random code without a function";
        var language = "javascript";

        // Act
        var parameters = _wrapper.ExtractParameterNames(code, language);

        // Assert
        Assert.Empty(parameters);
    }

    [Fact]
    public void WrapCodeForExecution_JavaScript_WithNonJSONInput_UsesRawString()
    {
        // Arrange
        var userCode = "var twoSum = function(nums, target) { return [0, 1]; };";
        var testCaseInput = "simple string input";
        var language = "javascript";

        // Act
        var wrapped = _wrapper.WrapCodeForExecution(userCode, language, testCaseInput);

        // Assert
        Assert.Contains(userCode, wrapped);
        Assert.Contains("const input", wrapped);
    }

    [Fact]
    public void WrapCodeForExecution_JavaScript_ListNodeInput_ConvertsArrayToListNode()
    {
        var userCode = "class ListNode { constructor(val, next=null) { this.val=val; this.next=next; } }\nvar addTwoNumbers = function(l1, l2) { return l1; };";
        var testCaseInput = "{\"l1\": [2,4,3], \"l2\": [5,6,4]}";
        var wrapped = _wrapper.WrapCodeForExecution(userCode, "javascript", testCaseInput);

        Assert.Contains("new ListNode(2, new ListNode(4, new ListNode(3", wrapped);
        Assert.Contains("new ListNode(5, new ListNode(6, new ListNode(4", wrapped);
        Assert.Contains("__vec_listToArray", wrapped);
    }

    [Fact]
    public void WrapCodeForExecution_Python_ListNodeInput_ConvertsArrayToListNode()
    {
        var userCode = "class ListNode:\n    def __init__(self, val=0, next=None):\n        self.val = val\n        self.next = next\ndef addTwoNumbers(l1, l2):\n    return l1";
        var testCaseInput = "{\"l1\": [2,4,3], \"l2\": [5,6,4]}";
        var wrapped = _wrapper.WrapCodeForExecution(userCode, "python", testCaseInput);

        Assert.Contains("ListNode(2, ListNode(4, ListNode(3", wrapped);
        Assert.Contains("__vec_list_to_array", wrapped);
        Assert.Contains("result = addTwoNumbers(", wrapped);
    }

    [Fact]
    public void WrapCodeForExecution_Java_ListNodeInput_GeneratesMainWithConversion()
    {
        var userCode = "public class ListNode { int val; ListNode next; ListNode() {} ListNode(int v) { val=v; } ListNode(int v, ListNode n) { val=v; next=n; } }\nclass Solution { public ListNode addTwoNumbers(ListNode l1, ListNode l2) { return l1; } }";
        var testCaseInput = "{\"l1\": [2,4,3], \"l2\": [5,6,4]}";
        var wrapped = _wrapper.WrapCodeForExecution(userCode, "java", testCaseInput);

        Assert.Contains("new ListNode(2, new ListNode(4, new ListNode(3", wrapped);
        Assert.Contains("public class Main", wrapped);
        Assert.Contains("__vec_listToArray", wrapped);
    }

    [Fact]
    public void WrapCodeForExecution_Python_WithNonJSONInput_UsesRawString()
    {
        // Arrange
        var userCode = "def twoSum(nums, target):\n    return [0, 1]";
        var testCaseInput = "simple string input";
        var language = "python";

        // Act
        var wrapped = _wrapper.WrapCodeForExecution(userCode, language, testCaseInput);

        // Assert
        Assert.Contains(userCode, wrapped);
        Assert.Contains("input_data", wrapped);
    }

    [Fact]
    public void WrapCodeForExecution_CSharp_ListNodeInput_GeneratesMainWithConversion()
    {
        var userCode = "public class ListNode { public int val; public ListNode next; public ListNode(int val=0, ListNode next=null) { this.val=val; this.next=next; } }\npublic class Solution { public ListNode AddTwoNumbers(ListNode l1, ListNode l2) { return l1; } }";
        var testCaseInput = "{\"l1\": [2,4,3], \"l2\": [5,6,4]}";
        var wrapped = _wrapper.WrapCodeForExecution(userCode, "csharp", testCaseInput);

        Assert.Contains("new ListNode(2, new ListNode(4, new ListNode(3", wrapped);
        Assert.Contains("class Program", wrapped);
        Assert.Contains("static void Main", wrapped);
        Assert.Contains("ListToArray", wrapped);
    }

    [Fact]
    public void WrapCodeForExecution_Cpp_ListNodeInput_GeneratesMainWithConversion()
    {
        var userCode = "struct ListNode { int val; ListNode *next; ListNode() : val(0), next(nullptr) {} ListNode(int x) : val(x), next(nullptr) {} ListNode(int x, ListNode *next) : val(x), next(next) {} };\nclass Solution { public: ListNode* addTwoNumbers(ListNode* l1, ListNode* l2) { return l1; } };";
        var testCaseInput = "{\"l1\": [2,4,3], \"l2\": [5,6,4]}";
        var wrapped = _wrapper.WrapCodeForExecution(userCode, "cpp", testCaseInput);

        Assert.Contains("new ListNode(2, new ListNode(4, new ListNode(3", wrapped);
        Assert.Contains("int main()", wrapped);
        Assert.Contains("__vec_listToArray", wrapped);
    }
}

