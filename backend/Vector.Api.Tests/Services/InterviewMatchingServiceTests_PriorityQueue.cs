using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Vector.Api.Data;
using Vector.Api.Models;
using Vector.Api.Services;
using Xunit;

namespace Vector.Api.Tests.Services;

/// <summary>
/// Tests for priority queuing: Users who confirmed but their match expired should be prioritized in the queue
/// Tests with 10+ users to verify queue ordering
/// </summary>
public class InterviewMatchingServiceTests_PriorityQueue : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IQuestionService> _questionServiceMock;
    private readonly Mock<ILogger<PeerInterviewService>> _loggerMock;
    private readonly Mock<ILogger<InterviewMatchingService>> _matchingLoggerMock;
    private readonly Mock<IMatchingPresenceService> _presenceServiceMock;
    private readonly PeerInterviewService _service;
    private readonly InterviewMatchingService _matchingService;

    public InterviewMatchingServiceTests_PriorityQueue()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        _questionServiceMock = new Mock<IQuestionService>();
        _loggerMock = new Mock<ILogger<PeerInterviewService>>();
        _matchingLoggerMock = new Mock<ILogger<InterviewMatchingService>>();
        _presenceServiceMock = new Mock<IMatchingPresenceService>();
        
        // Setup presence service to return true by default (user is active)
        _presenceServiceMock.Setup(p => p.IsUserActive(It.IsAny<Guid>(), It.IsAny<Guid>())).Returns(true);

        _service = new PeerInterviewService(
            _context,
            _questionServiceMock.Object,
            _loggerMock.Object
        );

        _matchingService = new InterviewMatchingService(
            _context,
            _service,
            _presenceServiceMock.Object,
            _matchingLoggerMock.Object
        );
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private async Task<(User user, ScheduledInterviewSession session)> CreateUserWithSession(
        string email, string level = "beginner")
    {
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = email, FirstName = "Test", LastName = "User" };
        await _context.Users.AddAsync(user);

        var session = new ScheduledInterviewSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            InterviewType = "data-structures-algorithms",
            PracticeType = "peers",
            InterviewLevel = level,
            ScheduledStartAt = DateTime.UtcNow.AddHours(1),
            Status = "Scheduled",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.ScheduledInterviewSessions.AddAsync(session);
        await _context.SaveChangesAsync();

        return (user, session);
    }

    private async Task<InterviewQuestion> CreateQuestion(string title, string difficulty = "Easy")
    {
        var question = new InterviewQuestion
        {
            Id = Guid.NewGuid(),
            Title = title,
            QuestionType = "Coding",
            Difficulty = difficulty,
            Category = "Arrays",
            IsActive = true,
            ApprovalStatus = "Approved",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.InterviewQuestions.AddAsync(question);
        await _context.SaveChangesAsync();
        return question;
    }

    [Fact]
    public async Task ExpireMatchIfNotConfirmedAsync_UserConfirmedButPartnerDidNot_PrioritizesConfirmedUser()
    {
        // Arrange: Create questions for matching
        await CreateQuestion("Test Question 1");
        await CreateQuestion("Test Question 2");

        // Create 12 users
        var users = new List<(User user, ScheduledInterviewSession session)>();
        for (int i = 0; i < 12; i++)
        {
            var (user, session) = await CreateUserWithSession($"user{i}@test.com", "beginner");
            users.Add((user, session));
        }

        // Users 0 and 1 match, User 0 confirms but User 1 doesn't
        var result1 = await _matchingService.StartMatchingAsync(users[0].session.Id, users[0].user.Id);
        var result2 = await _matchingService.StartMatchingAsync(users[1].session.Id, users[1].user.Id);
        
        await Task.Delay(500); // Allow matching to complete
        
        var status1 = await _matchingService.GetMatchingStatusAsync(users[0].session.Id, users[0].user.Id);
        var status2 = await _matchingService.GetMatchingStatusAsync(users[1].session.Id, users[1].user.Id);
        
        Assert.NotNull(status1);
        Assert.NotNull(status2);
        Assert.Equal("Matched", status1.Status);
        Assert.Equal("Matched", status2.Status);

        // Store original CreatedAt for User 0
        var expiredRequest1 = await _context.InterviewMatchingRequests
            .FirstOrDefaultAsync(r => r.Id == status1.Id);
        Assert.NotNull(expiredRequest1);
        var originalCreatedAt = expiredRequest1.CreatedAt;

        // User 0 confirms
        await _matchingService.ConfirmMatchAsync(status1!.Id, users[0].user.Id);
        
        // Reload to get updated UserConfirmed status
        await _context.Entry(expiredRequest1).ReloadAsync();
        Assert.True(expiredRequest1.UserConfirmed);
        
        // Simulate 15 seconds passing by updating UpdatedAt
        expiredRequest1.UpdatedAt = DateTime.UtcNow.AddSeconds(-16);
        await _context.SaveChangesAsync();

        // Now add 10 more users to the queue (Users 2-11)
        for (int i = 2; i < 12; i++)
        {
            await _matchingService.StartMatchingAsync(users[i].session.Id, users[i].user.Id);
        }
        
        await Task.Delay(500);

        // Expire the match - User 0 (who confirmed) should be re-queued with priority
        var expired = await _matchingService.ExpireMatchIfNotConfirmedAsync(expiredRequest1.Id, users[0].user.Id);
        Assert.True(expired);

        await Task.Delay(500);

        // Get all pending requests and verify ordering (also check Matched status as request might be matched immediately)
        var allPendingRequests = await _context.InterviewMatchingRequests
            .Where(r => (r.Status == "Pending" || r.Status == "Matched") && r.ExpiresAt > DateTime.UtcNow)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync();

        // Find User 0's new request (exclude the expired one)
        var expiredRequestId = expiredRequest1.Id;
        var user0NewRequest = allPendingRequests
            .Where(r => r.UserId == users[0].user.Id && r.Id != expiredRequestId)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefault();
        Assert.NotNull(user0NewRequest);
        
        // User 0's new request should have the original CreatedAt (priority)
        Assert.Equal(originalCreatedAt, user0NewRequest.CreatedAt);
        
        // User 0 should be before users who joined after their original request
        var user0Index = allPendingRequests.IndexOf(user0NewRequest);
        var usersJoinedAfterIndex = allPendingRequests
            .Where(r => r.CreatedAt > originalCreatedAt && r.UserId != users[0].user.Id)
            .Select(r => allPendingRequests.IndexOf(r))
            .ToList();
        
        // User 0 should be before all users who joined after
        Assert.All(usersJoinedAfterIndex, index => Assert.True(user0Index < index || user0Index == 0));
    }

    [Fact]
    public async Task ExpireMatchIfNotConfirmedAsync_With10Users_ConfirmedUserGetsPriorityOverNewUsers()
    {
        // Arrange: Create questions for matching
        await CreateQuestion("Test Question 1");
        await CreateQuestion("Test Question 2");

        // Create 10 users
        var users = new List<(User user, ScheduledInterviewSession session)>();
        for (int i = 0; i < 10; i++)
        {
            var (user, session) = await CreateUserWithSession($"user{i}@test.com", "beginner");
            users.Add((user, session));
        }

        // Users 0 and 1 match
        var result1 = await _matchingService.StartMatchingAsync(users[0].session.Id, users[0].user.Id);
        var result2 = await _matchingService.StartMatchingAsync(users[1].session.Id, users[1].user.Id);
        
        await Task.Delay(500);
        
        var status1 = await _matchingService.GetMatchingStatusAsync(users[0].session.Id, users[0].user.Id);
        Assert.NotNull(status1);
        Assert.Equal("Matched", status1.Status);

        // Store original CreatedAt for User 0
        var originalRequest = await _context.InterviewMatchingRequests
            .FirstOrDefaultAsync(r => r.Id == status1.Id);
        Assert.NotNull(originalRequest);
        var originalCreatedAt = originalRequest.CreatedAt;

        // User 0 confirms, User 1 does NOT confirm
        await _matchingService.ConfirmMatchAsync(status1!.Id, users[0].user.Id);
        
        // Reload to get updated UserConfirmed status
        await _context.Entry(originalRequest).ReloadAsync();
        Assert.True(originalRequest.UserConfirmed);

        // Simulate 15 seconds passing
        originalRequest.UpdatedAt = DateTime.UtcNow.AddSeconds(-16);
        await _context.SaveChangesAsync();

        // Add 8 more users (users 2-9) to the queue BEFORE expiring
        for (int i = 2; i < 10; i++)
        {
            await _matchingService.StartMatchingAsync(users[i].session.Id, users[i].user.Id);
        }
        
        await Task.Delay(500);

        // Now expire the match
        var expired = await _matchingService.ExpireMatchIfNotConfirmedAsync(originalRequest.Id, users[0].user.Id);
        Assert.True(expired);

        await Task.Delay(500);

        // Get all pending requests ordered by CreatedAt (also check Matched status)
        var allPendingRequests = await _context.InterviewMatchingRequests
            .Where(r => (r.Status == "Pending" || r.Status == "Matched") && r.ExpiresAt > DateTime.UtcNow)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync();

        // Find User 0's new request (exclude the expired one)
        var expiredRequestId = originalRequest.Id;
        var user0NewRequest = allPendingRequests
            .Where(r => r.UserId == users[0].user.Id && r.Id != expiredRequestId)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefault();
        Assert.NotNull(user0NewRequest);
        
        // User 0's new request should use the original CreatedAt (priority)
        Assert.Equal(originalCreatedAt, user0NewRequest.CreatedAt);
        
        // User 0 should be at the front of the queue (or near the front)
        var user0Index = allPendingRequests.IndexOf(user0NewRequest);
        // User 0 should be before users who joined after their original request
        Assert.True(user0Index < 3); // Should be in first few positions
    }

    [Fact]
    public async Task ExpireMatchOnUserDisconnectAsync_UserConfirmedButDisconnected_PrioritizesPartner()
    {
        // Arrange: Create questions for matching
        await CreateQuestion("Test Question 1");
        await CreateQuestion("Test Question 2");

        // Create 10 users
        var users = new List<(User user, ScheduledInterviewSession session)>();
        for (int i = 0; i < 10; i++)
        {
            var (user, session) = await CreateUserWithSession($"user{i}@test.com", "beginner");
            users.Add((user, session));
        }

        // Users 0 and 1 match
        var result1 = await _matchingService.StartMatchingAsync(users[0].session.Id, users[0].user.Id);
        var result2 = await _matchingService.StartMatchingAsync(users[1].session.Id, users[1].user.Id);
        
        await Task.Delay(500);
        
        var status1 = await _matchingService.GetMatchingStatusAsync(users[0].session.Id, users[0].user.Id);
        var status2 = await _matchingService.GetMatchingStatusAsync(users[1].session.Id, users[1].user.Id);
        
        Assert.NotNull(status1);
        Assert.NotNull(status2);
        Assert.Equal("Matched", status1.Status);

        // Store original CreatedAt for User 1
        var user1OriginalRequest = await _context.InterviewMatchingRequests
            .FirstOrDefaultAsync(r => r.Id == status2.Id);
        Assert.NotNull(user1OriginalRequest);
        var user1OriginalCreatedAt = user1OriginalRequest.CreatedAt;

        // User 1 confirms, User 0 does NOT confirm
        await _matchingService.ConfirmMatchAsync(status2!.Id, users[1].user.Id);
        
        // Reload to get updated UserConfirmed status
        await _context.Entry(user1OriginalRequest).ReloadAsync();
        Assert.True(user1OriginalRequest.UserConfirmed);

        // Add 8 more users (users 2-9) to the queue
        for (int i = 2; i < 10; i++)
        {
            await _matchingService.StartMatchingAsync(users[i].session.Id, users[i].user.Id);
        }
        
        await Task.Delay(500);

        // User 0 disconnects (who didn't confirm)
        var disconnected = await _matchingService.ExpireMatchOnUserDisconnectAsync(users[0].user.Id);
        Assert.True(disconnected);

        await Task.Delay(500);

        // Get all pending requests (also check Matched status)
        var allPendingRequests = await _context.InterviewMatchingRequests
            .Where(r => (r.Status == "Pending" || r.Status == "Matched") && r.ExpiresAt > DateTime.UtcNow)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync();

        // Find User 1's new request (exclude the expired one)
        var expiredRequestId = user1OriginalRequest.Id;
        var user1NewRequest = allPendingRequests
            .Where(r => r.UserId == users[1].user.Id && r.Id != expiredRequestId)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefault();
        Assert.NotNull(user1NewRequest);
        
        // User 1's new request should use the original CreatedAt (priority)
        Assert.Equal(user1OriginalCreatedAt, user1NewRequest.CreatedAt);
        
        // User 1 should be at the front of the queue
        var user1Index = allPendingRequests.IndexOf(user1NewRequest);
        Assert.True(user1Index < 3); // Should be in first few positions
    }

    [Fact]
    public async Task ExpireMatchIfNotConfirmedAsync_NeitherUserConfirmed_NoPriority()
    {
        // Arrange: Create questions for matching
        await CreateQuestion("Test Question 1");
        await CreateQuestion("Test Question 2");

        // Create 10 users
        var users = new List<(User user, ScheduledInterviewSession session)>();
        for (int i = 0; i < 10; i++)
        {
            var (user, session) = await CreateUserWithSession($"user{i}@test.com", "beginner");
            users.Add((user, session));
        }

        // Users 0 and 1 match, neither confirms
        var result1 = await _matchingService.StartMatchingAsync(users[0].session.Id, users[0].user.Id);
        var result2 = await _matchingService.StartMatchingAsync(users[1].session.Id, users[1].user.Id);
        
        await Task.Delay(500);
        
        var status1 = await _matchingService.GetMatchingStatusAsync(users[0].session.Id, users[0].user.Id);
        Assert.NotNull(status1);
        Assert.Equal("Matched", status1.Status);

        var originalRequest = await _context.InterviewMatchingRequests
            .FirstOrDefaultAsync(r => r.Id == status1.Id);
        Assert.NotNull(originalRequest);
        var originalCreatedAt = originalRequest.CreatedAt;

        // Verify neither user confirmed
        Assert.False(originalRequest.UserConfirmed);

        // Simulate 15 seconds passing
        originalRequest.UpdatedAt = DateTime.UtcNow.AddSeconds(-16);
        await _context.SaveChangesAsync();

        // Add 8 more users (users 2-9) to the queue
        for (int i = 2; i < 10; i++)
        {
            await _matchingService.StartMatchingAsync(users[i].session.Id, users[i].user.Id);
        }
        
        await Task.Delay(500);

        // Expire the match - neither user confirmed, so no priority
        var expired = await _matchingService.ExpireMatchIfNotConfirmedAsync(originalRequest.Id, users[0].user.Id);
        Assert.True(expired);

        await Task.Delay(500);

        // Get all pending requests (also check Matched status)
        var allPendingRequests = await _context.InterviewMatchingRequests
            .Where(r => (r.Status == "Pending" || r.Status == "Matched") && r.ExpiresAt > DateTime.UtcNow)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync();

        // Find User 0's new request (exclude the expired one)
        var expiredRequestId = originalRequest.Id;
        var user0NewRequest = allPendingRequests
            .Where(r => r.UserId == users[0].user.Id && r.Id != expiredRequestId)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefault();
        Assert.NotNull(user0NewRequest);
        
        // User 0's new request should have current timestamp (no priority)
        // Since they didn't confirm, they should be at the end of the queue
        Assert.True(user0NewRequest.CreatedAt >= DateTime.UtcNow.AddSeconds(-5));
        
        // User 0 should be at the end (or near the end) of the queue
        var user0Index = allPendingRequests.IndexOf(user0NewRequest);
        Assert.True(user0Index >= allPendingRequests.Count - 2); // Should be in last 2 positions
    }

    [Fact]
    public async Task ExpireMatchIfNotConfirmedAsync_BothUsersConfirmed_NoRequeue()
    {
        // Arrange: Create questions for matching
        await CreateQuestion("Test Question 1");
        await CreateQuestion("Test Question 2");

        // Create 2 users
        var users = new List<(User user, ScheduledInterviewSession session)>();
        for (int i = 0; i < 2; i++)
        {
            var (user, session) = await CreateUserWithSession($"user{i}@test.com", "beginner");
            users.Add((user, session));
        }

        // Users 0 and 1 match
        var result1 = await _matchingService.StartMatchingAsync(users[0].session.Id, users[0].user.Id);
        var result2 = await _matchingService.StartMatchingAsync(users[1].session.Id, users[1].user.Id);
        
        await Task.Delay(500);
        
        var status1 = await _matchingService.GetMatchingStatusAsync(users[0].session.Id, users[0].user.Id);
        var status2 = await _matchingService.GetMatchingStatusAsync(users[1].session.Id, users[1].user.Id);
        
        Assert.NotNull(status1);
        Assert.NotNull(status2);
        Assert.Equal("Matched", status1.Status);

        // Both users confirm
        await _matchingService.ConfirmMatchAsync(status1!.Id, users[0].user.Id);
        await _matchingService.ConfirmMatchAsync(status2!.Id, users[1].user.Id);
        
        await Task.Delay(500);

        // Reload to get updated status
        var request1 = await _context.InterviewMatchingRequests
            .FirstOrDefaultAsync(r => r.Id == status1.Id);
        Assert.NotNull(request1);
        Assert.Equal("Confirmed", request1.Status);

        // Try to expire - should return false (both confirmed)
        var expired = await _matchingService.ExpireMatchIfNotConfirmedAsync(request1.Id, users[0].user.Id);
        Assert.False(expired);
    }
}
