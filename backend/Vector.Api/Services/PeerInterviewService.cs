using Microsoft.EntityFrameworkCore;
using System.Linq;
using Vector.Api.Data;
using Vector.Api.DTOs.PeerInterview;
using Vector.Api.Models;
using Vector.Api.Constants;

namespace Vector.Api.Services;

public class PeerInterviewService : IPeerInterviewService
{
    private readonly ApplicationDbContext _context;
    private readonly IQuestionService _questionService;
    private readonly ICoinService _coinService;
    private readonly ILogger<PeerInterviewService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private static readonly Random _random = new Random();

    public PeerInterviewService(
        ApplicationDbContext context,
        IQuestionService questionService,
        ICoinService coinService,
        ILogger<PeerInterviewService> logger,
        IServiceProvider serviceProvider)
    {
        _context = context;
        _questionService = questionService;
        _coinService = coinService;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    // Scheduling
    public async Task<ScheduledInterviewSessionDto?> ScheduleInterviewSessionAsync(Guid userId, ScheduleInterviewDto dto)
    {
        // Validate user exists
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {userId} not found");
        }

        // Validate scheduled time is in the future
        if (dto.ScheduledStartAt <= DateTime.UtcNow)
        {
            return null;
        }

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

        // Assign a question if interview type is data-structures-algorithms or sql
        if (dto.InterviewType == "data-structures-algorithms" || dto.InterviewType == "sql")
        {
            var assignedQuestionId = await SelectRandomQuestionAsync(dto.InterviewType);
            if (assignedQuestionId.HasValue)
            {
                session.AssignedQuestionId = assignedQuestionId.Value;
                _logger.LogInformation("Assigned question {QuestionId} to scheduled session {SessionId} for user {UserId} (InterviewType: {InterviewType})",
                    assignedQuestionId.Value, session.Id, userId, dto.InterviewType);
            }
        }

        _context.ScheduledInterviewSessions.Add(session);
        await _context.SaveChangesAsync();

        return await MapToScheduledSessionDtoAsync(session);
    }

    public async Task<IEnumerable<ScheduledInterviewSessionDto>> GetUpcomingSessionsAsync(Guid userId)
    {
        // Upcoming interviews:
        // - Scheduled sessions only
        // - No active matched/confirmed live interview
        // - Visible until 10 minutes after ScheduledStartAt
        var now = DateTime.UtcNow;
        var windowStart = now.AddMinutes(-10);

        var candidateSessions = await _context.ScheduledInterviewSessions
            .Include(s => s.User)
            .Where(s => s.UserId == userId
                && s.Status == "Scheduled"
                && s.ScheduledStartAt >= windowStart)
            .OrderBy(s => s.ScheduledStartAt)
            .ToListAsync();

        if (!candidateSessions.Any())
        {
            return Array.Empty<ScheduledInterviewSessionDto>();
        }

        // Exclude any scheduled session that already has a confirmed live session.
        // In this system, a live session is created only after both users confirm.
        var sessionIds = candidateSessions.Select(s => s.Id).ToList();

        var confirmedScheduledSessionIds = await _context.InterviewMatchingRequests
            .Where(m => sessionIds.Contains(m.ScheduledSessionId)
                && m.LiveSessionId != null
                && m.Status == "Confirmed")
            .Select(m => m.ScheduledSessionId)
            .Distinct()
            .ToListAsync();

        var confirmedSet = confirmedScheduledSessionIds.ToHashSet();

        // Also exclude directly linked live sessions (defensive)
        var directlyLinkedLiveSessionIds = await _context.LiveInterviewSessions
            .Where(ls => ls.ScheduledSessionId != null && sessionIds.Contains(ls.ScheduledSessionId.Value))
            .Select(ls => ls.ScheduledSessionId!.Value)
            .Distinct()
            .ToListAsync();

        foreach (var sid in directlyLinkedLiveSessionIds)
        {
            confirmedSet.Add(sid);
        }

        var upcoming = candidateSessions.Where(s => !confirmedSet.Contains(s.Id)).ToList();

        var dtos = new List<ScheduledInterviewSessionDto>(upcoming.Count);
        foreach (var session in upcoming)
        {
            dtos.Add(await MapToScheduledSessionDtoAsync(session));
        }
        return dtos;
    }

