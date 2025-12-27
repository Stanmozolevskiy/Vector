using Microsoft.EntityFrameworkCore;
using Vector.Api.Data;
using Vector.Api.DTOs.Solution;
using Vector.Api.Models;

namespace Vector.Api.Services;

public class CodeDraftService : ICodeDraftService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CodeDraftService> _logger;

    public CodeDraftService(ApplicationDbContext context, ILogger<CodeDraftService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CodeDraftDto?> GetCodeDraftAsync(Guid userId, Guid questionId, string language)
    {
        var draft = await _context.UserCodeDrafts
            .FirstOrDefaultAsync(d => 
                d.UserId == userId && 
                d.QuestionId == questionId && 
                d.Language == language);

        if (draft == null)
        {
            return null;
        }

        return new CodeDraftDto
        {
            Id = draft.Id,
            QuestionId = draft.QuestionId,
            Language = draft.Language,
            Code = draft.Code,
            UpdatedAt = draft.UpdatedAt
        };
    }

    public async Task<CodeDraftDto> SaveCodeDraftAsync(Guid userId, SaveCodeDraftDto dto)
    {
        var existingDraft = await _context.UserCodeDrafts
            .FirstOrDefaultAsync(d => 
                d.UserId == userId && 
                d.QuestionId == dto.QuestionId && 
                d.Language == dto.Language);

        if (existingDraft != null)
        {
            // Update existing draft
            existingDraft.Code = dto.Code;
            existingDraft.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new CodeDraftDto
            {
                Id = existingDraft.Id,
                QuestionId = existingDraft.QuestionId,
                Language = existingDraft.Language,
                Code = existingDraft.Code,
                UpdatedAt = existingDraft.UpdatedAt
            };
        }
        else
        {
            // Create new draft
            var draft = new UserCodeDraft
            {
                UserId = userId,
                QuestionId = dto.QuestionId,
                Language = dto.Language,
                Code = dto.Code,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.UserCodeDrafts.Add(draft);
            await _context.SaveChangesAsync();

            return new CodeDraftDto
            {
                Id = draft.Id,
                QuestionId = draft.QuestionId,
                Language = draft.Language,
                Code = draft.Code,
                UpdatedAt = draft.UpdatedAt
            };
        }
    }

    public async Task<bool> DeleteCodeDraftAsync(Guid userId, Guid questionId, string language)
    {
        var draft = await _context.UserCodeDrafts
            .FirstOrDefaultAsync(d => 
                d.UserId == userId && 
                d.QuestionId == questionId && 
                d.Language == language);

        if (draft == null)
        {
            return false;
        }

        _context.UserCodeDrafts.Remove(draft);
        await _context.SaveChangesAsync();
        return true;
    }
}

