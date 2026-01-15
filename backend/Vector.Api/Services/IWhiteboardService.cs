using Vector.Api.DTOs.Whiteboard;
using Vector.Api.Models;

namespace Vector.Api.Services;

public interface IWhiteboardService
{
    Task<WhiteboardData?> GetWhiteboardDataAsync(Guid userId, Guid? questionId = null);
    Task<WhiteboardData?> GetWhiteboardDataBySessionAsync(Guid sessionId);
    Task<WhiteboardData?> GetWhiteboardDataBySessionAndUserAsync(Guid sessionId, Guid userId);
    Task<WhiteboardData> SaveWhiteboardDataAsync(Guid userId, SaveWhiteboardDataDto dto);
    Task<WhiteboardData> SaveWhiteboardDataBySessionAsync(Guid sessionId, SaveWhiteboardDataDto dto);
    Task<bool> DeleteWhiteboardDataAsync(Guid userId, Guid? questionId = null);
}
