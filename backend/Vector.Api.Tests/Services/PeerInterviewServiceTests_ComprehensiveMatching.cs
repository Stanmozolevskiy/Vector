using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Vector.Api.Data;
using Vector.Api.Models;
using Vector.Api.Services;
using Xunit;

namespace Vector.Api.Tests.Services;

/// <summary>
/// Comprehensive tests for matching, rematching, and expiring scenarios with 5-10 users
/// Tests that LiveInterviewSession is created only after both users confirm with InProgress status
/// Tests that expiration creates new requests for both users and re-adds them to queue
/// </summary>
public class PeerInterviewServiceComprehensiveMatchingTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IQuestionService> _questionServiceMock;
    private readonly Mock<ILogger<PeerInterviewService>> _loggerMock;
    private readonly Mock<ILogger<InterviewMatchingService>> _matchingLoggerMock;
    private readonly Mock<IMatchingPresenceService> _presenceServiceMock;
    private readonly PeerInterviewService _service;
    private readonly InterviewMatchingService _matchingService;

    public PeerInterviewServiceComprehensiveMatchingTests()
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
            _service, // Use real PeerInterviewService since InterviewMatchingService depends on it
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
    public async Task FiveUsers_Matching_Rematching_Expiring_ComprehensiveScenario()
    {
        // Arrange: Create 5 users
        var (user1, session1) = await CreateUserWithSession("user1@test.com", "beginner");
        var (user2, session2) = await CreateUserWithSession("user2@test.com", "beginner");
        var (user3, session3) = await CreateUserWithSession("user3@test.com", "beginner");
        var (user4, session4) = await CreateUserWithSession("user4@test.com", "beginner");
        var (user5, session5) = await CreateUserWithSession("user5@test.com", "beginner");

        var q1 = await CreateQuestion("Question 1", "Easy");
        session1.AssignedQuestionId = q1.Id;
        session2.AssignedQuestionId = q1.Id;
        session3.AssignedQuestionId = q1.Id;
        session4.AssignedQuestionId = q1.Id;
        session5.AssignedQuestionId = q1.Id;
        await _context.SaveChangesAsync();

        // Act 1: Users 1-2 start matching and get matched
        await _matchingService.StartMatchingAsync(session1.Id, user1.Id);
        await _matchingService.StartMatchingAsync(session2.Id, user2.Id);
        await Task.Delay(500);

        var request1 = await _context.InterviewMatchingRequests
            .FirstOrDefaultAsync(r => r.UserId == user1.Id && r.Status == "Matched");
        var request2 = await _context.InterviewMatchingRequests
            .FirstOrDefaultAsync(r => r.UserId == user2.Id && r.Status == "Matched");

        Assert.NotNull(request1);
        Assert.NotNull(request2);
        Assert.Null(request1!.LiveSessionId); // No live session until both confirm

        // Act 2: Both users confirm - LiveInterviewSession should be created with InProgress status
        var confirm1 = await _matchingService.ConfirmMatchAsync(request1.Id, user1.Id);
        Assert.False(confirm1.Completed); // Not completed yet

        var confirm2 = await _matchingService.ConfirmMatchAsync(request2!.Id, user2.Id);
        Assert.True(confirm2.Completed); // Both confirmed
        Assert.NotNull(confirm2.Session);

        // Verify live session was created with InProgress status
        var liveSession = await _context.LiveInterviewSessions
            .FirstOrDefaultAsync(s => s.Id == confirm2.Session!.Id);
        Assert.NotNull(liveSession);
        Assert.Equal("InProgress", liveSession.Status);
        Assert.NotNull(liveSession.StartedAt);

        // Verify both requests are confirmed
        await _context.Entry(request1).ReloadAsync();
        await _context.Entry(request2).ReloadAsync();
        Assert.Equal("Confirmed", request1.Status);
        Assert.Equal("Confirmed", request2.Status);
        Assert.NotNull(request1.LiveSessionId);
        Assert.NotNull(request2.LiveSessionId);

        // Act 3: Users 3-4 start matching and get matched
        await _matchingService.StartMatchingAsync(session3.Id, user3.Id);
        await _matchingService.StartMatchingAsync(session4.Id, user4.Id);
        await Task.Delay(500);

        var request3 = await _context.InterviewMatchingRequests
            .FirstOrDefaultAsync(r => r.UserId == user3.Id && r.Status == "Matched");
        var request4 = await _context.InterviewMatchingRequests
            .FirstOrDefaultAsync(r => r.UserId == user4.Id && r.Status == "Matched");

        Assert.NotNull(request3);
        Assert.NotNull(request4);
        Assert.Null(request3!.LiveSessionId); // No live session until both confirm

        // Act 4: Neither user confirms - expire after 15 seconds
        request3.UpdatedAt = DateTime.UtcNow.AddSeconds(-16);
        request4!.UpdatedAt = DateTime.UtcNow.AddSeconds(-16);
        await _context.SaveChangesAsync();

        var expired = await _matchingService.ExpireMatchIfNotConfirmedAsync(request3.Id, user3.Id);
        Assert.True(expired);

        // Wait for rematching to complete
        await Task.Delay(2000);

        // Assert: Both users should be expired
        await _context.Entry(request3).ReloadAsync();
        await _context.Entry(request4).ReloadAsync();

        Assert.Equal("Expired", request3.Status);
        Assert.Equal("Expired", request4.Status);

        // Verify new requests were created for both users
        var allRequestsForUser3 = await _context.InterviewMatchingRequests
            .Where(r => r.UserId == user3.Id)
            .ToListAsync();
        var allRequestsForUser4 = await _context.InterviewMatchingRequests
            .Where(r => r.UserId == user4.Id)
            .ToListAsync();

        var newRequest3 = allRequestsForUser3
            .Where(r => (r.Status == "Pending" || r.Status == "Matched") && r.Id != request3.Id)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefault();
        var newRequest4 = allRequestsForUser4
            .Where(r => (r.Status == "Pending" || r.Status == "Matched") && r.Id != request4.Id)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefault();

        Assert.NotNull(newRequest3);
        Assert.NotNull(newRequest4);

        // Act 5: User 5 starts matching
        // Note: Users 3 and 4's new requests might match with each other, so User 5 may not match immediately
        await _matchingService.StartMatchingAsync(session5.Id, user5.Id);
        await Task.Delay(500);

        // Assert: User 5 should have a request (may be Pending if users 3-4 matched with each other)
        var request5 = await _context.InterviewMatchingRequests
            .FirstOrDefaultAsync(r => r.UserId == user5.Id && (r.Status == "Pending" || r.Status == "Matched"));
        Assert.NotNull(request5);
        
        // If users 3 and 4 matched with each other, User 5 will be Pending (waiting for another user)
        // This is expected behavior - users 3-4 get priority since they were re-queued
        if (request5!.Status == "Matched")
        {
            Assert.NotNull(request5.MatchedUserId);
        }
        else
        {
            // User 5 is waiting - this is fine, users 3-4 matched with each other
            Assert.Equal("Pending", request5.Status);
        }
    }

    [Fact]
    public async Task EightUsers_MixedScenarios_SomeConfirm_SomeExpire()
    {
        // Arrange: Create 8 users
        var users = new List<(User user, ScheduledInterviewSession session)>();
        for (int i = 1; i <= 8; i++)
        {
            var (user, session) = await CreateUserWithSession($"user{i}@test.com", "beginner");
            users.Add((user, session));
        }

        var q1 = await CreateQuestion("Question 1", "Easy");
        foreach (var (_, session) in users)
        {
            session.AssignedQuestionId = q1.Id;
        }
        await _context.SaveChangesAsync();

        // Act 1: All 8 users start matching (should form 4 pairs)
        var tasks = users.Select(u => _matchingService.StartMatchingAsync(u.session.Id, u.user.Id)).ToArray();
        await Task.WhenAll(tasks);
        await Task.Delay(1000);

        var matchedRequests = await _context.InterviewMatchingRequests
            .Where(r => r.Status == "Matched")
            .ToListAsync();
        Assert.Equal(8, matchedRequests.Count); // 4 pairs

        // Act 2: First pair (users 1-2) - both confirm (should create live session)
        var request1 = matchedRequests.First(r => r.UserId == users[0].user.Id);
        var request2 = matchedRequests.First(r => r.UserId == users[1].user.Id);

        await _matchingService.ConfirmMatchAsync(request1.Id, users[0].user.Id);
        var confirm2 = await _matchingService.ConfirmMatchAsync(request2.Id, users[1].user.Id);

        Assert.True(confirm2.Completed);
        Assert.NotNull(confirm2.Session);

        var liveSession1 = await _context.LiveInterviewSessions
            .FirstOrDefaultAsync(s => s.Id == confirm2.Session!.Id);
        Assert.NotNull(liveSession1);
        Assert.Equal("InProgress", liveSession1.Status);

        // Act 3: Second pair (users 3-4) - one confirms, one doesn't (should expire both)
        var request3 = matchedRequests.First(r => r.UserId == users[2].user.Id);
        var request4 = matchedRequests.First(r => r.UserId == users[3].user.Id);

        await _matchingService.ConfirmMatchAsync(request3.Id, users[2].user.Id);

        // Simulate 15 seconds passing
        request3.UpdatedAt = DateTime.UtcNow.AddSeconds(-16);
        request4.UpdatedAt = DateTime.UtcNow.AddSeconds(-16);
        await _context.SaveChangesAsync();

        await _matchingService.ExpireMatchIfNotConfirmedAsync(request3.Id, users[2].user.Id);
        await Task.Delay(2000);

        // Assert: Both should be expired
        await _context.Entry(request3).ReloadAsync();
        await _context.Entry(request4).ReloadAsync();
        Assert.Equal("Expired", request3.Status);
        Assert.Equal("Expired", request4.Status);

        // Verify new requests created
        var newRequest3 = await _context.InterviewMatchingRequests
            .Where(r => r.UserId == users[2].user.Id && r.Status != "Expired" && r.Id != request3.Id)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync();
        var newRequest4 = await _context.InterviewMatchingRequests
            .Where(r => r.UserId == users[3].user.Id && r.Status != "Expired" && r.Id != request4.Id)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync();

        Assert.NotNull(newRequest3);
        Assert.NotNull(newRequest4);

        // Act 4: Third pair (users 5-6) - neither confirms (should expire both)
        var request5 = matchedRequests.First(r => r.UserId == users[4].user.Id);
        var request6 = matchedRequests.First(r => r.UserId == users[5].user.Id);

        request5.UpdatedAt = DateTime.UtcNow.AddSeconds(-16);
        request6.UpdatedAt = DateTime.UtcNow.AddSeconds(-16);
        await _context.SaveChangesAsync();

        await _matchingService.ExpireMatchIfNotConfirmedAsync(request5.Id, users[4].user.Id);
        await Task.Delay(2000);

        // Assert: Both should be expired
        await _context.Entry(request5).ReloadAsync();
        await _context.Entry(request6).ReloadAsync();
        Assert.Equal("Expired", request5.Status);
        Assert.Equal("Expired", request6.Status);

        // Verify new requests created
        var newRequest5 = await _context.InterviewMatchingRequests
            .Where(r => r.UserId == users[4].user.Id && r.Status != "Expired" && r.Id != request5.Id)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync();
        var newRequest6 = await _context.InterviewMatchingRequests
            .Where(r => r.UserId == users[5].user.Id && r.Status != "Expired" && r.Id != request6.Id)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync();

        Assert.NotNull(newRequest5);
        Assert.NotNull(newRequest6);

        // Act 5: Fourth pair (users 7-8) - both confirm (should create live session)
        var request7 = matchedRequests.First(r => r.UserId == users[6].user.Id);
        var request8 = matchedRequests.First(r => r.UserId == users[7].user.Id);

        await _matchingService.ConfirmMatchAsync(request7.Id, users[6].user.Id);
        var confirm8 = await _matchingService.ConfirmMatchAsync(request8.Id, users[7].user.Id);

        Assert.True(confirm8.Completed);
        Assert.NotNull(confirm8.Session);

        var liveSession2 = await _context.LiveInterviewSessions
            .FirstOrDefaultAsync(s => s.Id == confirm8.Session!.Id);
        Assert.NotNull(liveSession2);
        Assert.Equal("InProgress", liveSession2.Status);

        // Final assertion: Should have 2 live sessions (users 1-2 and 7-8)
        var allLiveSessions = await _context.LiveInterviewSessions
            .Where(s => s.Status == "InProgress")
            .ToListAsync();
        Assert.Equal(2, allLiveSessions.Count);
    }

    [Fact]
    public async Task TenUsers_ComplexMatchingAndRematching_AllConfirmAndExpire()
    {
        // Arrange: Create 10 users
        var users = new List<(User user, ScheduledInterviewSession session)>();
        for (int i = 1; i <= 10; i++)
        {
            var level = i <= 5 ? "beginner" : "intermediate";
            var (user, session) = await CreateUserWithSession($"user{i}@test.com", level);
            users.Add((user, session));
        }

        var q1 = await CreateQuestion("Question 1", "Easy");
        var q2 = await CreateQuestion("Question 2", "Medium");
        for (int i = 0; i < 10; i++)
        {
            users[i].session.AssignedQuestionId = i < 5 ? q1.Id : q2.Id;
        }
        await _context.SaveChangesAsync();

        // Act 1: All 10 users start matching (should form 5 pairs)
        var tasks = users.Select(u => _matchingService.StartMatchingAsync(u.session.Id, u.user.Id)).ToArray();
        await Task.WhenAll(tasks);
        await Task.Delay(1000);

        var matchedRequests = await _context.InterviewMatchingRequests
            .Where(r => r.Status == "Matched")
            .ToListAsync();
        Assert.Equal(10, matchedRequests.Count);

        // Act 2: First 2 pairs (users 1-4) - all confirm (should create 2 live sessions)
        for (int i = 0; i < 4; i += 2)
        {
            var request1 = matchedRequests.First(r => r.UserId == users[i].user.Id);
            var request2 = matchedRequests.First(r => r.UserId == users[i + 1].user.Id);

            await _matchingService.ConfirmMatchAsync(request1.Id, users[i].user.Id);
            var confirm2 = await _matchingService.ConfirmMatchAsync(request2.Id, users[i + 1].user.Id);

            Assert.True(confirm2.Completed);
            Assert.NotNull(confirm2.Session);

            var liveSession = await _context.LiveInterviewSessions
                .FirstOrDefaultAsync(s => s.Id == confirm2.Session!.Id);
            Assert.NotNull(liveSession);
            Assert.Equal("InProgress", liveSession.Status);
        }

        // Act 3: Third pair (users 5-6) - one confirms, one doesn't (should expire both)
        var request5 = matchedRequests.First(r => r.UserId == users[4].user.Id);
        var request6 = matchedRequests.First(r => r.UserId == users[5].user.Id);

        await _matchingService.ConfirmMatchAsync(request5.Id, users[4].user.Id);

        request5.UpdatedAt = DateTime.UtcNow.AddSeconds(-16);
        request6.UpdatedAt = DateTime.UtcNow.AddSeconds(-16);
        await _context.SaveChangesAsync();

        await _matchingService.ExpireMatchIfNotConfirmedAsync(request5.Id, users[4].user.Id);
        await Task.Delay(2000);

        Assert.Equal("Expired", request5.Status);
        Assert.Equal("Expired", request6.Status);

        // Act 4: Fourth pair (users 7-8) - neither confirms (should expire both)
        var request7 = matchedRequests.First(r => r.UserId == users[6].user.Id);
        var request8 = matchedRequests.First(r => r.UserId == users[7].user.Id);

        request7.UpdatedAt = DateTime.UtcNow.AddSeconds(-16);
        request8.UpdatedAt = DateTime.UtcNow.AddSeconds(-16);
        await _context.SaveChangesAsync();

        await _matchingService.ExpireMatchIfNotConfirmedAsync(request7.Id, users[6].user.Id);
        await Task.Delay(2000);

        Assert.Equal("Expired", request7.Status);
        Assert.Equal("Expired", request8.Status);

        // Act 5: Fifth pair (users 9-10) - both confirm (should create live session)
        var request9 = matchedRequests.First(r => r.UserId == users[8].user.Id);
        var request10 = matchedRequests.First(r => r.UserId == users[9].user.Id);

        await _matchingService.ConfirmMatchAsync(request9.Id, users[8].user.Id);
        var confirm10 = await _matchingService.ConfirmMatchAsync(request10.Id, users[9].user.Id);

        Assert.True(confirm10.Completed);
        Assert.NotNull(confirm10.Session);

        // Final assertion: Should have 3 live sessions total
        var allLiveSessions = await _context.LiveInterviewSessions
            .Where(s => s.Status == "InProgress")
            .ToListAsync();
        Assert.Equal(3, allLiveSessions.Count);
    }
}

