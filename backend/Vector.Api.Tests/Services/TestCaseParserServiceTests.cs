using Vector.Api.Services;
using Xunit;

namespace Vector.Api.Tests.Services;

public class TestCaseParserServiceTests
{
    private readonly TestCaseParserService _parser;

    public TestCaseParserServiceTests()
    {
        _parser = new TestCaseParserService();
    }

    [Fact]
    public void ParseTestCases_EmptyInput_ReturnsNoTestCasesError()
    {
        // Arrange
        var rawText = "";
        var parameterCount = 2;

        // Act
        var result = _parser.ParseTestCases(rawText, parameterCount);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotNull(result.Error);
        Assert.Equal("NO_TESTCASES", result.Error.Type);
    }

    [Fact]
    public void ParseTestCases_WhitespaceOnly_ReturnsNoTestCasesError()
    {
        // Arrange
        var rawText = "   \n  \t  \n  ";
        var parameterCount = 2;

        // Act
        var result = _parser.ParseTestCases(rawText, parameterCount);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotNull(result.Error);
        Assert.Equal("NO_TESTCASES", result.Error.Type);
    }

    [Fact]
    public void ParseTestCases_IncompleteCase_ReturnsIncompleteCaseError()
    {
        // Arrange
        var rawText = "[2,7,11,15]\n9\n[3,2,4]"; // 3 lines, but need 2 per case (incomplete)
        var parameterCount = 2;

        // Act
        var result = _parser.ParseTestCases(rawText, parameterCount);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotNull(result.Error);
        Assert.Equal("INCOMPLETE_CASE", result.Error.Type);
        Assert.Contains("Expected 2 lines per testcase", result.Error.Message);
    }

    [Fact]
    public void ParseTestCases_ValidTwoParameterCase_ReturnsValidResult()
    {
        // Arrange
        var rawText = "[2,7,11,15]\n9";
        var parameterCount = 2;
        var parameterNames = new[] { "nums", "target" };

        // Act
        var result = _parser.ParseTestCases(rawText, parameterCount, parameterNames);

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.Error);
        Assert.Single(result.TestCases);
        Assert.Equal(1, result.TestCases[0].CaseIndex);
        Assert.Equal(2, result.TestCases[0].InputValues.Length);
        Assert.Equal(parameterNames, result.TestCases[0].ParameterNames);
    }

    [Fact]
    public void ParseTestCases_MultipleCases_ReturnsAllCases()
    {
        // Arrange
        var rawText = "[2,7,11,15]\n9\n[3,2,4]\n6\n[3,3]\n6";
        var parameterCount = 2;

        // Act
        var result = _parser.ParseTestCases(rawText, parameterCount);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(3, result.TestCases.Count);
        Assert.Equal(1, result.TestCases[0].CaseIndex);
        Assert.Equal(2, result.TestCases[1].CaseIndex);
        Assert.Equal(3, result.TestCases[2].CaseIndex);
    }

    [Fact]
    public void ParseTestCases_IgnoresBlankLines()
    {
        // Arrange
        var rawText = "[2,7,11,15]\n\n9\n\n[3,2,4]\n\n6";
        var parameterCount = 2;

        // Act
        var result = _parser.ParseTestCases(rawText, parameterCount);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(2, result.TestCases.Count);
    }

    [Fact]
    public void ParseTestCases_JSONArrayInput_ParsesCorrectly()
    {
        // Arrange
        var rawText = "[2,7,11,15]\n9";
        var parameterCount = 2;

        // Act
        var result = _parser.ParseTestCases(rawText, parameterCount);

        // Assert
        Assert.True(result.IsValid);
        var firstInput = result.TestCases[0].InputValues[0];
        Assert.NotNull(firstInput);
        Assert.IsType<object[]>(firstInput);
    }

    [Fact]
    public void ParseTestCases_NumberInput_ParsesCorrectly()
    {
        // Arrange
        var rawText = "[2,7,11,15]\n9";
        var parameterCount = 2;

        // Act
        var result = _parser.ParseTestCases(rawText, parameterCount);

        // Assert
        Assert.True(result.IsValid);
        var secondInput = result.TestCases[0].InputValues[1];
        Assert.NotNull(secondInput);
        // Should be parsed as decimal (number)
        Assert.IsType<decimal>(secondInput);
        Assert.Equal(9m, secondInput);
    }

    [Fact]
    public void ParseTestCases_StringInput_ParsesAsRawString()
    {
        // Arrange
        var rawText = "hello\nworld";
        var parameterCount = 2;

        // Act
        var result = _parser.ParseTestCases(rawText, parameterCount);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal("hello", result.TestCases[0].InputValues[0]);
        Assert.Equal("world", result.TestCases[0].InputValues[1]);
    }

    [Fact]
    public void ParseTestCases_SingleParameterCase_ReturnsValidResult()
    {
        // Arrange
        var rawText = "abc\ndef\nghi";
        var parameterCount = 1;

        // Act
        var result = _parser.ParseTestCases(rawText, parameterCount);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(3, result.TestCases.Count);
        Assert.Single(result.TestCases[0].InputValues);
    }

    [Fact]
    public void ParseTestCases_ThreeParameterCase_ReturnsValidResult()
    {
        // Arrange
        var rawText = "1\n2\n3\n4\n5\n6";
        var parameterCount = 3;

        // Act
        var result = _parser.ParseTestCases(rawText, parameterCount);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(2, result.TestCases.Count);
        Assert.Equal(3, result.TestCases[0].InputValues.Length);
    }
}

