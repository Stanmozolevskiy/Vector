using Microsoft.EntityFrameworkCore;
using Vector.Api.Data;
using Vector.Api.DTOs.PeerInterview;
using Vector.Api.Models;

namespace Vector.Api.Services;

public class PeerInterviewService : IPeerInterviewService
{
    private readonly ApplicationDbContext _context;
    private readonly IQuestionService _questionService;
    private readonly ILogger<PeerInterviewService> _logger;
    private static readonly Random _random = new Random();

    public PeerInterviewService(
        ApplicationDbContext context,
        IQuestionService questionService,
        ILogger<PeerInterviewService> logger)
    {
        _context = context;
        _questionService = questionService;
        _logger = logger;
    }

    // Scheduling
    public async Task<ScheduledInterviewSessionDto> ScheduleInterviewSessionAsync(Guid userId, ScheduleInterviewDto dto)
    {
        var session = new ScheduledInterviewSession
        {
            UserId = userId,
            InterviewType = dto.InterviewType,
            PracticeType = dto.PracticeType,
            InterviewLevel = dto.InterviewLevel,
            ScheduledStartAt = dto.ScheduledStartAt,
            Status = "Scheduled",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assign a question if interview type is data-structures-algorithms
        if (dto.InterviewType == "data-structures-algorithms")
        {
            var assignedQuestionId = await SelectRandomQuestionAsync(dto.InterviewType);
            if (assignedQuestionId.HasValue)
            {
                session.AssignedQuestionId = assignedQuestionId.Value;
                _logger.LogInformation("Assigned question {QuestionId} to scheduled session {SessionId} for user {UserId}",
                    assignedQuestionId.Value, session.Id, userId);
            }
        }

        _context.ScheduledInterviewSessions.Add(session);
        await _context.SaveChangesAsync();

        return await MapToScheduledSessionDtoAsync(session);
    }

    public async Task<IEnumerable<ScheduledInterviewSessionDto>> GetUpcomingSessionsAsync(Guid userId)
    {
        var now = DateTime.UtcNow;
        var sessions = await _context.ScheduledInterviewSessions
            .Include(s => s.User)
            .Where(s => s.UserId == userId 
                && s.Status != "Cancelled"
                && s.Status != "Completed"
                && s.ScheduledStartAt > now)
            .OrderBy(s => s.ScheduledStartAt)
            .ToListAsync();

        var dtos = new List<ScheduledInterviewSessionDto>();
        foreach (var session in sessions)
        {
            dtos.Add(await MapToScheduledSessionDtoAsync(session));
        }
        return dtos;
    }

    public async Task<IEnumerable<ScheduledInterviewSessionDto>> GetPastSessionsAsync(Guid userId)
    {
        var now = DateTime.UtcNow;
        var sessions = await _context.ScheduledInterviewSessions
            .Include(s => s.User)
            .Where(s => s.UserId == userId 
                && s.Status != "Cancelled"
                && (s.Status == "Completed" || s.ScheduledStartAt <= now))
            .OrderByDescending(s => s.ScheduledStartAt)
            .ToListAsync();

        var dtos = new List<ScheduledInterviewSessionDto>();
        foreach (var session in sessions)
        {
            dtos.Add(await MapToScheduledSessionDtoAsync(session));
        }
        return dtos;
    }

    public async Task<ScheduledInterviewSessionDto?> GetScheduledSessionByIdAsync(Guid sessionId, Guid userId)
    {
        var session = await _context.ScheduledInterviewSessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);

        if (session == null) return null;

        return await MapToScheduledSessionDtoAsync(session);
    }

    public async Task<bool> CancelScheduledSessionAsync(Guid sessionId, Guid userId)
    {
        var session = await _context.ScheduledInterviewSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);

        if (session == null) return false;

