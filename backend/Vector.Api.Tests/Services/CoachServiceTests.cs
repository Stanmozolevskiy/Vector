using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Vector.Api.Data;
using Vector.Api.DTOs.Coach;
using Vector.Api.Models;
using Vector.Api.Services;
using Xunit;

namespace Vector.Api.Tests.Services;

public class CoachServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<ILogger<CoachService>> _loggerMock;
    private readonly CoachService _service;

    public CoachServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _emailServiceMock = new Mock<IEmailService>();
        _loggerMock = new Mock<ILogger<CoachService>>();

        _service = new CoachService(_context, _emailServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task SubmitApplicationAsync_WithValidData_CreatesApplication()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            Role = "student",
            EmailVerified = true
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var dto = new SubmitCoachApplicationDto
        {
            Motivation = "I want to help students",
            Experience = "5 years experience",
            Specialization = "System Design",
            ImageUrls = new List<string> { "https://example.com/image1.jpg", "https://example.com/image2.jpg" }
        };

        // Act
        var result = await _service.SubmitApplicationAsync(user.Id, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dto.Motivation, result.Motivation);
        Assert.Equal(dto.Experience, result.Experience);
        Assert.Equal(dto.Specialization, result.Specialization);
        Assert.NotNull(result.ImageUrls);
        var imageUrlsList = result.ImageUrls.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        Assert.Equal(2, imageUrlsList.Count);
        Assert.Contains("https://example.com/image1.jpg", imageUrlsList);
        Assert.Contains("https://example.com/image2.jpg", imageUrlsList);
        Assert.Equal("pending", result.Status);

        var application = await _context.CoachApplications.FirstOrDefaultAsync(a => a.UserId == user.Id);
        Assert.NotNull(application);
        Assert.Equal("pending", application.Status);
        _emailServiceMock.Verify(x => x.SendEmailAsync(
            user.Email,
            "Coach Application Received",
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task SubmitApplicationAsync_WhenUserIsAlreadyCoach_ThrowsException()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "coach@example.com",
            FirstName = "Coach",
            LastName = "User",
            Role = "coach",
            EmailVerified = true
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var dto = new SubmitCoachApplicationDto { Motivation = "Test motivation" };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.SubmitApplicationAsync(user.Id, dto));
    }

    [Fact]
    public async Task SubmitApplicationAsync_WhenApplicationExists_ThrowsException()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            Role = "student",
            EmailVerified = true
        };
        _context.Users.Add(user);

        var existingApplication = new CoachApplication
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Motivation = "Existing application",
            Status = "pending"
        };
        _context.CoachApplications.Add(existingApplication);
        await _context.SaveChangesAsync();

        var dto = new SubmitCoachApplicationDto { Motivation = "New application" };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.SubmitApplicationAsync(user.Id, dto));
    }

    [Fact]
    public async Task ReviewApplicationAsync_WhenApproved_UpdatesUserRole()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            Role = "student",
            EmailVerified = true
        };
        _context.Users.Add(user);

        var admin = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@example.com",
            FirstName = "Admin",
            LastName = "User",
            Role = "admin",
            EmailVerified = true
        };
        _context.Users.Add(admin);

        var application = new CoachApplication
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Motivation = "Test motivation",
            Status = "pending"
        };
        _context.CoachApplications.Add(application);
        await _context.SaveChangesAsync();

        var dto = new ReviewCoachApplicationDto
        {
            Status = "approved",
            AdminNotes = "Great application!"
        };

        // Act
        var result = await _service.ReviewApplicationAsync(application.Id, admin.Id, dto);

        // Assert
        Assert.Equal("approved", result.Status);
        Assert.Equal(dto.AdminNotes, result.AdminNotes);

        var updatedUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
        Assert.NotNull(updatedUser);
        Assert.Equal("coach", updatedUser.Role);

        _emailServiceMock.Verify(x => x.SendEmailAsync(
            user.Email,
            "Congratulations! Your Coach Application Has Been Approved",
            It.Is<string>(body => body.Contains("approved") && body.Contains(dto.AdminNotes))), 
            Times.Once);
    }

    [Fact]
    public async Task ReviewApplicationAsync_WhenRejected_SendsRejectionEmail()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            Role = "student",
            EmailVerified = true
        };
        _context.Users.Add(user);

        var admin = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@example.com",
            FirstName = "Admin",
            LastName = "User",
            Role = "admin",
            EmailVerified = true
        };
        _context.Users.Add(admin);

        var application = new CoachApplication
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Motivation = "Test motivation",
            Status = "pending"
        };
        _context.CoachApplications.Add(application);
        await _context.SaveChangesAsync();

        var dto = new ReviewCoachApplicationDto
        {
            Status = "rejected",
            AdminNotes = "Need more experience"
        };

        // Act
        var result = await _service.ReviewApplicationAsync(application.Id, admin.Id, dto);

        // Assert
        Assert.Equal("rejected", result.Status);
        Assert.Equal(dto.AdminNotes, result.AdminNotes);

        var updatedUser = await _context.Users.FindAsync(user.Id);
        Assert.Equal("student", updatedUser?.Role); // Role should not change

        _emailServiceMock.Verify(x => x.SendEmailAsync(
            user.Email,
            "Update on Your Coach Application",
            It.Is<string>(body => body.Contains("unable to approve") && body.Contains(dto.AdminNotes))), 
            Times.Once);
    }

    [Fact]
    public async Task GetApplicationByUserIdAsync_WithExistingApplication_ReturnsApplication()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            Role = "student"
        };
        _context.Users.Add(user);

        var application = new CoachApplication
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Motivation = "Test motivation",
            Status = "pending"
        };
        _context.CoachApplications.Add(application);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetApplicationByUserIdAsync(user.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(application.Id, result.Id);
        Assert.Equal(application.Motivation, result.Motivation);
    }

    [Fact]
    public async Task GetApplicationByUserIdAsync_WithNoApplication_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = await _service.GetApplicationByUserIdAsync(userId);

        // Assert
        Assert.Null(result);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}

