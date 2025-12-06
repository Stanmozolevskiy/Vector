using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Vector.Api.Controllers;
using Vector.Api.DTOs.Coach;
using Vector.Api.Models;
using Vector.Api.Services;
using Xunit;

namespace Vector.Api.Tests.Controllers;

public class CoachControllerTests
{
    private readonly Mock<ICoachService> _coachServiceMock;
    private readonly Mock<IS3Service> _s3ServiceMock;
    private readonly Mock<ILogger<CoachController>> _mockLogger;
    private readonly CoachController _controller;

    public CoachControllerTests()
    {
        _coachServiceMock = new Mock<ICoachService>();
        _s3ServiceMock = new Mock<IS3Service>();
        _mockLogger = new Mock<ILogger<CoachController>>();
        
        _controller = new CoachController(_coachServiceMock.Object, _s3ServiceMock.Object, _mockLogger.Object);
    }

    private void SetupControllerWithUser(Guid userId, string role = "student")
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    private void SetupControllerWithoutUser()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
        };
    }

    [Fact]
    public async Task SubmitApplication_WithValidData_ReturnsCreated()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupControllerWithUser(userId);
        
        var dto = new SubmitCoachApplicationDto
        {
            Motivation = "I want to help students prepare for interviews",
            Experience = "5 years of software engineering",
            Specialization = "System Design",
            ImageUrls = new List<string> { "https://example.com/image1.jpg" }
        };

        var application = new CoachApplication
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Motivation = dto.Motivation,
            Experience = dto.Experience,
            Specialization = dto.Specialization,
            ImageUrls = string.Join(",", dto.ImageUrls),
            Status = "pending",
            User = new User { Id = userId, Email = "test@example.com", FirstName = "Test", LastName = "User" }
        };

        _coachServiceMock.Setup(x => x.SubmitApplicationAsync(userId, dto))
            .ReturnsAsync(application);
        
        // Mock GetApplicationByUserIdAsync since controller calls it after submission
        _coachServiceMock.Setup(x => x.GetApplicationByUserIdAsync(userId))
            .ReturnsAsync(application);

        // Act
        var result = await _controller.SubmitApplication(dto);

        // Assert
        var createdResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(201, createdResult.StatusCode);
        _coachServiceMock.Verify(x => x.SubmitApplicationAsync(userId, dto), Times.Once);
        _coachServiceMock.Verify(x => x.GetApplicationByUserIdAsync(userId), Times.Once);
    }

    [Fact]
    public async Task SubmitApplication_WithInvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        SetupControllerWithoutUser();
        var dto = new SubmitCoachApplicationDto { Motivation = "Test motivation" };

        // Act
        var result = await _controller.SubmitApplication(dto);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(401, unauthorizedResult.StatusCode);
    }

    [Fact]
    public async Task GetMyApplication_WithValidTokenAndApplication_ReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupControllerWithUser(userId);

        var application = new CoachApplication
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Motivation = "Test motivation",
            Status = "pending",
            User = new User { Id = userId, Email = "test@example.com", FirstName = "Test", LastName = "User" }
        };

        _coachServiceMock.Setup(x => x.GetApplicationByUserIdAsync(userId))
            .ReturnsAsync(application);

        // Act
        var result = await _controller.GetMyApplication();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        _coachServiceMock.Verify(x => x.GetApplicationByUserIdAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetMyApplication_WithNoApplication_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupControllerWithUser(userId);

        _coachServiceMock.Setup(x => x.GetApplicationByUserIdAsync(userId))
            .ReturnsAsync((CoachApplication?)null);

        // Act
        var result = await _controller.GetMyApplication();

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFoundResult.StatusCode);
    }

    [Fact]
    public async Task GetMyApplication_WithInvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        SetupControllerWithoutUser();

        // Act
        var result = await _controller.GetMyApplication();

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(401, unauthorizedResult.StatusCode);
    }

    [Fact]
    public async Task UploadImage_WithValidFile_ReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupControllerWithUser(userId);

        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("test.jpg");
        fileMock.Setup(f => f.ContentType).Returns("image/jpeg");
        fileMock.Setup(f => f.Length).Returns(1024);
        fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream());

        var imageUrl = "https://example.com/image.jpg";
        _s3ServiceMock.Setup(x => x.UploadFileAsync(
            It.IsAny<Stream>(), 
            It.IsAny<string>(), 
            It.IsAny<string>(), 
            "coach-applications"))
            .ReturnsAsync(imageUrl);

        // Act
        var result = await _controller.UploadImage(fileMock.Object);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        _s3ServiceMock.Verify(x => x.UploadFileAsync(
            It.IsAny<Stream>(), 
            It.IsAny<string>(), 
            It.IsAny<string>(), 
            "coach-applications"), Times.Once);
    }

    [Fact]
    public async Task UploadImage_WithInvalidFileType_ReturnsBadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupControllerWithUser(userId);

        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("test.pdf");
        fileMock.Setup(f => f.ContentType).Returns("application/pdf");
        fileMock.Setup(f => f.Length).Returns(1024);

        // Act
        var result = await _controller.UploadImage(fileMock.Object);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task UploadImage_WithFileTooLarge_ReturnsBadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupControllerWithUser(userId);

        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("test.jpg");
        fileMock.Setup(f => f.ContentType).Returns("image/jpeg");
        fileMock.Setup(f => f.Length).Returns(11 * 1024 * 1024); // 11MB

        // Act
        var result = await _controller.UploadImage(fileMock.Object);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }
}

