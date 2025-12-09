using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vector.Api.Data;
using Vector.Api.DTOs.Auth;
using Vector.Api.Models;
using Xunit;

namespace Vector.Api.Tests.Integration;

public class AuthIntegrationTests : IClassFixture<TestWebApplicationFactory>, IDisposable
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private IServiceScope? _scope;
    private ApplicationDbContext? _context;

    public AuthIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    private ApplicationDbContext GetContext()
    {
        if (_context == null)
        {
            _scope = _factory.CreateScope();
            _context = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        }
        return _context;
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsCreated()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Email = "test@example.com",
            Password = "Test1234!",
            FirstName = "Test",
            LastName = "User"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", registerDto);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(result.TryGetProperty("id", out _));
        Assert.Equal("test@example.com", result.GetProperty("email").GetString());

        // Verify user was created in database
        var user = await GetContext().Users.FirstOrDefaultAsync(u => u.Email == "test@example.com");
        Assert.NotNull(user);
        Assert.Equal("Test", user.FirstName);
        Assert.Equal("User", user.LastName);
    }

    [Fact]
    public async Task Register_WithInvalidEmail_ReturnsBadRequest()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Email = "invalid-email",
            Password = "Test1234!",
            FirstName = "Test",
            LastName = "User"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", registerDto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithWeakPassword_ReturnsBadRequest()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Email = "test@example.com",
            Password = "weak",
            FirstName = "Test",
            LastName = "User"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", registerDto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokens()
    {
        // Arrange - Register a user first
        var registerDto = new RegisterDto
        {
            Email = "login@example.com",
            Password = "Test1234!",
            FirstName = "Login",
            LastName = "User"
        };
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerDto);
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        // Wait a moment for the database to be updated
        await Task.Delay(100);

        // Verify email (in real scenario, user clicks link, but for test we'll verify directly)
        var context = GetContext();
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == "login@example.com");
        
        // Retry once if user not found
        if (user == null)
        {
            await Task.Delay(100);
            user = await context.Users.FirstOrDefaultAsync(u => u.Email == "login@example.com");
        }

        if (user != null)
        {
            user.EmailVerified = true;
            await context.SaveChangesAsync();
        }
        else
        {
            throw new InvalidOperationException("User was not created after registration");
        }

        var loginDto = new LoginDto
        {
            Email = "login@example.com",
            Password = "Test1234!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(result.TryGetProperty("accessToken", out _));
        Assert.True(result.TryGetProperty("tokenType", out _));
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "nonexistent@example.com",
            Password = "WrongPassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMe_WithoutToken_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/users/me");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMe_WithValidToken_ReturnsUser()
    {
        // Arrange - Register and verify user
        var registerDto = new RegisterDto
        {
            Email = "me@example.com",
            Password = "Test1234!",
            FirstName = "Me",
            LastName = "User"
        };
        await _client.PostAsJsonAsync("/api/auth/register", registerDto);

        // Wait a moment for the database to be updated
        await Task.Delay(100);

        // Verify email
        var context = GetContext();
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == "me@example.com");
        
        // Retry once if user not found
        if (user == null)
        {
            await Task.Delay(100);
            user = await context.Users.FirstOrDefaultAsync(u => u.Email == "me@example.com");
        }

        if (user != null)
        {
            user.EmailVerified = true;
            await context.SaveChangesAsync();
        }
        else
        {
            throw new InvalidOperationException("User was not created after registration");
        }

        var loginDto = new LoginDto
        {
            Email = "me@example.com",
            Password = "Test1234!"
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginDto);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        if (loginResult.TryGetProperty("accessToken", out var tokenElement))
        {
            var token = tokenElement.GetString();
            _client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await _client.GetAsync("/api/users/me");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var userResult = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("me@example.com", userResult.GetProperty("email").GetString());
        }
    }

    public void Dispose()
    {
        _context?.Database.EnsureDeleted();
        _context?.Dispose();
        _scope?.Dispose();
        _client.Dispose();
    }
}

