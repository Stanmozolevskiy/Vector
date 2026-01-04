using Microsoft.EntityFrameworkCore;
using Vector.Api.Data;
using Vector.Api.DTOs.PeerInterview;
using Vector.Api.Models;

namespace Vector.Api.Services;

/// <summary>
/// Service for interview matching functionality
/// Handles matching users for peer interviews
/// </summary>
public class InterviewMatchingService : IInterviewMatchingService
{
    private readonly ApplicationDbContext _context;
    private readonly IPeerInterviewService _peerInterviewService;
    private readonly IMatchingPresenceService _presenceService;
    private readonly ILogger<InterviewMatchingService> _logger;
    private static readonly Random _random = new Random();

    public InterviewMatchingService(
        ApplicationDbContext context,
        IPeerInterviewService peerInterviewService,
        IMatchingPresenceService presenceService,
        ILogger<InterviewMatchingService> logger)
    {
        _context = context;
        _peerInterviewService = peerInterviewService;
        _presenceService = presenceService;
        _logger = logger;
    }

    public async Task<StartMatchingResponseDto> StartMatchingAsync(Guid scheduledSessionId, Guid userId)
    {
        var scheduledSession = await _context.ScheduledInterviewSessions
            .FirstOrDefaultAsync(s => s.Id == scheduledSessionId && s.UserId == userId);

        if (scheduledSession == null)
        {
            throw new KeyNotFoundException("Scheduled session not found.");
        }

        if (scheduledSession.Status == "Cancelled")
        {
            throw new InvalidOperationException("Cannot start matching for a cancelled session.");
        }

        // Check if there's already a matching request for this session
        var existingRequest = await _context.InterviewMatchingRequests
            .FirstOrDefaultAsync(m => m.ScheduledSessionId == scheduledSessionId 
                && m.Status != "Expired" 
                && m.Status != "Cancelled"
                && m.Status != "Confirmed"); // Don't reuse confirmed requests

        if (existingRequest != null)
        {
            // If already confirmed with live session, return it
            if (existingRequest.Status == "Confirmed" && existingRequest.LiveSessionId.HasValue)
            {
                var liveSession = await _peerInterviewService.GetLiveSessionByIdAsync(existingRequest.LiveSessionId.Value, userId);
                if (liveSession != null)
                {
                    return new StartMatchingResponseDto
                    {
                        MatchingRequest = await MapToMatchingRequestDtoAsync(existingRequest),
                        Matched = true,
                        SessionComplete = true,
                        Session = liveSession
                    };
                }
            }

            // Return existing request (might be Pending, Matched, or Confirmed)
            // Note: Matched status means waiting for confirmation, no LiveSessionId yet
            return new StartMatchingResponseDto
            {
                MatchingRequest = await MapToMatchingRequestDtoAsync(existingRequest),
                Matched = existingRequest.Status == "Matched" || existingRequest.Status == "Confirmed"
            };
        }

        // CRITICAL: Only create new matching request if user is actively on the matching modal (presence check)
        if (!_presenceService.IsUserActive(userId, scheduledSessionId))
        {
            _logger.LogWarning("User {UserId} attempted to create matching request for session {SessionId} but is not present in matching modal", 
                userId, scheduledSessionId);
            throw new InvalidOperationException("Cannot create matching request. User must be on the 'Waiting for your partner...' page.");
        }

        // Create new matching request - user joins the queue
        var matchingRequest = new InterviewMatchingRequest
        {
            UserId = userId,
            ScheduledSessionId = scheduledSessionId,
            InterviewType = scheduledSession.InterviewType,
            PracticeType = scheduledSession.PracticeType,
            InterviewLevel = scheduledSession.InterviewLevel,
            ScheduledStartAt = scheduledSession.ScheduledStartAt,
            Status = "Pending", // User is in queue
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.InterviewMatchingRequests.Add(matchingRequest);
        await _context.SaveChangesAsync();

        _logger.LogInformation("INTERVIEW_STATUS: User {UserId} - StartInterview clicked and Finding partner -> Looking For Partner", userId);

        // Try to match immediately (might find a match right away)
        var isMatched = await TryMatchAsync(matchingRequest);

        // Refresh the request to get updated status
        await _context.Entry(matchingRequest).ReloadAsync();

        // LiveSessionId will be null until both users confirm
        // Only return session if status is Confirmed (both users confirmed)
        LiveInterviewSessionDto? sessionDto = null;
        if (matchingRequest.Status == "Confirmed" && matchingRequest.LiveSessionId.HasValue)
        {
            sessionDto = await _peerInterviewService.GetLiveSessionByIdAsync(matchingRequest.LiveSessionId.Value, userId);
        }

        return new StartMatchingResponseDto
        {
            MatchingRequest = await MapToMatchingRequestDtoAsync(matchingRequest),
            Matched = isMatched,
            Session = sessionDto
        };
    }

    public async Task<MatchingRequestDto?> GetMatchingStatusAsync(Guid scheduledSessionId, Guid userId)
    {
        // Log all requests in queue for debugging
        var allRequests = await _context.InterviewMatchingRequests
            .Where(r => r.ScheduledSessionId == scheduledSessionId && r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
        
        _logger.LogInformation("GET_MATCHING_STATUS: UserId={UserId}, SessionId={SessionId}, Total requests={Count}", 
            userId, scheduledSessionId, allRequests.Count);
        foreach (var req in allRequests)
        {
            _logger.LogInformation("GET_MATCHING_STATUS REQUEST: RequestId={RequestId}, Status={Status}, CreatedAt={CreatedAt}, ExpiresAt={ExpiresAt}", 
                req.Id, req.Status, req.CreatedAt, req.ExpiresAt);
        }
        
        // Log all pending requests in the entire queue for this scheduled session
        var allPendingInQueue = await _context.InterviewMatchingRequests
            .Where(r => r.ScheduledSessionId == scheduledSessionId 
                && r.Status == "Pending" 
                && r.ExpiresAt > DateTime.UtcNow)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync();
        _logger.LogInformation("GET_MATCHING_STATUS: Total pending requests in queue for SessionId={SessionId}: {Count}", 
            scheduledSessionId, allPendingInQueue.Count);
        foreach (var req in allPendingInQueue)
        {
            _logger.LogInformation("GET_MATCHING_STATUS QUEUE: RequestId={RequestId}, UserId={UserId}, CreatedAt={CreatedAt}, InterviewType={InterviewType}, Level={Level}", 
                req.Id, req.UserId, req.CreatedAt, req.InterviewType, req.InterviewLevel);
        }
        
        var matchingRequest = await _context.InterviewMatchingRequests
            .Include(m => m.User)
            .Include(m => m.MatchedUser)
            .Include(m => m.ScheduledSession)
            .FirstOrDefaultAsync(m => m.ScheduledSessionId == scheduledSessionId 
                && m.UserId == userId
                && m.Status != "Expired"
                && m.Status != "Cancelled");

        if (matchingRequest == null)
        {
            _logger.LogInformation("GET_MATCHING_STATUS: No active matching request found for UserId={UserId}, SessionId={SessionId}", 
                userId, scheduledSessionId);
            return null;
        }
        
        // If the request is Pending, try to match it
        if (matchingRequest.Status == "Pending" && matchingRequest.ExpiresAt > DateTime.UtcNow)
        {
            var requestId = matchingRequest.Id;
            _logger.LogInformation("GET_MATCHING_STATUS: Found pending request, attempting to match. RequestId={RequestId}, UserId={UserId}", 
                requestId, userId);
            // Try to match this pending request
            await TryMatchAsync(matchingRequest);
            
            // Reload with includes to get updated status (may now be Matched)
            matchingRequest = await _context.InterviewMatchingRequests
                .Include(m => m.User)
                .Include(m => m.MatchedUser)
                .Include(m => m.ScheduledSession)
                .FirstOrDefaultAsync(m => m.Id == requestId);
            
            if (matchingRequest == null)
            {
                _logger.LogWarning("GET_MATCHING_STATUS: Request disappeared after matching attempt. RequestId={RequestId}", requestId);
                return null;
            }
            
            _logger.LogInformation("GET_MATCHING_STATUS: After matching attempt, status is {Status}. RequestId={RequestId}", 
                matchingRequest.Status, matchingRequest.Id);
        }

        // Check if expired
        if (matchingRequest.ExpiresAt < DateTime.UtcNow && matchingRequest.Status == "Pending")
        {
            _logger.LogInformation("GET_MATCHING_STATUS: Request expired, marking as Expired. RequestId={RequestId}", 
                matchingRequest.Id);
            matchingRequest.Status = "Expired";
            await _context.SaveChangesAsync();
            return null;
        }

        _logger.LogInformation("GET_MATCHING_STATUS: Returning request. RequestId={RequestId}, Status={Status}", 
            matchingRequest.Id, matchingRequest.Status);
        return await MapToMatchingRequestDtoAsync(matchingRequest);
    }

    public async Task<ConfirmMatchResponseDto> ConfirmMatchAsync(Guid matchingRequestId, Guid userId)
    {
        _logger.LogInformation("ConfirmMatchAsync called for MatchingRequestId: {MatchingRequestId}, UserId: {UserId}", matchingRequestId, userId);

        // Find the matching request - user confirms on their OWN request
        var matchingRequest = await _context.InterviewMatchingRequests
            .Include(m => m.User)
            .Include(m => m.MatchedUser)
            .Include(m => m.ScheduledSession)
            .FirstOrDefaultAsync(m => m.Id == matchingRequestId && m.UserId == userId);

        if (matchingRequest == null)
        {
            _logger.LogWarning("Matching request not found or user mismatch: {MatchingRequestId}, UserId: {UserId}", matchingRequestId, userId);
            throw new KeyNotFoundException("Matching request not found.");
        }

        // Must be matched (live session will be created after both confirm)
        if (matchingRequest.Status != "Matched")
        {
            _logger.LogWarning("Cannot confirm - Status: {Status}", matchingRequest.Status);
            throw new InvalidOperationException("Match not ready for confirmation.");
        }

        // Mark user as confirmed
        if (matchingRequest.UserConfirmed)
        {
            _logger.LogInformation("User {UserId} already confirmed", userId);
        }
        else
        {
            matchingRequest.UserConfirmed = true;
            matchingRequest.UpdatedAt = DateTime.UtcNow;
            _logger.LogInformation("User {UserId} confirmed", userId);
        }

        // Find the other user's matching request to check if they confirmed
        InterviewMatchingRequest? otherUserRequest = null;
        bool bothConfirmed = false;
        
        if (matchingRequest.MatchedUserId.HasValue)
        {
            otherUserRequest = await _context.InterviewMatchingRequests
                .FirstOrDefaultAsync(m => m.UserId == matchingRequest.MatchedUserId.Value 
                    && m.MatchedUserId == matchingRequest.UserId 
                    && m.Status == "Matched");

            if (otherUserRequest != null)
            {
                bothConfirmed = matchingRequest.UserConfirmed && otherUserRequest.UserConfirmed;
                _logger.LogInformation("Both confirmed check - ThisUser: {ThisConfirmed}, OtherUser: {OtherConfirmed}, BothConfirmed: {BothConfirmed}",
                    matchingRequest.UserConfirmed, otherUserRequest.UserConfirmed, bothConfirmed);
            }
        }

        // If both confirmed, CREATE live session with InProgress status immediately
        if (bothConfirmed)
        {
            // Create live session NOW (only after both users confirm)
            var liveSession = await CreateLiveSessionForMatchAsync(matchingRequest, otherUserRequest!);
            
            // Set status to InProgress immediately (not Pending)
            liveSession.Status = "InProgress";
            liveSession.StartedAt = DateTime.UtcNow;
            liveSession.UpdatedAt = DateTime.UtcNow;
            
            // Update both matching requests to Confirmed and set LiveSessionId
            matchingRequest.Status = "Confirmed";
            matchingRequest.LiveSessionId = liveSession.Id;
            if (otherUserRequest != null)
            {
                otherUserRequest.Status = "Confirmed";
                otherUserRequest.LiveSessionId = liveSession.Id;
                otherUserRequest.UpdatedAt = DateTime.UtcNow;
            }
            
            // Update scheduled session
            var scheduledSession = await _context.ScheduledInterviewSessions
                .FirstOrDefaultAsync(s => s.Id == matchingRequest.ScheduledSessionId);
            if (scheduledSession != null)
            {
                scheduledSession.Status = "InProgress";
                scheduledSession.UpdatedAt = DateTime.UtcNow;
            }
            
            _logger.LogInformation("INTERVIEW_STATUS: User {UserId1} and User {UserId2} - Both users confirm match -> In Live Session", matchingRequest.UserId, otherUserRequest!.UserId);
            _logger.LogInformation("Both users confirmed! Live session {LiveSessionId} created and started with InProgress status", liveSession.Id);
        }

        await _context.SaveChangesAsync();

        var liveSessionDto = matchingRequest.LiveSessionId.HasValue
            ? await _peerInterviewService.GetLiveSessionByIdAsync(matchingRequest.LiveSessionId.Value, userId)
            : null;

        return new ConfirmMatchResponseDto
        {
            MatchingRequest = await MapToMatchingRequestDtoAsync(matchingRequest),
            Completed = bothConfirmed,
            Session = liveSessionDto
        };
    }

    public async Task<bool> ExpireMatchIfNotConfirmedAsync(Guid matchingRequestId, Guid userId)
    {
        var matchingRequest = await _context.InterviewMatchingRequests
            .Include(m => m.MatchedUser)
            .FirstOrDefaultAsync(m => m.Id == matchingRequestId && m.UserId == userId);

        if (matchingRequest == null || matchingRequest.Status != "Matched")
        {
            return false; // Already handled or not matched
        }

        // Check if match was created more than 15 seconds ago
        var matchAge = DateTime.UtcNow - matchingRequest.UpdatedAt;
        if (matchAge.TotalSeconds < 15)
        {
            return false; // Not expired yet
        }

        // Check if both users confirmed
        InterviewMatchingRequest? otherUserRequest = null;
        bool bothConfirmed = false;
        
        if (matchingRequest.MatchedUserId.HasValue)
        {
            otherUserRequest = await _context.InterviewMatchingRequests
                .FirstOrDefaultAsync(m => m.UserId == matchingRequest.MatchedUserId.Value 
                    && m.MatchedUserId == matchingRequest.UserId 
                    && m.Status == "Matched");

            if (otherUserRequest != null)
            {
                bothConfirmed = matchingRequest.UserConfirmed && otherUserRequest.UserConfirmed;
            }
        }

        if (bothConfirmed)
        {
            return false; // Both confirmed, don't expire
        }

        _logger.LogInformation("Match expired - User {UserId1} and User {UserId2} did not both confirm within 15 seconds", 
            matchingRequest.UserId, matchingRequest.MatchedUserId);

        // Expire both users' requests
        matchingRequest.Status = "Expired";
        matchingRequest.UpdatedAt = DateTime.UtcNow;
        
        if (otherUserRequest != null)
        {
            otherUserRequest.Status = "Expired";
            otherUserRequest.UpdatedAt = DateTime.UtcNow;
        }
        
        // Save expiration changes first
        await _context.SaveChangesAsync();
        
        // Check if users are still active and re-queue them if they are
        var user1IsActive = _presenceService.IsUserActive(matchingRequest.UserId, matchingRequest.ScheduledSessionId);
        
        Guid? newRequest1Id = null;
        if (user1IsActive)
        {
            // User 1 is still active - create new request and try to match
            var newRequest1 = new InterviewMatchingRequest
            {
                UserId = matchingRequest.UserId,
                ScheduledSessionId = matchingRequest.ScheduledSessionId,
                InterviewType = matchingRequest.InterviewType,
                PracticeType = matchingRequest.PracticeType,
                InterviewLevel = matchingRequest.InterviewLevel,
                ScheduledStartAt = matchingRequest.ScheduledStartAt,
                Status = "Pending",
                ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.InterviewMatchingRequests.Add(newRequest1);
            await _context.SaveChangesAsync();
            newRequest1Id = newRequest1.Id;
            
            // Try to match immediately
            await TryMatchAsync(newRequest1);
            await _context.Entry(newRequest1).ReloadAsync();
            
            _logger.LogInformation("REQUEUE_USER1: User {UserId} re-queued after expiration. New RequestId={RequestId}, Status={Status}", 
                matchingRequest.UserId, newRequest1.Id, newRequest1.Status);
        }
        else
        {
            _logger.LogInformation("NO_REQUEUE_USER1: User {UserId} not active, not re-queued after expiration.", matchingRequest.UserId);
        }
        
        Guid? newRequest2Id = null;
        if (otherUserRequest != null)
        {
            var user2IsActive = _presenceService.IsUserActive(otherUserRequest.UserId, otherUserRequest.ScheduledSessionId);
            
            if (user2IsActive)
            {
                // User 2 is still active - create new request and try to match
                var newRequest2 = new InterviewMatchingRequest
                {
                    UserId = otherUserRequest.UserId,
                    ScheduledSessionId = otherUserRequest.ScheduledSessionId,
                    InterviewType = otherUserRequest.InterviewType,
                    PracticeType = otherUserRequest.PracticeType,
                    InterviewLevel = otherUserRequest.InterviewLevel,
                    ScheduledStartAt = otherUserRequest.ScheduledStartAt,
                    Status = "Pending",
                    ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.InterviewMatchingRequests.Add(newRequest2);
                await _context.SaveChangesAsync();
                newRequest2Id = newRequest2.Id;
                
                // Try to match immediately
                await TryMatchAsync(newRequest2);
                await _context.Entry(newRequest2).ReloadAsync();
                
                _logger.LogInformation("REQUEUE_USER2: User {UserId} re-queued after expiration. New RequestId={RequestId}, Status={Status}", 
                    otherUserRequest.UserId, newRequest2.Id, newRequest2.Status);
            }
            else
            {
                _logger.LogInformation("NO_REQUEUE_USER2: User {UserId} not active, not re-queued after expiration.", otherUserRequest.UserId);
            }
        }
        
        // Log all pending requests in the queue after expiration
        var allPendingRequests = await _context.InterviewMatchingRequests
            .Where(r => r.Status == "Pending" && r.ExpiresAt > DateTime.UtcNow)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync();
        
        _logger.LogInformation("QUEUE STATUS AFTER EXPIRATION: Total pending requests: {Count}", allPendingRequests.Count);
        foreach (var req in allPendingRequests)
        {
            _logger.LogInformation("QUEUE REQUEST: UserId={UserId}, RequestId={RequestId}, CreatedAt={CreatedAt}, InterviewType={InterviewType}, Level={Level}", 
                req.UserId, req.Id, req.CreatedAt, req.InterviewType, req.InterviewLevel);
        }
        
        _logger.LogInformation("Expiration complete. User1 re-queued: {User1Requeued} (RequestId={RequestId1}), User2 re-queued: {User2Requeued} (RequestId={RequestId2})", 
            newRequest1Id.HasValue, newRequest1Id, newRequest2Id.HasValue, newRequest2Id);

        return true;
    }

    #region Private Helper Methods

    private async Task<bool> TryMatchAsync(InterviewMatchingRequest request)
    {
        // Clean up expired requests first
        await CleanupExpiredRequestsAsync();

        // Find matching requests (FIFO - oldest first)
        var potentialMatches = await _context.InterviewMatchingRequests
            .Where(m => m.Id != request.Id
                && m.Status == "Pending"
                && m.ExpiresAt > DateTime.UtcNow
                && m.InterviewType == request.InterviewType // Hard match
                && m.PracticeType == request.PracticeType // Hard match
                && m.ScheduledStartAt.Date == request.ScheduledStartAt.Date // Same day
                && m.UserId != request.UserId) // Not the same user
            .OrderBy(m => m.CreatedAt) // FIFO
            .ToListAsync();

        // Try to find best match (prefer same level, but accept any)
        InterviewMatchingRequest? bestMatch = null;

        // First try exact level match
        bestMatch = potentialMatches.FirstOrDefault(m => m.InterviewLevel == request.InterviewLevel);
        
        // If no exact match, take any match (soft match on level)
        if (bestMatch == null)
        {
            bestMatch = potentialMatches.FirstOrDefault();
        }

        if (bestMatch != null)
        {
            _logger.LogInformation("Match found! User {UserId1} matched with User {UserId2}", request.UserId, bestMatch.UserId);
            
            // Match the requests - LiveSession will be created ONLY after both users confirm
            // Update both matching requests
            request.MatchedUserId = bestMatch.UserId;
            bestMatch.MatchedUserId = request.UserId;
            request.Status = "Matched";
            bestMatch.Status = "Matched";
            request.LiveSessionId = null; // Will be set when both users confirm
            bestMatch.LiveSessionId = null; // Will be set when both users confirm
            request.UserConfirmed = false;
            bestMatch.UserConfirmed = false;
            request.UpdatedAt = DateTime.UtcNow;
            bestMatch.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            
            _logger.LogInformation("INTERVIEW_STATUS: User {UserId1} and User {UserId2} - Match found, 15 seconds confirmation window -> waiting for confirmation", request.UserId, bestMatch.UserId);
            _logger.LogInformation("Users {UserId1} and {UserId2} matched. Waiting for both to confirm readiness.", 
                request.UserId, bestMatch.UserId);
            
            return true;
        }

        return false;
    }
    
    private async Task<LiveInterviewSession> CreateLiveSessionForMatchAsync(
        InterviewMatchingRequest request1, 
        InterviewMatchingRequest request2)
    {
        // Determine which user is the scheduler (Interviewer)
        // The user who scheduled first (older CreatedAt) is the Interviewer
        var schedulerRequest = request1.CreatedAt < request2.CreatedAt ? request1 : request2;
        var matchedRequest = schedulerRequest.Id == request1.Id ? request2 : request1;
        
        var schedulerUserId = schedulerRequest.UserId;
        var matchedUserId = matchedRequest.UserId;
        
        _logger.LogInformation("Creating live session - Scheduler (Interviewer): {SchedulerId}, Matched User (Interviewee): {MatchedId}", 
            schedulerUserId, matchedUserId);

        // Get both scheduled sessions to access assigned questions
        var schedulerSession = await _context.ScheduledInterviewSessions
            .Include(s => s.AssignedQuestion)
            .FirstOrDefaultAsync(s => s.Id == schedulerRequest.ScheduledSessionId);
        var matchedSession = await _context.ScheduledInterviewSessions
            .Include(s => s.AssignedQuestion)
            .FirstOrDefaultAsync(s => s.Id == matchedRequest.ScheduledSessionId);

        Guid? firstQuestionId = null;
        Guid? secondQuestionId = null;

        // Use assigned questions from both users if available (for data-structures-algorithms)
        if (schedulerRequest.InterviewType == "data-structures-algorithms" 
            && schedulerSession?.AssignedQuestionId.HasValue == true 
            && matchedSession?.AssignedQuestionId.HasValue == true)
        {
            firstQuestionId = schedulerSession.AssignedQuestionId;
            secondQuestionId = matchedSession.AssignedQuestionId;

            // If both users have the same question, select a different one with the same difficulty
            if (firstQuestionId == secondQuestionId && schedulerSession.AssignedQuestion != null)
            {
                var difficulty = schedulerSession.AssignedQuestion.Difficulty;
                secondQuestionId = await SelectRandomQuestionByDifficultyAsync(
                    schedulerRequest.InterviewType, 
                    difficulty, 
                    firstQuestionId);
                
                _logger.LogInformation("Both users had same question {QuestionId}, selected different question {NewQuestionId} with difficulty {Difficulty}",
                    firstQuestionId, secondQuestionId, difficulty);
            }
        }
        else
        {
            // Fallback to random selection if assigned questions not available
            firstQuestionId = await SelectRandomQuestionAsync(schedulerRequest.InterviewType);
            secondQuestionId = await SelectRandomQuestionAsync(schedulerRequest.InterviewType, firstQuestionId);
        }

        if (!firstQuestionId.HasValue)
        {
            throw new InvalidOperationException("Could not find questions for the interview type.");
        }

        // Create live session (this is called AFTER both users confirm)
        var liveSession = new LiveInterviewSession
        {
            Id = Guid.NewGuid(),
            ScheduledSessionId = schedulerRequest.ScheduledSessionId,
            FirstQuestionId = firstQuestionId.Value,
            SecondQuestionId = secondQuestionId,
            ActiveQuestionId = firstQuestionId.Value, // Start with first question
            Status = "InProgress", // Set to InProgress immediately (both users already confirmed)
            StartedAt = DateTime.UtcNow, // Set immediately
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.LiveInterviewSessions.Add(liveSession);

        // Create participants with roles
        var schedulerParticipant = new LiveInterviewParticipant
        {
            Id = Guid.NewGuid(),
            LiveSessionId = liveSession.Id,
            UserId = schedulerUserId,
            Role = "Interviewer", // Scheduler is always Interviewer
            IsActive = true,
            JoinedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var matchedParticipant = new LiveInterviewParticipant
        {
            Id = Guid.NewGuid(),
            LiveSessionId = liveSession.Id,
            UserId = matchedUserId,
            Role = "Interviewee", // Matched user is always Interviewee initially
            IsActive = true,
            JoinedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.LiveInterviewParticipants.Add(schedulerParticipant);
        _context.LiveInterviewParticipants.Add(matchedParticipant);

        // Update scheduled session status
        var scheduledSession = await _context.ScheduledInterviewSessions
            .FirstOrDefaultAsync(s => s.Id == schedulerRequest.ScheduledSessionId);
        if (scheduledSession != null)
        {
            scheduledSession.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return liveSession;
    }

    private async Task<Guid?> SelectRandomQuestionAsync(string interviewType, Guid? excludeQuestionId = null)
    {
        // Map interview types to question types
        var questionTypeMap = new Dictionary<string, string>
        {
            { "data-structures-algorithms", "Coding" },
            { "system-design", "System Design" },
            { "behavioral", "Behavioral" },
            { "product-management", "Behavioral" },
            { "sql", "Coding" },
            { "data-science-ml", "Coding" }
        };

        var questionType = questionTypeMap.GetValueOrDefault(interviewType, "Coding");

        // Get random approved question of the appropriate type
        var query = _context.InterviewQuestions
            .Where(q => q.IsActive 
                && q.ApprovalStatus == "Approved"
                && q.QuestionType == questionType);
        
        // Exclude a question if specified (for second question)
        if (excludeQuestionId.HasValue)
        {
            query = query.Where(q => q.Id != excludeQuestionId.Value);
        }
        
        var questions = await query.ToListAsync();

        if (!questions.Any())
        {
            // Fallback to any approved question
            var fallbackQuery = _context.InterviewQuestions
                .Where(q => q.IsActive && q.ApprovalStatus == "Approved");
            
            if (excludeQuestionId.HasValue)
            {
                fallbackQuery = fallbackQuery.Where(q => q.Id != excludeQuestionId.Value);
            }
            
            questions = await fallbackQuery.ToListAsync();
        }

        if (!questions.Any())
        {
            return null;
        }

        var randomQuestion = questions[_random.Next(questions.Count)];
        return randomQuestion.Id;
    }

    private async Task<Guid?> SelectRandomQuestionByDifficultyAsync(string interviewType, string difficulty, Guid? excludeQuestionId = null)
    {
        // Map interview types to question types
        var questionTypeMap = new Dictionary<string, string>
        {
            { "data-structures-algorithms", "Coding" },
            { "system-design", "System Design" },
            { "behavioral", "Behavioral" },
            { "product-management", "Behavioral" },
            { "sql", "Coding" },
            { "data-science-ml", "Coding" }
        };

        var questionType = questionTypeMap.GetValueOrDefault(interviewType, "Coding");

        // Get random approved question with specific difficulty
        var query = _context.InterviewQuestions
            .Where(q => q.IsActive 
                && q.ApprovalStatus == "Approved"
                && q.QuestionType == questionType
                && q.Difficulty == difficulty);
        
        // Exclude a question if specified
        if (excludeQuestionId.HasValue)
        {
            query = query.Where(q => q.Id != excludeQuestionId.Value);
        }
        
        var questions = await query.ToListAsync();

        if (!questions.Any())
        {
            // Fallback to any approved question with same difficulty (any type)
            var fallbackQuery = _context.InterviewQuestions
                .Where(q => q.IsActive 
                    && q.ApprovalStatus == "Approved"
                    && q.Difficulty == difficulty);
            
            if (excludeQuestionId.HasValue)
            {
                fallbackQuery = fallbackQuery.Where(q => q.Id != excludeQuestionId.Value);
            }
            
            questions = await fallbackQuery.ToListAsync();
        }

        if (!questions.Any())
        {
            return null;
        }

        var randomQuestion = questions[_random.Next(questions.Count)];
        return randomQuestion.Id;
    }

    private async Task CleanupExpiredRequestsAsync()
    {
        var expiredRequests = await _context.InterviewMatchingRequests
            .Where(m => m.Status == "Pending" && m.ExpiresAt < DateTime.UtcNow)
            .ToListAsync();

        foreach (var request in expiredRequests)
        {
            request.Status = "Expired";
            request.UpdatedAt = DateTime.UtcNow;
        }

        if (expiredRequests.Any())
        {
            await _context.SaveChangesAsync();
        }
    }

    private async Task<MatchingRequestDto> MapToMatchingRequestDtoAsync(InterviewMatchingRequest request)
    {
        return new MatchingRequestDto
        {
            Id = request.Id,
            UserId = request.UserId,
            ScheduledSessionId = request.ScheduledSessionId,
            InterviewType = request.InterviewType,
            PracticeType = request.PracticeType,
            InterviewLevel = request.InterviewLevel,
            ScheduledStartAt = request.ScheduledStartAt,
            Status = request.Status,
            MatchedUserId = request.MatchedUserId,
            LiveSessionId = request.LiveSessionId,
            UserConfirmed = request.UserConfirmed,
            MatchedUserConfirmed = request.MatchedUserConfirmed,
            ExpiresAt = request.ExpiresAt,
            CreatedAt = request.CreatedAt,
            UpdatedAt = request.UpdatedAt,
            User = request.User != null ? new UserDto
            {
                Id = request.User.Id,
                FirstName = request.User.FirstName,
                LastName = request.User.LastName,
                Email = request.User.Email
            } : null,
            MatchedUser = request.MatchedUser != null ? new UserDto
            {
                Id = request.MatchedUser.Id,
                FirstName = request.MatchedUser.FirstName,
                LastName = request.MatchedUser.LastName,
                Email = request.MatchedUser.Email
            } : null,
            ScheduledSession = null // Set to null for now - can be enhanced later if needed
        };
    }

    public async Task ExpireAllRequestsForSessionAsync(Guid scheduledSessionId, Guid userId)
    {
        // Find all non-expired, non-confirmed requests for this user and session
        var requestsToExpire = await _context.InterviewMatchingRequests
            .Where(r => r.ScheduledSessionId == scheduledSessionId 
                && r.UserId == userId 
                && r.Status != "Expired" 
                && r.Status != "Cancelled"
                && r.Status != "Confirmed")
            .ToListAsync();

        if (requestsToExpire.Any())
        {
            foreach (var request in requestsToExpire)
            {
                request.Status = "Expired";
                request.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Expired {Count} matching requests for user {UserId} and session {SessionId} (page refresh/close)", 
                requestsToExpire.Count, userId, scheduledSessionId);
        }
    }

    public async Task<bool> ExpireMatchOnUserDisconnectAsync(Guid userId)
    {
        // Find all "Matched" requests for this user (they're in confirmation window)
        var matchedRequests = await _context.InterviewMatchingRequests
            .Where(r => r.UserId == userId && r.Status == "Matched")
            .ToListAsync();

        if (!matchedRequests.Any())
        {
            return false; // No matched requests to expire
        }

        foreach (var matchedRequest in matchedRequests)
        {
            // Find the partner's request
            InterviewMatchingRequest? partnerRequest = null;
            if (matchedRequest.MatchedUserId.HasValue)
            {
                partnerRequest = await _context.InterviewMatchingRequests
                    .FirstOrDefaultAsync(r => r.UserId == matchedRequest.MatchedUserId.Value 
                        && r.MatchedUserId == matchedRequest.UserId 
                        && r.Status == "Matched");
            }

            // Expire this user's request
            matchedRequest.Status = "Expired";
            matchedRequest.UpdatedAt = DateTime.UtcNow;

            // Expire partner's request if it exists
            if (partnerRequest != null)
            {
                partnerRequest.Status = "Expired";
                partnerRequest.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("DISCONNECT_EXPIRE: User {UserId} disconnected during confirmation. Expired match with User {PartnerId}. RequestId={RequestId}", 
                userId, matchedRequest.MatchedUserId, matchedRequest.Id);

            // Re-queue the partner if they're still active
            if (partnerRequest != null && matchedRequest.MatchedUserId.HasValue)
            {
                var partnerId = matchedRequest.MatchedUserId.Value;
                var partnerSessionId = partnerRequest.ScheduledSessionId;
                var partnerIsActive = _presenceService.IsUserActive(partnerId, partnerSessionId);

                if (partnerIsActive)
                {
                    // Partner is still active - create new request and try to match
                    var newRequest = new InterviewMatchingRequest
                    {
                        UserId = partnerId,
                        ScheduledSessionId = partnerSessionId,
                        InterviewType = partnerRequest.InterviewType,
                        PracticeType = partnerRequest.PracticeType,
                        InterviewLevel = partnerRequest.InterviewLevel,
                        ScheduledStartAt = partnerRequest.ScheduledStartAt,
                        Status = "Pending",
                        ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.InterviewMatchingRequests.Add(newRequest);
                    await _context.SaveChangesAsync();

                    // Try to match immediately
                    await TryMatchAsync(newRequest);
                    await _context.Entry(newRequest).ReloadAsync();

                    _logger.LogInformation("DISCONNECT_REQUEUE: Partner User {PartnerId} re-queued after disconnect expiration. New RequestId={RequestId}, Status={Status}", 
                        partnerId, newRequest.Id, newRequest.Status);
                }
                else
                {
                    _logger.LogInformation("DISCONNECT_NO_REQUEUE: Partner User {PartnerId} not active, not re-queued after disconnect expiration.", partnerId);
                }
            }
        }

        return true;
    }

    #endregion
}
