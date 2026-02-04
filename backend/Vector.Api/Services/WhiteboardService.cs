using Microsoft.EntityFrameworkCore;
using Vector.Api.Data;
using Vector.Api.DTOs.Whiteboard;
using Vector.Api.Models;

namespace Vector.Api.Services;

public class WhiteboardService : IWhiteboardService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<WhiteboardService> _logger;

    public WhiteboardService(ApplicationDbContext context, ILogger<WhiteboardService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<WhiteboardData?> GetWhiteboardDataAsync(Guid userId, Guid? questionId = null)
    {
        try
        {
            var query = _context.WhiteboardData
                .AsNoTracking()
                .Where(w => w.UserId == userId && w.SessionId == null); // Only user-based whiteboards

            // If questionId is provided, get whiteboard for that question, otherwise get main whiteboard
            if (questionId.HasValue)
            {
                query = query.Where(w => w.QuestionId == questionId.Value);
            }
            else
            {
                query = query.Where(w => w.QuestionId == null && w.SessionId == null);
            }

            var whiteboardData = await query
                .OrderByDescending(w => w.UpdatedAt)
                .FirstOrDefaultAsync();

            return whiteboardData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting whiteboard data for user {UserId} and question {QuestionId}", userId, questionId);
            throw;
        }
    }

    public async Task<WhiteboardData> SaveWhiteboardDataAsync(Guid userId, SaveWhiteboardDataDto dto)
    {
        try
        {
            Guid? questionId = string.IsNullOrWhiteSpace(dto.QuestionId) 
                ? null 
                : Guid.TryParse(dto.QuestionId, out var parsed) ? parsed : null;

            // Check if whiteboard data already exists for this user and question (not session-based)
            var existingUserWhiteboard = await _context.WhiteboardData
                .FirstOrDefaultAsync(w => w.UserId == userId && w.QuestionId == questionId && w.SessionId == null);

            if (existingUserWhiteboard != null)
            {
                // Update existing whiteboard
                existingUserWhiteboard.Elements = dto.Elements;
                existingUserWhiteboard.AppState = dto.AppState;
                existingUserWhiteboard.Files = dto.Files;
                existingUserWhiteboard.UpdatedAt = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
                _logger.LogInformation("Updated whiteboard data for user {UserId} and question {QuestionId}", userId, questionId);
                
                return existingUserWhiteboard;
            }
            else
            {
                // Create new whiteboard
                var whiteboardData = new WhiteboardData
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    QuestionId = questionId,
                    Elements = dto.Elements,
                    AppState = dto.AppState,
                    Files = dto.Files,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                };

                _context.WhiteboardData.Add(whiteboardData);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Created whiteboard data for user {UserId} and question {QuestionId}", userId, questionId);
                
                return whiteboardData;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving whiteboard data for user {UserId}", userId);
            throw;
        }
    }

    public async Task<WhiteboardData?> GetWhiteboardDataBySessionAsync(Guid sessionId)
    {
        try
        {
            var whiteboardData = await _context.WhiteboardData
                .AsNoTracking()
                .Where(w => w.SessionId == sessionId)
                .OrderByDescending(w => w.UpdatedAt)
                .FirstOrDefaultAsync();

            return whiteboardData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting whiteboard data for session {SessionId}", sessionId);
            throw;
        }
    }

    public async Task<WhiteboardData> SaveWhiteboardDataBySessionAsync(Guid sessionId, SaveWhiteboardDataDto dto)
    {
        try
        {
            // Check if whiteboard data already exists for this session
            var existingWhiteboard = await _context.WhiteboardData
                .FirstOrDefaultAsync(w => w.SessionId == sessionId);

            if (existingWhiteboard != null)
            {
                // Update existing whiteboard (shared for all users in session)
                existingWhiteboard.Elements = dto.Elements;
                existingWhiteboard.AppState = dto.AppState;
                existingWhiteboard.Files = dto.Files;
                existingWhiteboard.UpdatedAt = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
                _logger.LogInformation("Updated session whiteboard data for session {SessionId}", sessionId);
                
                return existingWhiteboard;
            }
            else
            {
                // Create new session-based whiteboard (shared between all users in session)
                var whiteboardData = new WhiteboardData
                {
                    Id = Guid.NewGuid(),
                    UserId = null, // Session-based, no specific user owner
                    SessionId = sessionId,
                    QuestionId = null,
                    Elements = dto.Elements,
                    AppState = dto.AppState,
                    Files = dto.Files,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                };

                _context.WhiteboardData.Add(whiteboardData);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Created session whiteboard data for session {SessionId}", sessionId);
                
                return whiteboardData;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving session whiteboard data for session {SessionId}", sessionId);
            throw;
        }
    }

    public async Task<bool> DeleteWhiteboardDataAsync(Guid userId, Guid? questionId = null)
    {
        try
        {
            var query = _context.WhiteboardData.Where(w => w.UserId == userId);

            if (questionId.HasValue)
            {
                query = query.Where(w => w.QuestionId == questionId.Value);
            }
            else
            {
                query = query.Where(w => w.QuestionId == null);
            }

            var whiteboardData = await query.FirstOrDefaultAsync();

            if (whiteboardData == null)
            {
                return false;
            }

            _context.WhiteboardData.Remove(whiteboardData);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Deleted whiteboard data for user {UserId} and question {QuestionId}", userId, questionId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting whiteboard data for user {UserId}", userId);
            throw;
        }
    }
}
