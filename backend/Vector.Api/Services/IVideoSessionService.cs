using Vector.Api.Models;

namespace Vector.Api.Services;

public interface IVideoSessionService
{
    Task<VideoSession> CreateVideoSessionAsync(Guid sessionId);
    Task<VideoSession?> GetVideoSessionByTokenAsync(string token);
    Task<VideoSession?> GetVideoSessionBySessionIdAsync(Guid sessionId);
    Task<VideoSession> UpdateSignalingDataAsync(Guid videoSessionId, string signalingData);
    Task<bool> EndVideoSessionAsync(Guid videoSessionId);
}

