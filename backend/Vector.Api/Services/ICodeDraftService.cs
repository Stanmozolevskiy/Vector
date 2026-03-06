using Vector.Api.DTOs.Solution;

namespace Vector.Api.Services;

public interface ICodeDraftService
{
    Task<CodeDraftDto?> GetCodeDraftAsync(Guid userId, Guid questionId, string language);
    Task<CodeDraftDto> SaveCodeDraftAsync(Guid userId, SaveCodeDraftDto dto);
    Task<bool> DeleteCodeDraftAsync(Guid userId, Guid questionId, string language);
}

