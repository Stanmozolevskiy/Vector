using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vector.Api.Controllers;
using Vector.Api.DTOs.CodeExecution;
using Vector.Api.Services;
using Xunit;

namespace Vector.Api.Tests.Controllers;

public class CodeExecutionControllerTests
{
    private readonly Mock<ICodeExecutionService> _mockService;
    private readonly Mock<ILogger<CodeExecutionController>> _mockLogger;
    private readonly CodeExecutionController _controller;

    public CodeExecutionControllerTests()
    {
        _mockService = new Mock<ICodeExecutionService>();
        _mockLogger = new Mock<ILogger<CodeExecutionController>>();
        _controller = new CodeExecutionController(_mockService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task ExecuteCode_ValidRequest_ReturnsOk()
    {
        // Arrange
        var request = new ExecutionRequestDto
        {
            SourceCode = "console.log('hello');",
            Language = "javascript"
        };
        var expectedResult = new ExecutionResultDto
        {
            Status = "Accepted",
            Output = "hello"
        };

        _mockService.Setup(s => s.ExecuteCodeAsync(request))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.ExecuteCode(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expectedResult, okResult.Value);
        _mockService.Verify(s => s.ExecuteCodeAsync(request), Times.Once);
    }

    [Fact]
    public async Task ExecuteCode_ServiceThrowsException_Returns500()
    {
        // Arrange
        var request = new ExecutionRequestDto
        {
            SourceCode = "invalid code",
            Language = "javascript"
        };

        _mockService.Setup(s => s.ExecuteCodeAsync(request))
            .ThrowsAsync(new Exception("Execution failed"));

        // Act
        var result = await _controller.ExecuteCode(request);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task RunCode_ValidRequest_ReturnsOk()
    {
        // Arrange
        var questionId = Guid.NewGuid();
        var request = new ExecutionRequestDto
        {
            SourceCode = "var twoSum = function(nums, target) { return [0, 1]; };",
            Language = "javascript"
        };
        var expectedResults = new[]
        {
            new TestResultDto { Passed = true, Output = "[0,1]" }
        };

        _mockService.Setup(s => s.RunCodeAsync(questionId, request))
            .ReturnsAsync(expectedResults);

        // Act
        var result = await _controller.RunCode(questionId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expectedResults, okResult.Value);
        _mockService.Verify(s => s.RunCodeAsync(questionId, request), Times.Once);
    }

    [Fact]
    public async Task RunCode_QuestionNotFound_Returns404()
    {
        // Arrange
        var questionId = Guid.NewGuid();
        var request = new ExecutionRequestDto
        {
            SourceCode = "code",
            Language = "javascript"
        };

        _mockService.Setup(s => s.RunCodeAsync(questionId, request))
            .ThrowsAsync(new KeyNotFoundException());

        // Act
        var result = await _controller.RunCode(questionId, request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public async Task RunCodeWithTestCases_ValidRequest_ReturnsOk()
    {
        // Arrange
        var questionId = Guid.NewGuid();
        var request = new RunCodeWithTestCasesDto
        {
            SourceCode = "var twoSum = function(nums, target) { return [0, 1]; };",
            Language = "javascript",
            TestCaseText = "[2,7,11,15]\n9"
        };
        var expectedResult = new RunResultDto
        {
            Status = "ACCEPTED",
            Cases = new List<CaseResultDto>
            {
                new CaseResultDto { CaseIndex = 1, Passed = true }
            }
        };

        _mockService.Setup(s => s.RunCodeWithTestCasesAsync(questionId, request))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.RunCodeWithTestCases(questionId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expectedResult, okResult.Value);
        _mockService.Verify(s => s.RunCodeWithTestCasesAsync(questionId, request), Times.Once);
    }

    [Fact]
    public async Task RunCodeWithTestCases_QuestionNotFound_Returns404()
    {
        // Arrange
        var questionId = Guid.NewGuid();
        var request = new RunCodeWithTestCasesDto
        {
            SourceCode = "code",
            Language = "javascript",
            TestCaseText = "test"
        };

        _mockService.Setup(s => s.RunCodeWithTestCasesAsync(questionId, request))
            .ThrowsAsync(new KeyNotFoundException());

        // Act
        var result = await _controller.RunCodeWithTestCases(questionId, request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public async Task ValidateSolution_ValidRequest_ReturnsOk()
    {
        // Arrange
        var questionId = Guid.NewGuid();
        var request = new ExecutionRequestDto
        {
            SourceCode = "var twoSum = function(nums, target) { return [0, 1]; };",
            Language = "javascript"
        };
        var expectedResults = new[]
        {
            new TestResultDto { Passed = true, Output = "[0,1]" }
        };

        _mockService.Setup(s => s.ValidateSolutionAsync(questionId, request))
            .ReturnsAsync(expectedResults);

        // Act
        var result = await _controller.ValidateSolution(questionId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expectedResults, okResult.Value);
        _mockService.Verify(s => s.ValidateSolutionAsync(questionId, request), Times.Once);
    }

    [Fact]
    public async Task ValidateSolution_QuestionNotFound_Returns404()
    {
        // Arrange
        var questionId = Guid.NewGuid();
        var request = new ExecutionRequestDto
        {
            SourceCode = "code",
            Language = "javascript"
        };

        _mockService.Setup(s => s.ValidateSolutionAsync(questionId, request))
            .ThrowsAsync(new KeyNotFoundException());

        // Act
        var result = await _controller.ValidateSolution(questionId, request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public async Task GetSupportedLanguages_ReturnsOk()
    {
        // Arrange
        var expectedLanguages = new List<SupportedLanguageDto>
        {
            new SupportedLanguageDto { Judge0LanguageId = 93, Name = "JavaScript", Value = "javascript", Version = "18.15.0" },
            new SupportedLanguageDto { Judge0LanguageId = 92, Name = "Python", Value = "python", Version = "3.11.1" }
        };

        _mockService.Setup(s => s.GetSupportedLanguagesAsync())
            .ReturnsAsync(expectedLanguages);

        // Act
        var result = await _controller.GetSupportedLanguages();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expectedLanguages, okResult.Value);
        _mockService.Verify(s => s.GetSupportedLanguagesAsync(), Times.Once);
    }

    [Fact]
    public async Task GetSupportedLanguages_ServiceThrowsException_Returns500()
    {
        // Arrange
        _mockService.Setup(s => s.GetSupportedLanguagesAsync())
            .ThrowsAsync(new Exception("Service error"));

        // Act
        var result = await _controller.GetSupportedLanguages();

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }
}

