using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using Vector.Api.Controllers;
using Vector.Api.Services;
using Xunit;

namespace Vector.Api.Tests.Controllers;

public class UserControllerProfilePictureTests
{
    private readonly Mock<IUserService> _userServiceMock;
    private readonly Mock<IJwtService> _jwtServiceMock;
    private readonly Mock<ILogger<UserController>> _loggerMock;
    private readonly UserController _controller;

    public UserControllerProfilePictureTests()
    {
        _userServiceMock = new Mock<IUserService>();
        _jwtServiceMock = new Mock<IJwtService>();
        _loggerMock = new Mock<ILogger<UserController>>();
        _controller = new UserController(_userServiceMock.Object, _jwtServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task UploadProfilePicture_WithValidImage_ReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupUserClaim(userId);

        var fileMock = new Mock<IFormFile>();
        var content = "fake image content";
        var fileName = "test.jpg";
        var ms = new MemoryStream();
        var writer = new StreamWriter(ms);
        writer.Write(content);
        writer.Flush();
        ms.Position = 0;

        fileMock.Setup(_ => _.OpenReadStream()).Returns(ms);
        fileMock.Setup(_ => _.FileName).Returns(fileName);
        fileMock.Setup(_ => _.Length).Returns(ms.Length);
        fileMock.Setup(_ => _.ContentType).Returns("image/jpeg");

        var expectedUrl = "https://s3.amazonaws.com/bucket/profile-pictures/test.jpg";
        _userServiceMock.Setup(x => x.UploadProfilePictureAsync(userId, It.IsAny<Stream>(), fileName, "image/jpeg"))
            .ReturnsAsync(expectedUrl);

        // Act
        var result = await _controller.UploadProfilePicture(fileMock.Object);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        dynamic value = okResult.Value!;
        Assert.Equal(expectedUrl, value.GetType().GetProperty("profilePictureUrl")!.GetValue(value, null));
    }

    [Fact]
    public async Task UploadProfilePicture_WithNoFile_ReturnsBadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupUserClaim(userId);

        // Act
        var result = await _controller.UploadProfilePicture(null!);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        dynamic value = badRequestResult.Value!;
        Assert.NotNull(value.GetType().GetProperty("error")!.GetValue(value, null));
    }

    [Fact]
    public async Task UploadProfilePicture_WithInvalidFileType_ReturnsBadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupUserClaim(userId);

        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(_ => _.ContentType).Returns("application/pdf");
        fileMock.Setup(_ => _.Length).Returns(1000);

        // Act
        var result = await _controller.UploadProfilePicture(fileMock.Object);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        dynamic value = badRequestResult.Value!;
        var error = value.GetType().GetProperty("error")!.GetValue(value, null);
        Assert.Contains("Invalid file type", error.ToString());
    }

    [Fact]
    public async Task UploadProfilePicture_WithLargeFile_ReturnsBadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupUserClaim(userId);

        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(_ => _.ContentType).Returns("image/jpeg");
        fileMock.Setup(_ => _.Length).Returns(6 * 1024 * 1024); // 6MB (exceeds 5MB limit)

        // Act
        var result = await _controller.UploadProfilePicture(fileMock.Object);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        dynamic value = badRequestResult.Value!;
        var error = value.GetType().GetProperty("error")!.GetValue(value, null);
        Assert.Contains("exceeds 5MB", error.ToString());
    }

    [Fact]
    public async Task UploadProfilePicture_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange - No user claim set
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(_ => _.ContentType).Returns("image/jpeg");
        fileMock.Setup(_ => _.Length).Returns(1000);

        // Act
        var result = await _controller.UploadProfilePicture(fileMock.Object);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task DeleteProfilePicture_WithExistingPicture_ReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupUserClaim(userId);

        _userServiceMock.Setup(x => x.DeleteProfilePictureAsync(userId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteProfilePicture();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        dynamic value = okResult.Value!;
        Assert.NotNull(value.GetType().GetProperty("message")!.GetValue(value, null));
    }

    [Fact]
    public async Task DeleteProfilePicture_WithNoPicture_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupUserClaim(userId);

        _userServiceMock.Setup(x => x.DeleteProfilePictureAsync(userId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteProfilePicture();

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeleteProfilePicture_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange - No user claim set

        // Act
        var result = await _controller.DeleteProfilePicture();

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    private void SetupUserClaim(Guid userId)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = claimsPrincipal
            }
        };
    }
}

