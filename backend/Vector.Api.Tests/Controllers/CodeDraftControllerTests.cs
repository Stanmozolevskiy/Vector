using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Vector.Api.Controllers;
using Vector.Api.DTOs.Solution;
using Vector.Api.Models;
using Vector.Api.Services;
using Xunit;

namespace Vector.Api.Tests.Controllers;

public class CodeDraftControllerTests
{
    private readonly Mock<ICodeDraftService> _codeDraftServiceMock;
    private readonly Mock<ILogger<CodeDraftController>> _loggerMock;
    private readonly CodeDraftController _controller;

    public CodeDraftControllerTests()
    {
        _codeDraftServiceMock = new Mock<ICodeDraftService>();
        _loggerMock = new Mock<ILogger<CodeDraftController>>();
        _controller = new CodeDraftController(_codeDraftServiceMock.Object, _loggerMock.Object);
    }

    private void SetupControllerWithUser(Guid userId)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, "User")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        
        var httpContext = new DefaultHttpContext
        {
            User = principal
        };
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    [Fact]
    public async Task GetCodeDraft_WhenDraftExists_ReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var questionId = Guid.NewGuid();
        var language = "javascript";
        SetupControllerWithUser(userId);

        var codeDraft = new CodeDraftDto
        {
            Id = Guid.NewGuid(),
            QuestionId = questionId,
            Language = language,
            Code = "function test() { return true; }",
            UpdatedAt = DateTime.UtcNow
        };

        _codeDraftServiceMock.Setup(x => x.GetCodeDraftAsync(userId, questionId, language))
            .ReturnsAsync(codeDraft);

        // Act
        var result = await _controller.GetCodeDraft(questionId, language);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        var returnedDraft = Assert.IsType<CodeDraftDto>(okResult.Value);
        Assert.Equal(codeDraft.Code, returnedDraft.Code);
    }

    [Fact]
    public async Task GetCodeDraft_WhenDraftDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var questionId = Guid.NewGuid();
        var language = "javascript";
        SetupControllerWithUser(userId);

        _codeDraftServiceMock.Setup(x => x.GetCodeDraftAsync(userId, questionId, language))
            .ReturnsAsync((CodeDraftDto?)null);

        // Act
        var result = await _controller.GetCodeDraft(questionId, language);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFoundResult.StatusCode);
    }

    [Fact]
    public async Task SaveCodeDraft_WithValidRequest_ReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var questionId = Guid.NewGuid();
        var language = "javascript";
        SetupControllerWithUser(userId);

        var saveDto = new SaveCodeDraftDto
        {
            QuestionId = questionId,
            Language = language,
            Code = "function test() { return true; }"
        };

        var savedDraft = new CodeDraftDto
        {
            Id = Guid.NewGuid(),
            QuestionId = questionId,
            Language = language,
            Code = saveDto.Code,
            UpdatedAt = DateTime.UtcNow
        };

        _codeDraftServiceMock.Setup(x => x.SaveCodeDraftAsync(userId, saveDto))
            .ReturnsAsync(savedDraft);

        // Act
        var result = await _controller.SaveCodeDraft(saveDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        var returnedDraft = Assert.IsType<CodeDraftDto>(okResult.Value);
        Assert.Equal(savedDraft.Code, returnedDraft.Code);
    }
}

