using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Vector.Api.Data;
using Vector.Api.DTOs.PeerInterview;
using Vector.Api.Models;
using Vector.Api.Services;
using Xunit;

namespace Vector.Api.Tests.Services;

/// <summary>
/// Comprehensive tests for matching, rematching, and expiring scenarios with multiple users (5-10 users)
/// </summary>
public class PeerInterviewServiceRematchingTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IQuestionService> _questionServiceMock;
    private readonly Mock<ILogger<PeerInterviewService>> _loggerMock;
    private readonly PeerInterviewService _service;

    public PeerInterviewServiceRematchingTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        _questionServiceMock = new Mock<IQuestionService>();
        _loggerMock = new Mock<ILogger<PeerInterviewService>>();

        _service = new PeerInterviewService(
            _context,
            _questionServiceMock.Object,
            _loggerMock.Object
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
            ApprovalStatus = "Approved"
        };
        await _context.InterviewQuestions.AddAsync(question);
        await _context.SaveChangesAsync();
        return question;
    }

    [Fact]
    public async Task MultipleUsers_MatchingAndRematching_ComplexScenario()
    {
        // Arrange: Create 8 users with sessions
        var (user1, session1) = await CreateUserWithSession("user1@test.com", "beginner");
        var (user2, session2) = await CreateUserWithSession("user2@test.com", "beginner");
        var (user3, session3) = await CreateUserWithSession("user3@test.com", "intermediate");
        var (user4, session4) = await CreateUserWithSession("user4@test.com", "intermediate");
        var (user5, session5) = await CreateUserWithSession("user5@test.com", "beginner");
        var (user6, session6) = await CreateUserWithSession("user6@test.com", "beginner");
        var (user7, session7) = await CreateUserWithSession("user7@test.com", "advanced");
        var (user8, session8) = await CreateUserWithSession("user8@test.com", "advanced");

        // Create questions
        var q1 = await CreateQuestion("Question 1", "Easy");
        var q2 = await CreateQuestion("Question 2", "Easy");
        var q3 = await CreateQuestion("Question 3", "Medium");
        var q4 = await CreateQuestion("Question 4", "Medium");
        var q5 = await CreateQuestion("Question 5", "Hard");
        var q6 = await CreateQuestion("Question 6", "Hard");

        // Assign questions to sessions
        session1.AssignedQuestionId = q1.Id;
        session2.AssignedQuestionId = q2.Id;
        session3.AssignedQuestionId = q3.Id;
        session4.AssignedQuestionId = q4.Id;
        session5.AssignedQuestionId = q1.Id; // Same as user1
        session6.AssignedQuestionId = q2.Id; // Same as user2
        session7.AssignedQuestionId = q5.Id;
        session8.AssignedQuestionId = q6.Id;
        await _context.SaveChangesAsync();

        // Act 1: Users 1-4 start matching (should match: 1-2, 3-4)
        var result1 = await _service.StartMatchingAsync(session1.Id, user1.Id);
        var result2 = await _service.StartMatchingAsync(session2.Id, user2.Id);
        var result3 = await _service.StartMatchingAsync(session3.Id, user3.Id);
        var result4 = await _service.StartMatchingAsync(session4.Id, user4.Id);

        // Wait a bit for matching to complete
        await Task.Delay(500);

        // Assert: Users 1-2 and 3-4 should be matched
        var request1 = await _context.InterviewMatchingRequests
            .FirstOrDefaultAsync(r => r.UserId == user1.Id && r.Status == "Matched");
        var request2 = await _context.InterviewMatchingRequests
            .FirstOrDefaultAsync(r => r.UserId == user2.Id && r.Status == "Matched");
        var request3 = await _context.InterviewMatchingRequests
            .FirstOrDefaultAsync(r => r.UserId == user3.Id && r.Status == "Matched");
        var request4 = await _context.InterviewMatchingRequests
            .FirstOrDefaultAsync(r => r.UserId == user4.Id && r.Status == "Matched");

        Assert.NotNull(request1);
        Assert.NotNull(request2);
        Assert.NotNull(request3);
        Assert.NotNull(request4);
        Assert.Equal(user2.Id, request1.MatchedUserId);
        Assert.Equal(user1.Id, request2.MatchedUserId);
        Assert.Equal(user4.Id, request3.MatchedUserId);
        Assert.Equal(user3.Id, request4.MatchedUserId);
        Assert.Null(request1.LiveSessionId); // No live session until both confirm
        Assert.Null(request2.LiveSessionId);

        // Act 2: Users 1 and 2 confirm (should create live session)
        var confirm1 = await _service.ConfirmMatchAsync(request1!.Id, user1.Id);
        Assert.False(confirm1.Completed); // Not completed yet

        var confirm2 = await _service.ConfirmMatchAsync(request2!.Id, user2.Id);
        Assert.True(confirm2.Completed); // Both confirmed
        Assert.NotNull(confirm2.Session);

        // Verify live session was created with InProgress status
        var liveSession1 = await _context.LiveInterviewSessions
            .FirstOrDefaultAsync(s => s.Id == confirm2.Session!.Id);
        Assert.NotNull(liveSession1);
        Assert.Equal("InProgress", liveSession1.Status);
        Assert.NotNull(liveSession1.StartedAt);

        // Verify both requests are confirmed
        await _context.Entry(request1).ReloadAsync();
        await _context.Entry(request2).ReloadAsync();
        Assert.Equal("Confirmed", request1.Status);
        Assert.Equal("Confirmed", request2.Status);
        Assert.NotNull(request1.LiveSessionId);
        Assert.NotNull(request2.LiveSessionId);

        // Act 3: Users 3 and 4 - User 3 confirms, User 4 doesn't (expire after 15 seconds)
        var confirm3 = await _service.ConfirmMatchAsync(request3!.Id, user3.Id);
        Assert.False(confirm3.Completed);

        // Simulate 15 seconds passing
        request3.UpdatedAt = DateTime.UtcNow.AddSeconds(-16);
        request4!.UpdatedAt = DateTime.UtcNow.AddSeconds(-16);
        await _context.SaveChangesAsync();

        // Expire the match
        var expired = await _service.ExpireMatchIfNotConfirmedAsync(request3.Id, user3.Id);
        Assert.True(expired);

        // Wait for rematching to complete
        await Task.Delay(2000);

        // Assert: User 3 should be requeued (may be matched again immediately since at front)
        // User 4 should be Expired with new request created
        await _context.Entry(request3).ReloadAsync();
        await _context.Entry(request4).ReloadAsync();

        // User 3: Either still Pending (waiting) or Matched again (matched immediately at front)
        Assert.True(request3.Status == "Pending" || request3.Status == "Matched");
        if (request3.Status == "Pending")
        {
            Assert.Null(request3.MatchedUserId);
            Assert.True(request3.ExpiresAt > DateTime.UtcNow.AddMinutes(9)); // Should have 10 minutes
        }

        Assert.Equal("Expired", request4.Status); // User 4 expired

        // Verify new request was created for User 4
        // Wait a bit for async operations to complete (SaveChangesAsync should have completed, but wait for context to sync)
        await Task.Delay(2000);
        var allRequestsForUser4 = await _context.InterviewMatchingRequests
            .Where(r => r.UserId == user4.Id)
            .ToListAsync();
        // New request may be Pending (waiting) or Matched (matched immediately)
        var newRequest4 = allRequestsForUser4
            .Where(r => (r.Status == "Pending" || r.Status == "Matched") && r.Id != request4.Id)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefault();
        Assert.NotNull(newRequest4);

        // Act 4: Users 5-8 start matching
        var result5 = await _service.StartMatchingAsync(session5.Id, user5.Id);
        var result6 = await _service.StartMatchingAsync(session6.Id, user6.Id);
        var result7 = await _service.StartMatchingAsync(session7.Id, user7.Id);
        var result8 = await _service.StartMatchingAsync(session8.Id, user8.Id);

        await Task.Delay(500);

        // Assert: User 3 (requeued) should match with User 5 or 6 (beginner level)
        // User 7-8 should match (advanced level)
        var request3After = await _context.InterviewMatchingRequests
            .FirstOrDefaultAsync(r => r.UserId == user3.Id && r.Status == "Matched");
        var request5 = await _context.InterviewMatchingRequests
            .FirstOrDefaultAsync(r => r.UserId == user5.Id && r.Status == "Matched");
        var request6 = await _context.InterviewMatchingRequests
            .FirstOrDefaultAsync(r => r.UserId == user6.Id && r.Status == "Matched");
        var request7 = await _context.InterviewMatchingRequests
            .FirstOrDefaultAsync(r => r.UserId == user7.Id && r.Status == "Matched");
        var request8 = await _context.InterviewMatchingRequests
            .FirstOrDefaultAsync(r => r.UserId == user8.Id && r.Status == "Matched");

        // User 3 should be matched (requeued at front)
        Assert.NotNull(request3After);
        Assert.NotNull(request3After.MatchedUserId);
        Assert.True(request3After.ExpiresAt > DateTime.UtcNow.AddMinutes(9)); // Should have 10 minutes timer

        // Users 7-8 should be matched
        Assert.NotNull(request7);
        Assert.NotNull(request8);
        Assert.Equal(user8.Id, request7.MatchedUserId);
        Assert.Equal(user7.Id, request8.MatchedUserId);

        // Act 5: Users 7 and 8 both confirm
        var confirm7 = await _service.ConfirmMatchAsync(request7!.Id, user7.Id);
        var confirm8 = await _service.ConfirmMatchAsync(request8!.Id, user8.Id);

        Assert.True(confirm8.Completed);
        Assert.NotNull(confirm8.Session);

        var liveSession2 = await _context.LiveInterviewSessions
            .FirstOrDefaultAsync(s => s.Id == confirm8.Session!.Id);
        Assert.NotNull(liveSession2);
        Assert.Equal("InProgress", liveSession2.Status);
    }

    [Fact]
    public async Task MultipleUsers_NeitherUserConfirms_BothExpiredAndRequeued()
    {
        // Arrange: Create 4 users
        var (user1, session1) = await CreateUserWithSession("user1@test.com", "beginner");
        var (user2, session2) = await CreateUserWithSession("user2@test.com", "beginner");
        var (user3, session3) = await CreateUserWithSession("user3@test.com", "beginner");
        var (user4, session4) = await CreateUserWithSession("user4@test.com", "beginner");

        var q1 = await CreateQuestion("Question 1", "Easy");
        var q2 = await CreateQuestion("Question 2", "Easy");
        session1.AssignedQuestionId = q1.Id;
        session2.AssignedQuestionId = q2.Id;
        session3.AssignedQuestionId = q1.Id;
        session4.AssignedQuestionId = q2.Id;
        await _context.SaveChangesAsync();

        // Act 1: Users 1-2 start matching and get matched
        await _service.StartMatchingAsync(session1.Id, user1.Id);
        await _service.StartMatchingAsync(session2.Id, user2.Id);
        await Task.Delay(500);

        var request1 = await _context.InterviewMatchingRequests
            .FirstOrDefaultAsync(r => r.UserId == user1.Id && r.Status == "Matched");
        var request2 = await _context.InterviewMatchingRequests
            .FirstOrDefaultAsync(r => r.UserId == user2.Id && r.Status == "Matched");

        Assert.NotNull(request1);
        Assert.NotNull(request2);

        // Act 2: Neither user confirms - expire after 15 seconds
        request1!.UpdatedAt = DateTime.UtcNow.AddSeconds(-16);
        request2!.UpdatedAt = DateTime.UtcNow.AddSeconds(-16);
        await _context.SaveChangesAsync();

        var expired = await _service.ExpireMatchIfNotConfirmedAsync(request1.Id, user1.Id);
        Assert.True(expired);

        // Assert: Both should be Expired, new requests created
        await _context.Entry(request1).ReloadAsync();
        await _context.Entry(request2).ReloadAsync();

        Assert.Equal("Expired", request1.Status);
        Assert.Equal("Expired", request2.Status);

        // Verify new requests were created for both (may be Pending or Matched)
        // Wait a bit for async operations to complete (SaveChangesAsync should have completed, but wait for context to sync)
        await Task.Delay(2000);
        var allRequestsForUser1 = await _context.InterviewMatchingRequests
            .Where(r => r.UserId == user1.Id)
            .ToListAsync();
        var allRequestsForUser2 = await _context.InterviewMatchingRequests
            .Where(r => r.UserId == user2.Id)
            .ToListAsync();
        var newRequest1 = allRequestsForUser1
            .Where(r => (r.Status == "Pending" || r.Status == "Matched") && r.Id != request1.Id)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefault();
        var newRequest2 = allRequestsForUser2
            .Where(r => (r.Status == "Pending" || r.Status == "Matched") && r.Id != request2.Id)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefault();

        Assert.NotNull(newRequest1);
        Assert.NotNull(newRequest2);
        Assert.True(newRequest1.ExpiresAt > DateTime.UtcNow.AddMinutes(9));
        Assert.True(newRequest2.ExpiresAt > DateTime.UtcNow.AddMinutes(9));

        // Act 3: Users 3-4 start matching, should match with new requests from users 1-2
        await _service.StartMatchingAsync(session3.Id, user3.Id);
        await _service.StartMatchingAsync(session4.Id, user4.Id);
        await Task.Delay(500);

        // Verify rematching occurred
        var finalRequest1 = await _context.InterviewMatchingRequests
            .FirstOrDefaultAsync(r => r.UserId == user1.Id && r.Status == "Matched");
        var finalRequest2 = await _context.InterviewMatchingRequests
            .FirstOrDefaultAsync(r => r.UserId == user2.Id && r.Status == "Matched");

        // Users 1-2 should be matched again (with users 3-4 or each other)
        Assert.NotNull(finalRequest1);
        Assert.NotNull(finalRequest2);
    }

    [Fact]
    public async Task MultipleUsers_OneUserConfirms_ConfirmedUserRequeuedAtFront()
    {
        // Arrange: Create 6 users
        var (user1, session1) = await CreateUserWithSession("user1@test.com", "beginner");
        var (user2, session2) = await CreateUserWithSession("user2@test.com", "beginner");
        var (user3, session3) = await CreateUserWithSession("user3@test.com", "beginner");
        var (user4, session4) = await CreateUserWithSession("user4@test.com", "beginner");
        var (user5, session5) = await CreateUserWithSession("user5@test.com", "beginner");
        var (user6, session6) = await CreateUserWithSession("user6@test.com", "beginner");

        var q1 = await CreateQuestion("Question 1", "Easy");
        session1.AssignedQuestionId = q1.Id;
        session2.AssignedQuestionId = q1.Id;
        session3.AssignedQuestionId = q1.Id;
        session4.AssignedQuestionId = q1.Id;
        session5.AssignedQuestionId = q1.Id;
        session6.AssignedQuestionId = q1.Id;
        await _context.SaveChangesAsync();

        // Act 1: Users 1-2 match
        await _service.StartMatchingAsync(session1.Id, user1.Id);
        await _service.StartMatchingAsync(session2.Id, user2.Id);
        await Task.Delay(500);

        var request1 = await _context.InterviewMatchingRequests
            .FirstOrDefaultAsync(r => r.UserId == user1.Id && r.Status == "Matched");
        var request2 = await _context.InterviewMatchingRequests
            .FirstOrDefaultAsync(r => r.UserId == user2.Id && r.Status == "Matched");

        // Act 2: User 1 confirms, User 2 doesn't
        await _service.ConfirmMatchAsync(request1!.Id, user1.Id);

        // Simulate 15 seconds passing
        request1.UpdatedAt = DateTime.UtcNow.AddSeconds(-16);
        request2!.UpdatedAt = DateTime.UtcNow.AddSeconds(-16);
        await _context.SaveChangesAsync();

        // Expire
        var expired = await _service.ExpireMatchIfNotConfirmedAsync(request1.Id, user1.Id);
        Assert.True(expired);

        // Wait for rematching to complete
        await Task.Delay(2000);

        // Assert: User 1 should be requeued (may be matched again immediately since at front)
        // User 2 should be Expired
        await _context.Entry(request1).ReloadAsync();
        await _context.Entry(request2).ReloadAsync();

        // User 1: Either still Pending (waiting) or Matched again (matched immediately at front)
        Assert.True(request1.Status == "Pending" || request1.Status == "Matched");
        if (request1.Status == "Pending")
        {
            Assert.Null(request1.MatchedUserId);
            Assert.True(request1.ExpiresAt > DateTime.UtcNow.AddMinutes(9)); // 10 minutes timer restarted
        }

        Assert.Equal("Expired", request2.Status);

        // Act 3: Users 3-4 start matching
        await _service.StartMatchingAsync(session3.Id, user3.Id);
        await _service.StartMatchingAsync(session4.Id, user4.Id);
        await Task.Delay(500);

        // Assert: User 1 (requeued at front) should match immediately
        var request1After = await _context.InterviewMatchingRequests
            .FirstOrDefaultAsync(r => r.UserId == user1.Id && r.Status == "Matched");
        Assert.NotNull(request1After);
        Assert.NotNull(request1After.MatchedUserId);
    }

    [Fact]
    public async Task TenUsers_ComplexMatchingAndRematching_AllScenarios()
    {
        // Arrange: Create 10 users
        var users = new List<(User user, ScheduledInterviewSession session)>();
        for (int i = 1; i <= 10; i++)
        {
            var level = i <= 3 ? "beginner" : i <= 6 ? "intermediate" : "advanced";
            var (user, session) = await CreateUserWithSession($"user{i}@test.com", level);
            users.Add((user, session));
        }

        // Create questions
        var questions = new List<InterviewQuestion>();
        for (int i = 1; i <= 10; i++)
        {
            var difficulty = i <= 3 ? "Easy" : i <= 6 ? "Medium" : "Hard";
            var q = await CreateQuestion($"Question {i}", difficulty);
            questions.Add(q);
            users[i - 1].session.AssignedQuestionId = q.Id;
        }
        await _context.SaveChangesAsync();

        // Act 1: All 10 users start matching
        var tasks = users.Select(u => _service.StartMatchingAsync(u.session.Id, u.user.Id)).ToArray();
        await Task.WhenAll(tasks);
        await Task.Delay(1000); // Wait for matching

        // Assert: Should have 5 pairs matched
        var matchedRequests = await _context.InterviewMatchingRequests
            .Where(r => r.Status == "Matched")
            .ToListAsync();
        Assert.Equal(10, matchedRequests.Count); // 10 users = 5 pairs

        // Act 2: First pair (users 1-2) both confirm
        var request1 = matchedRequests.First(r => r.UserId == users[0].user.Id);
        var request2 = matchedRequests.First(r => r.UserId == users[1].user.Id);

        await _service.ConfirmMatchAsync(request1.Id, users[0].user.Id);
        var confirm2 = await _service.ConfirmMatchAsync(request2.Id, users[1].user.Id);

        Assert.True(confirm2.Completed);
        Assert.NotNull(confirm2.Session);

        // Act 3: Second pair (users 3-4) - User 3 confirms, User 4 doesn't
        var request3 = matchedRequests.First(r => r.UserId == users[2].user.Id);
        var request4 = matchedRequests.First(r => r.UserId == users[3].user.Id);

        await _service.ConfirmMatchAsync(request3.Id, users[2].user.Id);

        // Expire
        request3.UpdatedAt = DateTime.UtcNow.AddSeconds(-16);
        request4.UpdatedAt = DateTime.UtcNow.AddSeconds(-16);
        await _context.SaveChangesAsync();

        await _service.ExpireMatchIfNotConfirmedAsync(request3.Id, users[2].user.Id);

        // Act 4: Third pair (users 5-6) - Neither confirms
        var request5 = matchedRequests.First(r => r.UserId == users[4].user.Id);
        var request6 = matchedRequests.First(r => r.UserId == users[5].user.Id);

        request5.UpdatedAt = DateTime.UtcNow.AddSeconds(-16);
        request6.UpdatedAt = DateTime.UtcNow.AddSeconds(-16);
        await _context.SaveChangesAsync();

        await _service.ExpireMatchIfNotConfirmedAsync(request5.Id, users[4].user.Id);

        // Wait for rematching to complete
        await Task.Delay(2000);

        // Assert: Verify final state
        await _context.Entry(request3).ReloadAsync();
        await _context.Entry(request4).ReloadAsync();
        await _context.Entry(request5).ReloadAsync();
        await _context.Entry(request6).ReloadAsync();

        // User 3 should be requeued (may be matched again immediately since at front)
        Assert.True(request3.Status == "Pending" || request3.Status == "Matched");
        if (request3.Status == "Pending")
        {
            Assert.Null(request3.MatchedUserId);
        }

        // User 4 should be Expired
        Assert.Equal("Expired", request4.Status);

        // Users 5-6 should both be Expired
        Assert.Equal("Expired", request5.Status);
        Assert.Equal("Expired", request6.Status);

        // Verify new requests were created for expired users (may be Pending or Matched)
        // Wait a bit for async operations to complete (SaveChangesAsync should have completed, but wait for context to sync)
        await Task.Delay(2000);
        var allRequestsForUser4 = await _context.InterviewMatchingRequests
            .Where(r => r.UserId == users[3].user.Id)
            .ToListAsync();
        var allRequestsForUser5 = await _context.InterviewMatchingRequests
            .Where(r => r.UserId == users[4].user.Id)
            .ToListAsync();
        var allRequestsForUser6 = await _context.InterviewMatchingRequests
            .Where(r => r.UserId == users[5].user.Id)
            .ToListAsync();
        var newRequest4 = allRequestsForUser4
            .Where(r => (r.Status == "Pending" || r.Status == "Matched") && r.Id != request4.Id)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefault();
        var newRequest5 = allRequestsForUser5
            .Where(r => (r.Status == "Pending" || r.Status == "Matched") && r.Id != request5.Id)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefault();
        var newRequest6 = allRequestsForUser6
            .Where(r => (r.Status == "Pending" || r.Status == "Matched") && r.Id != request6.Id)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefault();

        Assert.NotNull(newRequest4);
        Assert.NotNull(newRequest5);
        Assert.NotNull(newRequest6);

        // Verify live session count (should be 1 - only for users 1-2)
        var liveSessions = await _context.LiveInterviewSessions
            .Where(s => s.Status == "InProgress")
            .ToListAsync();
        Assert.Single(liveSessions);
    }
}