        session.Status = "Cancelled";
        session.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return true;
    }

    // Matching - SIMPLIFIED: Just add user to queue
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
                var liveSession = await GetLiveSessionByIdAsync(existingRequest.LiveSessionId.Value, userId);
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

        // Try to match immediately (might find a match right away)
        var isMatched = await TryMatchAsync(matchingRequest);

        // Refresh the request to get updated status
        await _context.Entry(matchingRequest).ReloadAsync();

        // LiveSessionId will be null until both users confirm
        // Only return session if status is Confirmed (both users confirmed)
        LiveInterviewSessionDto? sessionDto = null;
        if (matchingRequest.Status == "Confirmed" && matchingRequest.LiveSessionId.HasValue)
        {
            sessionDto = await GetLiveSessionByIdAsync(matchingRequest.LiveSessionId.Value, userId);
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
        var matchingRequest = await _context.InterviewMatchingRequests
            .Include(m => m.User)
            .Include(m => m.MatchedUser)
            .Include(m => m.ScheduledSession)
            .FirstOrDefaultAsync(m => m.ScheduledSessionId == scheduledSessionId 
                && m.UserId == userId
                && m.Status != "Expired"
                && m.Status != "Cancelled");

        if (matchingRequest == null) return null;

        // Check if expired
        if (matchingRequest.ExpiresAt < DateTime.UtcNow && matchingRequest.Status == "Pending")
        {
            matchingRequest.Status = "Expired";
            await _context.SaveChangesAsync();
            return null;
        }

        return await MapToMatchingRequestDtoAsync(matchingRequest);
    }

    // SIMPLIFIED: Just mark user as confirmed, check if both confirmed, update session status
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
            
            _logger.LogInformation("Both users confirmed! Live session {LiveSessionId} created and started with InProgress status", liveSession.Id);
        }

        await _context.SaveChangesAsync();

        var liveSessionDto = matchingRequest.LiveSessionId.HasValue
            ? await GetLiveSessionByIdAsync(matchingRequest.LiveSessionId.Value, userId)
            : null;

        return new ConfirmMatchResponseDto
        {
            MatchingRequest = await MapToMatchingRequestDtoAsync(matchingRequest),
            Completed = bothConfirmed,
            Session = liveSessionDto
        };
    }

    // Expire match if not both confirmed within 15 seconds, re-queue users
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

        // No live session exists yet (it's only created after both confirm)
        // So no need to delete anything

        // Determine who confirmed and who didn't
        bool user1Confirmed = matchingRequest.UserConfirmed;
        bool user2Confirmed = otherUserRequest?.UserConfirmed ?? false;

        if (user1Confirmed && !user2Confirmed)
        {
            // User 1 confirmed, User 2 didn't
            // User 1: Reset to Pending, clear match info, restart timer, requeue at front
            matchingRequest.Status = "Pending";
            matchingRequest.MatchedUserId = null;
            matchingRequest.LiveSessionId = null;
            matchingRequest.UserConfirmed = false;
            matchingRequest.MatchedUserConfirmed = false;
            matchingRequest.ExpiresAt = DateTime.UtcNow.AddMinutes(10); // Restart 10-minute timer
            matchingRequest.UpdatedAt = DateTime.UtcNow;
            
            InterviewMatchingRequest? newRequest2 = null;
            // User 2: Mark as Expired, create new request
            if (otherUserRequest != null)
            {
                otherUserRequest.Status = "Expired";
                otherUserRequest.UpdatedAt = DateTime.UtcNow;
                
                // Create new matching request for User 2
                newRequest2 = new InterviewMatchingRequest
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
            }
            
            // Save all changes (matchingRequest status change + otherUserRequest status change + newRequest2)
            await _context.SaveChangesAsync();
            
            if (newRequest2 != null)
            {
                var newRequest2Id = newRequest2.Id;
                // Try to match User 2's new request
                _ = Task.Run(async () =>
                {
                    await Task.Delay(200);
                    var newRequest2Reloaded = await _context.InterviewMatchingRequests.FindAsync(newRequest2Id);
                    if (newRequest2Reloaded != null && newRequest2Reloaded.Status == "Pending")
                    {
                        await TryMatchAsync(newRequest2Reloaded);
                    }
                });
            }
            
            // Requeue User 1 at front (match immediately)
            _ = Task.Run(async () =>
            {
                await Task.Delay(100);
                await TryMatchAsync(matchingRequest);
            });
            
            _logger.LogInformation("User {UserId1} confirmed but User {UserId2} didn't. User 1 requeued at front, User 2 expired and new request created.", 
                matchingRequest.UserId, matchingRequest.MatchedUserId);
        }
        else if (!user1Confirmed && user2Confirmed)
        {
            // User 2 confirmed, User 1 didn't
            // User 2: Reset to Pending, clear match info, restart timer, requeue at front
            if (otherUserRequest != null)
            {
                otherUserRequest.Status = "Pending";
                otherUserRequest.MatchedUserId = null;
                otherUserRequest.LiveSessionId = null;
                otherUserRequest.UserConfirmed = false;
                otherUserRequest.MatchedUserConfirmed = false;
                otherUserRequest.ExpiresAt = DateTime.UtcNow.AddMinutes(10); // Restart 10-minute timer
                otherUserRequest.UpdatedAt = DateTime.UtcNow;
            }
            
            // User 1: Mark as Expired, create new request
            matchingRequest.Status = "Expired";
            matchingRequest.UpdatedAt = DateTime.UtcNow;
            
            // Create new matching request for User 1
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
            
            // Save all changes (otherUserRequest status change + matchingRequest status change + newRequest1)
            await _context.SaveChangesAsync();
            
            var newRequest1Id = newRequest1.Id;
            
            // Requeue User 2 at front (match immediately)
            if (otherUserRequest != null)
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(100);
                    await TryMatchAsync(otherUserRequest);
                });
            }
            
            // Try to match User 1's new request
            _ = Task.Run(async () =>
            {
                await Task.Delay(200);
                var newRequest1Reloaded = await _context.InterviewMatchingRequests.FindAsync(newRequest1Id);
                if (newRequest1Reloaded != null && newRequest1Reloaded.Status == "Pending")
                {
                    await TryMatchAsync(newRequest1Reloaded);
                }
            });
            
            _logger.LogInformation("User {UserId2} confirmed but User {UserId1} didn't. User 2 requeued at front, User 1 expired and new request created.", 
                matchingRequest.MatchedUserId, matchingRequest.UserId);
        }
        else
        {
            // Neither user confirmed - both expired, create new requests for both
            matchingRequest.Status = "Expired";
            matchingRequest.UpdatedAt = DateTime.UtcNow;
            
            if (otherUserRequest != null)
            {
                otherUserRequest.Status = "Expired";
                otherUserRequest.UpdatedAt = DateTime.UtcNow;
            }
            
            // Create new matching request for User 1
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
            
            Guid? newRequest2Id = null;
            // Create new matching request for User 2
            if (otherUserRequest != null)
            {
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
                newRequest2Id = newRequest2.Id;
            }
            
            // Save all changes (both expired requests + both new requests)
            await _context.SaveChangesAsync();
            
            var newRequest1Id = newRequest1.Id;
            
            // Try to match both new requests
            _ = Task.Run(async () =>
            {
                await Task.Delay(100);
                var newRequest1Reloaded = await _context.InterviewMatchingRequests.FindAsync(newRequest1Id);
                if (newRequest1Reloaded != null && newRequest1Reloaded.Status == "Pending")
                {
                    await TryMatchAsync(newRequest1Reloaded);
                }
            });
            
            if (newRequest2Id.HasValue)
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(200);
                    var newRequest2Reloaded = await _context.InterviewMatchingRequests.FindAsync(newRequest2Id.Value);
                    if (newRequest2Reloaded != null && newRequest2Reloaded.Status == "Pending")
                    {
                        await TryMatchAsync(newRequest2Reloaded);
                    }
                });
            }
            
            _logger.LogInformation("Neither user confirmed. Both expired and new requests created for rematching.");
        }

        return true;
    }

    // Live Sessions
    public async Task<LiveInterviewSessionDto?> GetLiveSessionByIdAsync(Guid sessionId, Guid userId)
    {
        var session = await _context.LiveInterviewSessions
            .Include(s => s.FirstQuestion)
            .Include(s => s.SecondQuestion)
            .Include(s => s.Participants)
                .ThenInclude(p => p.User)
            .Include(s => s.ScheduledSession)
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session == null) return null;

        // Verify user is a participant
        var isParticipant = session.Participants.Any(p => p.UserId == userId);
        if (!isParticipant)
        {
            throw new UnauthorizedAccessException("User is not a participant in this session.");
        }

        return await MapToLiveSessionDtoAsync(session, userId);
    }

    public async Task<SwitchRolesResponseDto> SwitchRolesAsync(Guid sessionId, Guid userId)
    {
        var session = await _context.LiveInterviewSessions
            .Include(s => s.Participants)
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session == null)
        {
            throw new KeyNotFoundException("Session not found.");
        }

        var participant = session.Participants.FirstOrDefault(p => p.UserId == userId);
        if (participant == null)
        {
            throw new UnauthorizedAccessException("User is not a participant in this session.");
        }

        var otherParticipant = session.Participants.FirstOrDefault(p => p.UserId != userId);
        if (otherParticipant == null)
        {
            throw new InvalidOperationException("Session must have exactly 2 participants.");
        }

        // Swap roles
        var tempRole = participant.Role;
        participant.Role = otherParticipant.Role;
        otherParticipant.Role = tempRole;

        // Switch active question when roles are swapped
        // If currently on FirstQuestionId, switch to SecondQuestionId and vice versa
        if (session.FirstQuestionId.HasValue && session.SecondQuestionId.HasValue)
        {
            if (session.ActiveQuestionId == session.FirstQuestionId)
            {
                session.ActiveQuestionId = session.SecondQuestionId.Value;
            }
            else if (session.ActiveQuestionId == session.SecondQuestionId)
            {
                session.ActiveQuestionId = session.FirstQuestionId.Value;
            }
        }

        participant.UpdatedAt = DateTime.UtcNow;
        otherParticipant.UpdatedAt = DateTime.UtcNow;
        session.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Reload session with all includes
        var updatedSession = await _context.LiveInterviewSessions
            .Include(s => s.FirstQuestion)
            .Include(s => s.SecondQuestion)
            .Include(s => s.Participants)
                .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        return new SwitchRolesResponseDto
        {
            Session = await MapToLiveSessionDtoAsync(updatedSession!, userId),
            YourNewRole = participant.Role,
            PartnerNewRole = otherParticipant.Role
        };
    }

    public async Task<ChangeQuestionResponseDto> ChangeQuestionAsync(Guid sessionId, Guid userId, Guid? newQuestionId = null)
    {
        var session = await _context.LiveInterviewSessions
            .Include(s => s.Participants)
            .Include(s => s.FirstQuestion)
            .Include(s => s.SecondQuestion)
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session == null)
        {
            throw new KeyNotFoundException("Session not found.");
        }

        var participant = session.Participants.FirstOrDefault(p => p.UserId == userId);
        if (participant == null || participant.Role != "Interviewer")
        {
            throw new UnauthorizedAccessException("Only the interviewer can change the question.");
        }

        // If no question ID provided, select a random question based on interview type
        // Exclude current active question to ensure we get a different one
        if (!newQuestionId.HasValue)
        {
            newQuestionId = await SelectRandomQuestionAsync(session, session.ActiveQuestionId);
        }

        if (!newQuestionId.HasValue)
        {
            throw new InvalidOperationException("Could not find a suitable question.");
        }

        // Verify question exists
        var question = await _context.InterviewQuestions
            .FirstOrDefaultAsync(q => q.Id == newQuestionId.Value && q.IsActive);

        if (question == null)
        {
            throw new KeyNotFoundException("Question not found.");
        }

        // Replace the currently active question with the new question
        // This ensures role switching continues to work correctly
        if (session.FirstQuestionId == null)
        {
            // No first question set yet, set it as the active question
            session.FirstQuestionId = newQuestionId.Value;
            session.ActiveQuestionId = newQuestionId.Value;
        }
        else if (session.SecondQuestionId == null)
        {
            // No second question set yet, set it as the active question
            session.SecondQuestionId = newQuestionId.Value;
            session.ActiveQuestionId = newQuestionId.Value;
        }
        else
        {
            // Both questions are set - replace the currently active one
            // If ActiveQuestionId matches FirstQuestionId, replace FirstQuestionId
            // If ActiveQuestionId matches SecondQuestionId, replace SecondQuestionId
            if (session.ActiveQuestionId == session.FirstQuestionId)
            {
                session.FirstQuestionId = newQuestionId.Value;
                session.ActiveQuestionId = newQuestionId.Value;
            }
            else if (session.ActiveQuestionId == session.SecondQuestionId)
            {
                session.SecondQuestionId = newQuestionId.Value;
                session.ActiveQuestionId = newQuestionId.Value;
            }
            else
            {
                // ActiveQuestionId doesn't match either - default to replacing FirstQuestionId
                session.FirstQuestionId = newQuestionId.Value;
                session.ActiveQuestionId = newQuestionId.Value;
            }
        }

        session.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Reload session
        var updatedSession = await _context.LiveInterviewSessions
            .Include(s => s.FirstQuestion)
            .Include(s => s.SecondQuestion)
            .Include(s => s.Participants)
                .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        var activeQuestion = updatedSession!.ActiveQuestionId.HasValue
            ? await _context.InterviewQuestions.FindAsync(updatedSession.ActiveQuestionId.Value)
            : null;

        return new ChangeQuestionResponseDto
        {
            Session = await MapToLiveSessionDtoAsync(updatedSession, userId),
            NewActiveQuestion = activeQuestion != null ? new QuestionSummaryDto
            {
                Id = activeQuestion.Id,
                Title = activeQuestion.Title,
                Difficulty = activeQuestion.Difficulty,
                QuestionType = activeQuestion.QuestionType
            } : null
        };
    }

    public async Task<LiveInterviewSessionDto> EndInterviewAsync(Guid sessionId, Guid userId)
    {
        var session = await _context.LiveInterviewSessions
            .Include(s => s.Participants)
                .ThenInclude(p => p.User)
            .Include(s => s.FirstQuestion)
            .Include(s => s.SecondQuestion)
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session == null)
        {
            throw new KeyNotFoundException("Session not found.");
        }

        var participant = session.Participants.FirstOrDefault(p => p.UserId == userId);
        if (participant == null)
        {
            throw new UnauthorizedAccessException("User is not a participant in this session.");
        }

        // Use helper function to update all scheduled sessions for both users
        await UpdateScheduledSessionsOnEndAsync(session);

        // Reload session with all includes
        var updatedSession = await _context.LiveInterviewSessions
            .Include(s => s.FirstQuestion)
            .Include(s => s.SecondQuestion)
            .Include(s => s.Participants)
                .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        return await MapToLiveSessionDtoAsync(updatedSession!, userId);
    }

    /// <summary>
    /// Helper function to update all scheduled sessions when an interview ends.
    /// Ensures both users' scheduled sessions are marked as completed and have access to
    /// both questions and partner information.
    /// </summary>
    private async Task UpdateScheduledSessionsOnEndAsync(LiveInterviewSession liveSession)
    {
        // Mark live session as completed
        liveSession.Status = "Completed";
        liveSession.EndedAt = DateTime.UtcNow;
        liveSession.UpdatedAt = DateTime.UtcNow;

        // Ensure both questions are set (they should already be set, but verify)
        if (!liveSession.FirstQuestionId.HasValue || !liveSession.SecondQuestionId.HasValue)
        {
            _logger.LogWarning("Live session {SessionId} is missing questions when ending", liveSession.Id);
        }

        // Find all matching requests that reference this live session
        var matchingRequests = await _context.InterviewMatchingRequests
            .Where(m => m.LiveSessionId == liveSession.Id)
            .Include(m => m.ScheduledSession)
            .ToListAsync();

        // Get all unique scheduled session IDs from matching requests
        var scheduledSessionIds = matchingRequests
            .Select(m => m.ScheduledSessionId)
            .Distinct()
            .ToList();

        // Also include the directly linked scheduled session if it exists
        if (liveSession.ScheduledSessionId.HasValue && !scheduledSessionIds.Contains(liveSession.ScheduledSessionId.Value))
        {
            scheduledSessionIds.Add(liveSession.ScheduledSessionId.Value);
        }

        // Update all scheduled sessions for both users
        if (scheduledSessionIds.Any())
        {
            var scheduledSessions = await _context.ScheduledInterviewSessions
                .Where(s => scheduledSessionIds.Contains(s.Id))
                .ToListAsync();

            foreach (var scheduledSession in scheduledSessions)
            {
                scheduledSession.Status = "Completed";
                scheduledSession.UpdatedAt = DateTime.UtcNow;
                // The LiveSession navigation property will automatically link to the live session
                // which contains both questions and participant information
            }

            _logger.LogInformation("Updated {Count} scheduled sessions to Completed for live session {SessionId}",
                scheduledSessions.Count, liveSession.Id);
        }

        await _context.SaveChangesAsync();
    }

    // Feedback
    public async Task<InterviewFeedbackDto> SubmitFeedbackAsync(Guid userId, SubmitFeedbackDto dto)
    {
        // Verify session exists and user is a participant
        var session = await _context.LiveInterviewSessions
            .Include(s => s.Participants)
            .FirstOrDefaultAsync(s => s.Id == dto.LiveSessionId);

        if (session == null)
        {
            throw new KeyNotFoundException("Session not found.");
        }

        var participant = session.Participants.FirstOrDefault(p => p.UserId == userId);
        if (participant == null)
        {
            throw new UnauthorizedAccessException("User is not a participant in this session.");
        }

        // Verify reviewee is the other participant
        var otherParticipant = session.Participants.FirstOrDefault(p => p.UserId == dto.RevieweeId);
        if (otherParticipant == null || otherParticipant.UserId == userId)
        {
            throw new InvalidOperationException("Reviewee must be the other participant in the session.");
        }

        // Check if feedback already exists
        var existingFeedback = await _context.InterviewFeedbacks
            .FirstOrDefaultAsync(f => f.LiveSessionId == dto.LiveSessionId 
                && f.ReviewerId == userId 
                && f.RevieweeId == dto.RevieweeId);

        InterviewFeedback feedback;
        if (existingFeedback != null)
        {
            // Update existing feedback
            feedback = existingFeedback;
            feedback.ProblemSolvingRating = dto.ProblemSolvingRating;
            feedback.ProblemSolvingDescription = dto.ProblemSolvingDescription;
            feedback.CodingSkillsRating = dto.CodingSkillsRating;
            feedback.CodingSkillsDescription = dto.CodingSkillsDescription;
            feedback.CommunicationRating = dto.CommunicationRating;
            feedback.CommunicationDescription = dto.CommunicationDescription;
            feedback.ThingsDidWell = dto.ThingsDidWell;
            feedback.AreasForImprovement = dto.AreasForImprovement;
            feedback.InterviewerPerformanceRating = dto.InterviewerPerformanceRating;
            feedback.InterviewerPerformanceDescription = dto.InterviewerPerformanceDescription;
            feedback.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            // Create new feedback
            feedback = new InterviewFeedback
            {
                LiveSessionId = dto.LiveSessionId,
                ReviewerId = userId,
                RevieweeId = dto.RevieweeId,
                ProblemSolvingRating = dto.ProblemSolvingRating,
                ProblemSolvingDescription = dto.ProblemSolvingDescription,
                CodingSkillsRating = dto.CodingSkillsRating,
                CodingSkillsDescription = dto.CodingSkillsDescription,
                CommunicationRating = dto.CommunicationRating,
                CommunicationDescription = dto.CommunicationDescription,
                ThingsDidWell = dto.ThingsDidWell,
                AreasForImprovement = dto.AreasForImprovement,
                InterviewerPerformanceRating = dto.InterviewerPerformanceRating,
                InterviewerPerformanceDescription = dto.InterviewerPerformanceDescription,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.InterviewFeedbacks.Add(feedback);
        }

        await _context.SaveChangesAsync();

        return await MapToFeedbackDtoAsync(feedback);
    }

    public async Task<IEnumerable<InterviewFeedbackDto>> GetFeedbackForSessionAsync(Guid sessionId, Guid userId)
    {
        // Verify user is a participant
        var session = await _context.LiveInterviewSessions
            .Include(s => s.Participants)
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session == null)
        {
            throw new KeyNotFoundException("Session not found.");
        }

        var participant = session.Participants.FirstOrDefault(p => p.UserId == userId);
        if (participant == null)
        {
            throw new UnauthorizedAccessException("User is not a participant in this session.");
        }

        // Return only feedback ABOUT this user (where they are the reviewee)
        // Each user should only see feedback left for them, not feedback they left for others
        var feedbacks = await _context.InterviewFeedbacks
            .Include(f => f.Reviewer)
            .Include(f => f.Reviewee)
            .Where(f => f.LiveSessionId == sessionId && f.RevieweeId == userId)
            .ToListAsync();

        var dtos = new List<InterviewFeedbackDto>();
        foreach (var feedback in feedbacks)
        {
            dtos.Add(await MapToFeedbackDtoAsync(feedback));
        }
        return dtos;
    }

    public async Task<InterviewFeedbackDto?> GetFeedbackAsync(Guid feedbackId, Guid userId)
    {
        var feedback = await _context.InterviewFeedbacks
            .Include(f => f.Reviewer)
            .Include(f => f.Reviewee)
            .Include(f => f.LiveSession)
                .ThenInclude(s => s!.Participants)
            .FirstOrDefaultAsync(f => f.Id == feedbackId);

        if (feedback == null) return null;

        // Verify user is part of the session
        var isParticipant = feedback.LiveSession?.Participants.Any(p => p.UserId == userId) ?? false;
        if (!isParticipant)
        {
            throw new UnauthorizedAccessException("User is not a participant in this session.");
        }

        return await MapToFeedbackDtoAsync(feedback);
    }

    public async Task<FeedbackStatusDto> GetFeedbackStatusAsync(Guid sessionId, Guid userId)
    {
        // Verify user is a participant
        var session = await _context.LiveInterviewSessions
            .Include(s => s.Participants)
                .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session == null)
        {
            throw new KeyNotFoundException("Session not found.");
        }

        var participant = session.Participants.FirstOrDefault(p => p.UserId == userId);
        if (participant == null)
        {
            throw new UnauthorizedAccessException("User is not a participant in this session.");
        }

        // Find the opponent (the other participant)
        var opponent = session.Participants.FirstOrDefault(p => p.UserId != userId);
        if (opponent == null)
        {
            throw new InvalidOperationException("Opponent not found in session.");
        }

        // Check if current user has submitted feedback for opponent
        var userFeedbackForOpponent = await _context.InterviewFeedbacks
            .FirstOrDefaultAsync(f => f.LiveSessionId == sessionId 
                && f.ReviewerId == userId 
                && f.RevieweeId == opponent.UserId);

        // Check if opponent has submitted feedback for current user
        var opponentFeedbackForUser = await _context.InterviewFeedbacks
            .Include(f => f.Reviewer)
            .Include(f => f.Reviewee)
            .FirstOrDefaultAsync(f => f.LiveSessionId == sessionId 
                && f.ReviewerId == opponent.UserId 
                && f.RevieweeId == userId);

        var status = new FeedbackStatusDto
        {
            HasUserSubmittedFeedback = userFeedbackForOpponent != null,
            HasOpponentSubmittedFeedback = opponentFeedbackForUser != null,
            OpponentId = opponent.UserId,
            LiveSessionId = sessionId,
            Opponent = opponent.User != null ? new UserDto
            {
                Id = opponent.User.Id,
                FirstName = opponent.User.FirstName,
                LastName = opponent.User.LastName,
                Email = opponent.User.Email
            } : null
        };

        // If opponent has submitted feedback, include it
        if (opponentFeedbackForUser != null)
        {
            status.OpponentFeedback = await MapToFeedbackDtoAsync(opponentFeedbackForUser);
        }

        return status;
    }

    // Private helper methods
    // SIMPLIFIED: Match 2 users from queue, IMMEDIATELY create live session with questions and roles
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
            
            _logger.LogInformation("Users {UserId1} and {UserId2} matched. Waiting for both to confirm readiness.", 
                request.UserId, bestMatch.UserId);
            
            return true;
        }

        return false;
    }
    
    // Create live session immediately when match is found (before confirmation)
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

        // Update scheduled session status (LiveSessionId is set via navigation property)
        var scheduledSession = await _context.ScheduledInterviewSessions
            .FirstOrDefaultAsync(s => s.Id == schedulerRequest.ScheduledSessionId);
        if (scheduledSession != null)
        {
            scheduledSession.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return liveSession;
    }

    private async Task<LiveInterviewSession> CreateLiveSessionAsync(InterviewMatchingRequest matchingRequest)
    {
        // Select first question based on interview type
        var firstQuestionId = await SelectRandomQuestionAsync(matchingRequest.InterviewType);
        
        var liveSession = new LiveInterviewSession
        {
            ScheduledSessionId = matchingRequest.ScheduledSessionId,
            FirstQuestionId = firstQuestionId,
            ActiveQuestionId = firstQuestionId,
            Status = "InProgress",
            StartedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.LiveInterviewSessions.Add(liveSession);
        await _context.SaveChangesAsync();

        // Create participants
        // Session creator = Interviewer, matched user = Interviewee
        var interviewer = new LiveInterviewParticipant
        {
            LiveSessionId = liveSession.Id,
            UserId = matchingRequest.UserId,
            Role = "Interviewer",
            IsActive = true,
            JoinedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var interviewee = new LiveInterviewParticipant
        {
            LiveSessionId = liveSession.Id,
            UserId = matchingRequest.MatchedUserId!.Value,
            Role = "Interviewee",
            IsActive = true,
            JoinedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.LiveInterviewParticipants.Add(interviewer);
        _context.LiveInterviewParticipants.Add(interviewee);
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
            { "product-management", "Behavioral" }, // Product management uses behavioral questions
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

    /// <summary>
    /// Select a random question with a specific difficulty, excluding a question ID if provided
    /// </summary>
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

    private async Task<Guid?> SelectRandomQuestionAsync(LiveInterviewSession session, Guid? excludeQuestionId = null)
    {
        // Get interview type from scheduled session
        if (session.ScheduledSessionId.HasValue)
        {
            var scheduledSession = await _context.ScheduledInterviewSessions
                .FirstOrDefaultAsync(s => s.Id == session.ScheduledSessionId.Value);
            
            if (scheduledSession != null)
            {
                // Exclude the current active question (or provided exclude ID) to ensure we get a different question
                var excludeId = excludeQuestionId ?? session.ActiveQuestionId;
                return await SelectRandomQuestionAsync(scheduledSession.InterviewType, excludeId);
            }
        }

        // Fallback to any question, excluding current active question (or provided exclude ID)
        var fallbackExcludeId = excludeQuestionId ?? session.ActiveQuestionId;
        return await SelectRandomQuestionAsync("data-structures-algorithms", fallbackExcludeId);
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

    // Mapping methods
    private async Task<ScheduledInterviewSessionDto> MapToScheduledSessionDtoAsync(ScheduledInterviewSession session)
    {
        await _context.Entry(session).Reference(s => s.User).LoadAsync();
        await _context.Entry(session).Reference(s => s.LiveSession).LoadAsync();
        await _context.Entry(session).Reference(s => s.AssignedQuestion).LoadAsync();

        // If LiveSession is not directly linked, find it through matching requests
        LiveInterviewSession? liveSession = session.LiveSession;
        if (liveSession == null)
        {
            // Find matching request for this scheduled session that has a live session
            var matchingRequest = await _context.InterviewMatchingRequests
                .Include(m => m.LiveSession)
                    .ThenInclude(ls => ls!.FirstQuestion)
                .Include(m => m.LiveSession)
                    .ThenInclude(ls => ls!.SecondQuestion)
                .Include(m => m.LiveSession)
                    .ThenInclude(ls => ls!.Participants)
                        .ThenInclude(p => p.User)
                .FirstOrDefaultAsync(m => m.ScheduledSessionId == session.Id && m.LiveSessionId != null);
            
            if (matchingRequest?.LiveSession != null)
            {
                liveSession = matchingRequest.LiveSession;
            }
        }
        else
        {
            // Ensure LiveSession includes FirstQuestion and SecondQuestion for display
            await _context.Entry(liveSession).Reference(ls => ls.FirstQuestion).LoadAsync();
            await _context.Entry(liveSession).Reference(ls => ls.SecondQuestion).LoadAsync();
            await _context.Entry(liveSession).Collection(ls => ls.Participants).LoadAsync();
            foreach (var participant in liveSession.Participants)
            {
                await _context.Entry(participant).Reference(p => p.User).LoadAsync();
            }
        }

        return new ScheduledInterviewSessionDto
        {
            Id = session.Id,
            UserId = session.UserId,
            InterviewType = session.InterviewType,
            PracticeType = session.PracticeType,
            InterviewLevel = session.InterviewLevel,
            ScheduledStartAt = session.ScheduledStartAt,
            Status = session.Status,
            LiveSessionId = liveSession?.Id,
            AssignedQuestionId = session.AssignedQuestionId,
            CreatedAt = session.CreatedAt,
            UpdatedAt = session.UpdatedAt,
            User = session.User != null ? new UserDto
            {
                Id = session.User.Id,
                FirstName = session.User.FirstName,
                LastName = session.User.LastName,
                Email = session.User.Email
            } : null,
            LiveSession = liveSession != null 
                ? await MapToLiveSessionDtoAsync(liveSession, session.UserId)
                : null,
            AssignedQuestion = session.AssignedQuestion != null ? new QuestionSummaryDto
            {
                Id = session.AssignedQuestion.Id,
                Title = session.AssignedQuestion.Title,
                Difficulty = session.AssignedQuestion.Difficulty,
                QuestionType = session.AssignedQuestion.QuestionType
            } : null
        };
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
            ScheduledSession = request.ScheduledSession != null
                ? await MapToScheduledSessionDtoAsync(request.ScheduledSession)
                : null
        };
    }

    private async Task<LiveInterviewSessionDto> MapToLiveSessionDtoAsync(LiveInterviewSession session, Guid userId)
    {
        var participants = session.Participants.Select(p => new ParticipantDto
        {
            Id = p.Id,
            UserId = p.UserId,
            Role = p.Role,
            IsActive = p.IsActive,
            JoinedAt = p.JoinedAt,
            User = p.User != null ? new UserDto
            {
                Id = p.User.Id,
                FirstName = p.User.FirstName,
                LastName = p.User.LastName,
                Email = p.User.Email
            } : null
        }).ToList();

        var activeQuestion = session.ActiveQuestionId.HasValue
            ? await _context.InterviewQuestions.FindAsync(session.ActiveQuestionId.Value)
            : null;

        return new LiveInterviewSessionDto
        {
            Id = session.Id,
            ScheduledSessionId = session.ScheduledSessionId,
            FirstQuestionId = session.FirstQuestionId,
            SecondQuestionId = session.SecondQuestionId,
            ActiveQuestionId = session.ActiveQuestionId,
            Status = session.Status,
            StartedAt = session.StartedAt,
            EndedAt = session.EndedAt,
            CreatedAt = session.CreatedAt,
            UpdatedAt = session.UpdatedAt,
            FirstQuestion = session.FirstQuestion != null ? new QuestionSummaryDto
            {
                Id = session.FirstQuestion.Id,
                Title = session.FirstQuestion.Title,
                Difficulty = session.FirstQuestion.Difficulty,
                QuestionType = session.FirstQuestion.QuestionType
            } : null,
            SecondQuestion = session.SecondQuestion != null ? new QuestionSummaryDto
            {
                Id = session.SecondQuestion.Id,
                Title = session.SecondQuestion.Title,
                Difficulty = session.SecondQuestion.Difficulty,
                QuestionType = session.SecondQuestion.QuestionType
            } : null,
            ActiveQuestion = activeQuestion != null ? new QuestionSummaryDto
            {
                Id = activeQuestion.Id,
                Title = activeQuestion.Title,
                Difficulty = activeQuestion.Difficulty,
                QuestionType = activeQuestion.QuestionType
            } : null,
            Participants = participants
        };
    }

    private async Task<InterviewFeedbackDto> MapToFeedbackDtoAsync(InterviewFeedback feedback)
    {
        return new InterviewFeedbackDto
        {
            Id = feedback.Id,
            LiveSessionId = feedback.LiveSessionId,
            ReviewerId = feedback.ReviewerId,
            RevieweeId = feedback.RevieweeId,
            ProblemSolvingRating = feedback.ProblemSolvingRating,
            ProblemSolvingDescription = feedback.ProblemSolvingDescription,
            CodingSkillsRating = feedback.CodingSkillsRating,
            CodingSkillsDescription = feedback.CodingSkillsDescription,
            CommunicationRating = feedback.CommunicationRating,
            CommunicationDescription = feedback.CommunicationDescription,
            ThingsDidWell = feedback.ThingsDidWell,
            AreasForImprovement = feedback.AreasForImprovement,
            InterviewerPerformanceRating = feedback.InterviewerPerformanceRating,
            InterviewerPerformanceDescription = feedback.InterviewerPerformanceDescription,
            CreatedAt = feedback.CreatedAt,
            UpdatedAt = feedback.UpdatedAt,
            Reviewer = feedback.Reviewer != null ? new UserDto
            {
                Id = feedback.Reviewer.Id,
                FirstName = feedback.Reviewer.FirstName,
                LastName = feedback.Reviewer.LastName,
                Email = feedback.Reviewer.Email
            } : null,
            Reviewee = feedback.Reviewee != null ? new UserDto
            {
                Id = feedback.Reviewee.Id,
                FirstName = feedback.Reviewee.FirstName,
                LastName = feedback.Reviewee.LastName,
                Email = feedback.Reviewee.Email
            } : null
        };
    }
}