    public async Task<IEnumerable<ScheduledInterviewSessionDto>> GetPastSessionsAsync(Guid userId)
    {
        // Past interviews:
        // - Only live interviews that were confirmed by BOTH users.
        // - Live interviews can be InProgress or Completed.
        var candidateSessions = await _context.ScheduledInterviewSessions
            .Include(s => s.User)
            .Include(s => s.LiveSession)
            .Where(s => s.UserId == userId && s.Status != "Cancelled")
            .OrderByDescending(s => s.ScheduledStartAt)
            .ToListAsync();

        if (!candidateSessions.Any())
        {
            return Array.Empty<ScheduledInterviewSessionDto>();
        }

        var sessionIds = candidateSessions.Select(s => s.Id).ToList();
        var confirmedSessionIds = await _context.InterviewMatchingRequests
            .Where(m => sessionIds.Contains(m.ScheduledSessionId)
                && m.LiveSessionId != null
                && m.Status == "Confirmed")
            .Select(m => m.ScheduledSessionId)
            .Distinct()
            .ToListAsync();

        var confirmedSet = confirmedSessionIds.ToHashSet();

        // Add from direct links (defensive)
        var directlyLinkedLiveSessions = await _context.LiveInterviewSessions
            .Where(ls => sessionIds.Contains(ls.ScheduledSessionId ?? Guid.Empty))
            .ToListAsync();
        
        foreach (var liveSession in directlyLinkedLiveSessions)
        {
            if (liveSession.ScheduledSessionId.HasValue)
            {
                confirmedSet.Add(liveSession.ScheduledSessionId.Value);
            }
        }

        var pastSessions = candidateSessions
            .Where(s =>
            {
                // Primary: only sessions that have a confirmed live session.
                if (confirmedSet.Contains(s.Id)) return true;
                // Defensive: if session is directly linked to a live session, include it.
                return s.LiveSession != null;
            })
            .ToList();

        var dtos = new List<ScheduledInterviewSessionDto>();
        foreach (var session in pastSessions)
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

        // Cannot cancel already completed sessions
        if (session.Status == "Completed")
        {
            return false;
        }

        session.Status = "Cancelled";
        session.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return true;
    }

    // Practice with a friend (no scheduling / no matching queue)
    public async Task<(Guid LiveSessionId, Guid CreatorScheduledSessionId, string InterviewType, Guid? ActiveQuestionId)> CreateFriendInterviewAsync(
        Guid userId,
        CreateFriendInterviewDto dto)
    {
        // Validate user exists
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {userId} not found");
        }

        var interviewType = (dto.InterviewType ?? string.Empty).Trim().ToLowerInvariant();
        var interviewLevel = (dto.InterviewLevel ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(interviewType))
        {
            throw new InvalidOperationException("InterviewType is required.");
        }
        if (string.IsNullOrWhiteSpace(interviewLevel))
        {
            throw new InvalidOperationException("InterviewLevel is required.");
        }

