using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using StackExchange.Redis;
using Vector.Api.Data;
using Vector.Api.Services;

namespace Vector.Api.Tests.Integration;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = "TestDb_" + Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the real database
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add InMemory database for testing - use same database name for all scopes
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
            });

            // Remove real Redis connection
            var redisDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IConnectionMultiplexer));
            if (redisDescriptor != null)
            {
                services.Remove(redisDescriptor);
            }

            // Mock Redis
            var redisMock = new Mock<IConnectionMultiplexer>();
            var redisServiceMock = new Mock<IRedisService>();
            
            // Setup Redis service mocks
            redisServiceMock.Setup(r => r.StoreRefreshTokenAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<TimeSpan>()))
                .ReturnsAsync(true);
            redisServiceMock.Setup(r => r.IsTokenBlacklistedAsync(It.IsAny<string>()))
                .ReturnsAsync(false);
            redisServiceMock.Setup(r => r.BlacklistTokenAsync(It.IsAny<string>(), It.IsAny<TimeSpan>()))
                .ReturnsAsync(true);
            redisServiceMock.Setup(r => r.CheckRateLimitAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan>()))
                .ReturnsAsync(true);
            redisServiceMock.Setup(r => r.ResetRateLimitAsync(It.IsAny<string>()))
                .ReturnsAsync(true);
            // Setup generic method for any type T
            redisServiceMock.Setup(r => r.GetCachedUserSessionAsync<Vector.Api.Models.User>(It.IsAny<Guid>()))
                .ReturnsAsync((Vector.Api.Models.User?)null);
            redisServiceMock.Setup(r => r.CacheUserSessionAsync(It.IsAny<Guid>(), It.IsAny<object>(), It.IsAny<TimeSpan>()))
                .ReturnsAsync(true);

            services.AddSingleton(redisMock.Object);
            services.AddSingleton(redisServiceMock.Object);

            // Mock Email Service
            var emailServiceMock = new Mock<IEmailService>();
            emailServiceMock.Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            emailServiceMock.Setup(e => e.SendVerificationEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            emailServiceMock.Setup(e => e.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var emailDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IEmailService));
            if (emailDescriptor != null)
            {
                services.Remove(emailDescriptor);
            }
            services.AddSingleton(emailServiceMock.Object);

            // Mock S3 Service
            var s3ServiceMock = new Mock<IS3Service>();
            s3ServiceMock.Setup(s => s.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("https://test-bucket.s3.amazonaws.com/test-file.jpg");

            var s3Descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IS3Service));
            if (s3Descriptor != null)
            {
                services.Remove(s3Descriptor);
            }
            services.AddSingleton(s3ServiceMock.Object);
        });

        builder.UseEnvironment("Test");
    }

    public ApplicationDbContext GetDbContext()
    {
        var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    }

    public IServiceScope CreateScope()
    {
        return Services.CreateScope();
    }
}

