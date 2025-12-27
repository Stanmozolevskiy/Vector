using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using Vector.Api.Data;
using Vector.Api.Models;

namespace Vector.Api.Services;

public class VideoSessionService : IVideoSessionService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<VideoSessionService> _logger;

    public VideoSessionService(ApplicationDbContext context, ILogger<VideoSessionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<VideoSession> CreateVideoSessionAsync(Guid sessionId)
    {
        // Check if session exists
        var interviewSession = await _context.PeerInterviewSessions.FindAsync(sessionId);
        if (interviewSession == null)
        {
            throw new KeyNotFoundException($"Interview session with ID {sessionId} not found");
        }

        // Check if video session already exists
        var existingVideoSession = await _context.VideoSessions
            .FirstOrDefaultAsync(v => v.SessionId == sessionId && v.Status == "Active");

        if (existingVideoSession != null)
        {
            return existingVideoSession;
        }

        // Generate unique token
        var token = GenerateToken();

        var videoSession = new VideoSession
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            Token = token,
            Status = "Active",
            CreatedAt = DateTime.UtcNow
        };

        _context.VideoSessions.Add(videoSession);
        await _context.SaveChangesAsync();

        return videoSession;
    }

    public async Task<VideoSession?> GetVideoSessionByTokenAsync(string token)
    {
        return await _context.VideoSessions
            .Include(v => v.Session)
            .FirstOrDefaultAsync(v => v.Token == token);
    }

    public async Task<VideoSession?> GetVideoSessionBySessionIdAsync(Guid sessionId)
    {
        return await _context.VideoSessions
            .Include(v => v.Session)
            .FirstOrDefaultAsync(v => v.SessionId == sessionId && v.Status == "Active");
    }

    public async Task<VideoSession> UpdateSignalingDataAsync(Guid videoSessionId, string signalingData)
    {
        var videoSession = await _context.VideoSessions.FindAsync(videoSessionId);
        if (videoSession == null)
        {
            throw new KeyNotFoundException($"Video session with ID {videoSessionId} not found");
        }

        videoSession.SignalingData = signalingData;
        await _context.SaveChangesAsync();

        return videoSession;
    }

    public async Task<bool> EndVideoSessionAsync(Guid videoSessionId)
    {
        var videoSession = await _context.VideoSessions.FindAsync(videoSessionId);
        if (videoSession == null)
        {
            return false;
        }

        videoSession.Status = "Ended";
        videoSession.EndedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return true;
    }

    private string GenerateToken()
    {
        // Generate a secure random token
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[32];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }
}