        // Create a scheduled session record for the creator (used for history grids + metadata)
        var scheduledSession = new ScheduledInterviewSession
        {
            UserId = userId,
            InterviewType = interviewType,
            PracticeType = "friend",
            InterviewLevel = interviewLevel,
            ScheduledStartAt = DateTime.UtcNow,
            Status = "InProgress",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assign a question to the scheduled session for coding/sql (optional reference)
        if (interviewType == "data-structures-algorithms" || interviewType == "sql" || interviewType == "data-science-ml")
        {
            var assignedQuestionId = await SelectRandomQuestionAsync(interviewType);
            if (assignedQuestionId.HasValue)
            {
                scheduledSession.AssignedQuestionId = assignedQuestionId.Value;
            }
        }

        _context.ScheduledInterviewSessions.Add(scheduledSession);

        // Determine questions for the live session
        Guid? firstQuestionId = null;
        Guid? secondQuestionId = null;
        if (interviewType != "system-design")
        {
            firstQuestionId = await SelectRandomQuestionAsync(interviewType);
            if (!firstQuestionId.HasValue)
            {
                throw new InvalidOperationException("Could not find questions for the interview type.");
            }
            secondQuestionId = await SelectRandomQuestionAsync(interviewType, firstQuestionId);
        }

        // System design uses a shared Excalidraw room for both users
        string? interviewerRoomId = null;
        string? intervieweeRoomId = null;
        if (interviewType == "system-design")
        {
            var sharedRoomId = GenerateExcalidrawRoomId();
            interviewerRoomId = sharedRoomId;
            intervieweeRoomId = sharedRoomId;
        }

        var liveSession = new LiveInterviewSession
        {
            Id = Guid.NewGuid(),
            ScheduledSessionId = scheduledSession.Id,
            FirstQuestionId = firstQuestionId,
            SecondQuestionId = secondQuestionId,
            ActiveQuestionId = firstQuestionId,
            Status = "InProgress",
            StartedAt = DateTime.UtcNow,
            InterviewerRoomId = interviewerRoomId,
            IntervieweeRoomId = intervieweeRoomId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.LiveInterviewSessions.Add(liveSession);

        // Add creator as participant (Interviewer by default)
        _context.LiveInterviewParticipants.Add(new LiveInterviewParticipant
        {
            Id = Guid.NewGuid(),
            LiveSessionId = liveSession.Id,
            UserId = userId,
            Role = "Interviewer",
            IsActive = true,
            JoinedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        // Award coins to question creators when their questions are used in interviews
        await AwardCoinsForQuestionUsageAsync(firstQuestionId, liveSession.Id);
        await AwardCoinsForQuestionUsageAsync(secondQuestionId, liveSession.Id);

        return (liveSession.Id, scheduledSession.Id, interviewType, liveSession.ActiveQuestionId);
    }

    public async Task<LiveInterviewSessionDto> JoinFriendInterviewAsync(Guid liveSessionId, Guid userId)
    {
        var session = await _context.LiveInterviewSessions
            .Include(s => s.FirstQuestion)
            .Include(s => s.SecondQuestion)
            .Include(s => s.ScheduledSession)
            .Include(s => s.Participants)
            .FirstOrDefaultAsync(s => s.Id == liveSessionId);

        if (session == null)
        {
            throw new KeyNotFoundException("Session not found.");
        }

        if (session.ScheduledSession == null || !string.Equals(session.ScheduledSession.PracticeType, "friend", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("This session is not a 'practice with a friend' session.");
        }

        // If already a participant, just return the session
        if (session.Participants.Any(p => p.UserId == userId))
        {
            var existing = await _context.LiveInterviewSessions
                .Include(s => s.FirstQuestion)
                .Include(s => s.SecondQuestion)
                .Include(s => s.Participants)
                    .ThenInclude(p => p.User)
                .Include(s => s.ScheduledSession)
                .FirstOrDefaultAsync(s => s.Id == liveSessionId);

            return await MapToLiveSessionDtoAsync(existing!, userId);
        }

        if (session.Participants.Count >= 2)
        {
            throw new InvalidOperationException("This session already has two participants.");
        }

        var creatorScheduledSession = session.ScheduledSession;
        var creatorUserId = creatorScheduledSession.UserId;

        // Create scheduled session for the joining user so it appears in their history
        var joinerScheduledSession = new ScheduledInterviewSession
        {
            UserId = userId,
            InterviewType = creatorScheduledSession.InterviewType,
            PracticeType = "friend",
            InterviewLevel = creatorScheduledSession.InterviewLevel,
            ScheduledStartAt = creatorScheduledSession.ScheduledStartAt,
            Status = "InProgress",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // For coding/sql, set assigned question for joiner if we have a second question
        if ((creatorScheduledSession.InterviewType == "data-structures-algorithms" || creatorScheduledSession.InterviewType == "sql" || creatorScheduledSession.InterviewType == "data-science-ml")
            && session.SecondQuestionId.HasValue)
        {
            joinerScheduledSession.AssignedQuestionId = session.SecondQuestionId.Value;
        }

        _context.ScheduledInterviewSessions.Add(joinerScheduledSession);

        // Add the joiner as a participant
        _context.LiveInterviewParticipants.Add(new LiveInterviewParticipant
        {
            Id = Guid.NewGuid(),
            LiveSessionId = session.Id,
            UserId = userId,
            Role = "Interviewee",
            IsActive = true,
            JoinedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        // Create matching records (Confirmed) so the interview appears in both users' "Past interviews"
        var existingConfirmed = await _context.InterviewMatchingRequests
            .AnyAsync(m => m.LiveSessionId == session.Id && m.Status == "Confirmed");

        if (!existingConfirmed)
        {
            var now = DateTime.UtcNow;
            var expiresAt = now.AddHours(6);

            _context.InterviewMatchingRequests.Add(new InterviewMatchingRequest
            {
                Id = Guid.NewGuid(),
                UserId = creatorUserId,
                ScheduledSessionId = creatorScheduledSession.Id,
                InterviewType = creatorScheduledSession.InterviewType,
                PracticeType = "friend",
                InterviewLevel = creatorScheduledSession.InterviewLevel,
                ScheduledStartAt = creatorScheduledSession.ScheduledStartAt,
                Status = "Confirmed",
                MatchedUserId = userId,
                LiveSessionId = session.Id,
                UserConfirmed = true,
                MatchedUserConfirmed = true,
                ExpiresAt = expiresAt,
                CreatedAt = now,
                UpdatedAt = now
            });

            _context.InterviewMatchingRequests.Add(new InterviewMatchingRequest
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ScheduledSessionId = joinerScheduledSession.Id,
                InterviewType = creatorScheduledSession.InterviewType,
                PracticeType = "friend",
                InterviewLevel = creatorScheduledSession.InterviewLevel,
                ScheduledStartAt = creatorScheduledSession.ScheduledStartAt,
                Status = "Confirmed",
                MatchedUserId = creatorUserId,
                LiveSessionId = session.Id,
                UserConfirmed = true,
                MatchedUserConfirmed = true,
                ExpiresAt = expiresAt,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        await _context.SaveChangesAsync();

        // Reload with all includes for DTO
        var updated = await _context.LiveInterviewSessions
            .Include(s => s.FirstQuestion)
            .Include(s => s.SecondQuestion)
            .Include(s => s.Participants)
                .ThenInclude(p => p.User)
            .Include(s => s.ScheduledSession)
            .FirstOrDefaultAsync(s => s.Id == liveSessionId);

        return await MapToLiveSessionDtoAsync(updated!, userId);
    }

    // Live Sessions

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

        // Award coins to question creator when their question is used in interview
        await AwardCoinsForQuestionUsageAsync(newQuestionId, sessionId);

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
            .Include(s => s.ScheduledSession) // Include to check PracticeType
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

        // Award coins to all participants for completing the interview
        // Only award coins for scheduled interviews (peers/expert), NOT friend practice sessions
        var isFriendInterview = session.ScheduledSession != null && 
                                session.ScheduledSession.PracticeType.Equals("friend", StringComparison.OrdinalIgnoreCase);
        
        if (!isFriendInterview)
        {
            foreach (var p in session.Participants)
            {
                try
                {
                    await _coinService.AwardCoinsAsync(
                        p.UserId,
                        AchievementTypes.InterviewCompleted,
                        "Completed a mock interview",
                        sessionId,
                        "LiveInterviewSession");
                    
                    _logger.LogInformation("Awarded coins to user {UserId} for completing interview {SessionId}", 
                        p.UserId, sessionId);
                }
                catch (Exception ex)
                {
                    // Log but don't fail the interview end operation
                    _logger.LogWarning(ex, "Failed to award coins to user {UserId} for interview {SessionId}", 
                        p.UserId, sessionId);
                }
            }
        }
        else
        {
            _logger.LogInformation("Skipping coin award for friend practice session {SessionId}", sessionId);
        }

        // Check and complete referrals for first-time interview completers (non-friend interviews only)
        if (!isFriendInterview)
        {
            foreach (var p in session.Participants)
            {
                try
                {
                    // Check if this is the user's first completed interview
                    var completedCount = await _context.ScheduledInterviewSessions
                        .Where(s => s.UserId == p.UserId && s.Status == "Completed" && s.PracticeType != "friend")
                        .CountAsync();

                    if (completedCount == 1) // Just completed their first interview
                    {
                        var referralService = _serviceProvider.GetRequiredService<IReferralService>();
                        await referralService.CompleteReferralAsync(p.UserId);
                        _logger.LogInformation("Checked referral completion for user {UserId} after first interview", p.UserId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to check/complete referral for user {UserId}", p.UserId);
                }
            }
        }

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
                .Include(s => s.LiveSession) // Include existing LiveSession if any
                .ToListAsync();

            foreach (var scheduledSession in scheduledSessions)
            {
                scheduledSession.Status = "Completed";
                scheduledSession.UpdatedAt = DateTime.UtcNow;
                // Explicitly link LiveSession navigation property so GetPastSessionsAsync can find it
                scheduledSession.LiveSession = liveSession;
                // Also ensure the foreign key relationship is set if there's a foreign key column
                // Note: The relationship is managed via navigation property, but we need to ensure it's tracked
                _context.Entry(scheduledSession).Reference(s => s.LiveSession).IsLoaded = true;
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

        // Award coins for feedback submission (only for new feedback, not updates)
        if (existingFeedback == null)
        {
            try
            {
                await _coinService.AwardCoinsAsync(
                    userId,
                    AchievementTypes.FeedbackSubmitted,
                    "Submitted interview feedback",
                    feedback.Id,
                    "InterviewFeedback");
                
                _logger.LogInformation("Awarded coins to user {UserId} for submitting feedback {FeedbackId}", 
                    userId, feedback.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to award coins to user {UserId} for feedback {FeedbackId}", 
                    userId, feedback.Id);
            }

            // Award bonus coins if the reviewee was a great interview partner (5 stars only)
            if (dto.InterviewerPerformanceRating == 5)
            {
                try
                {
                    await _coinService.AwardCoinsAsync(
                        dto.RevieweeId,
                        AchievementTypes.GreatMockInterviewPartner,
                        "Received 5-star rating as interview partner",
                        feedback.Id,
                        "InterviewFeedback");
                    
                    _logger.LogInformation("Awarded bonus coins to user {UserId} for being a great partner (5 stars)", 
                        dto.RevieweeId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to award bonus coins to user {UserId}", dto.RevieweeId);
                }
            }
        }

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
    private async Task<Guid?> SelectRandomQuestionAsync(string interviewType, Guid? excludeQuestionId = null)
    {
        // Map interview types to question types
        var questionTypeMap = new Dictionary<string, string>
        {
            { "data-structures-algorithms", "Coding" },
            { "system-design", "System Design" },
            { "behavioral", "Behavioral" },
            { "product-management", "Behavioral" }, // Product management uses behavioral questions
            { "sql", "SQL" }, // SQL interviews use SQL questions
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
            InterviewType = session.ScheduledSession?.InterviewType,
            FirstQuestionId = session.FirstQuestionId,
            SecondQuestionId = session.SecondQuestionId,
            ActiveQuestionId = session.ActiveQuestionId,
            Status = session.Status,
            StartedAt = session.StartedAt,
            EndedAt = session.EndedAt,
            InterviewerRoomId = session.InterviewerRoomId,
            IntervieweeRoomId = session.IntervieweeRoomId,
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

    /// <summary>
    /// Generates an Excalidraw room ID in the format: roomId,key
    /// Similar to Excalidraw's format: "35a7b3f8f24f22d21f18,gaiLKrrJVtanONO5UiU2UA"
    /// </summary>
    private static string GenerateExcalidrawRoomId()
    {
        const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var roomId = new string(Enumerable.Repeat(chars, 22)
            .Select(s => s[_random.Next(s.Length)]).ToArray());
        var key = new string(Enumerable.Repeat(chars, 22)
            .Select(s => s[_random.Next(s.Length)]).ToArray());
        return $"{roomId},{key}";
    }

    /// <summary>
    /// Awards coins to a question creator when their question is used in an interview
    /// </summary>
    private async Task AwardCoinsForQuestionUsageAsync(Guid? questionId, Guid sessionId)
    {
        if (!questionId.HasValue)
        {
            return; // No question to award coins for
        }

        try
        {
            // Get the question to find its creator
            var question = await _context.InterviewQuestions
                .AsNoTracking()
                .FirstOrDefaultAsync(q => q.Id == questionId.Value);

            if (question == null || !question.CreatedBy.HasValue)
            {
                _logger.LogDebug("Question {QuestionId} not found or has no creator", questionId.Value);
                return;
            }

            // Award coins to the question creator
            await _coinService.AwardCoinsAsync(
                question.CreatedBy.Value,
                AchievementTypes.QuestionInAnotherInterview,
                $"Your question '{question.Title}' was used in an interview",
                sessionId,
                "LiveInterviewSession");

            _logger.LogInformation(
                "Awarded coins to user {UserId} for question {QuestionId} used in interview {SessionId}",
                question.CreatedBy.Value, questionId.Value, sessionId);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Maximum"))
        {
            // User already received coins for this question in this session - this is fine
            _logger.LogDebug(
                "Question {QuestionId} already awarded coins for session {SessionId}",
                questionId.Value, sessionId);
        }
        catch (Exception ex)
        {
            // Log but don't fail the interview session creation/update
            _logger.LogError(ex,
                "Failed to award coins for question {QuestionId} used in session {SessionId}",
                questionId.Value, sessionId);
        }
    }
}

