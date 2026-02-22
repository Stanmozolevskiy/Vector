using Vector.Api.DTOs.Question;
using Vector.Api.Models;

namespace Vector.Api.Services;

public interface IQuestionService
{
    Task<InterviewQuestion?> GetQuestionByIdAsync(Guid questionId);
    Task<IEnumerable<InterviewQuestion>> GetQuestionsAsync(QuestionFilterDto? filter = null);
    Task<List<RelatedQuestionDto>> GetRelatedQuestionsAsync(IEnumerable<Guid> questionIds);
    Task<InterviewQuestion> CreateQuestionAsync(CreateQuestionDto dto, Guid createdBy);
    Task<InterviewQuestion?> UpdateQuestionAsync(Guid questionId, UpdateQuestionDto dto, Guid updatedBy);
    Task<bool> DeleteQuestionAsync(Guid questionId);
    Task<IEnumerable<QuestionTestCase>> GetTestCasesAsync(Guid questionId, bool includeHidden = false);
    Task<QuestionTestCase> AddTestCaseAsync(Guid questionId, CreateTestCaseDto dto);
    Task<IEnumerable<QuestionSolution>> GetSolutionsAsync(Guid questionId, string? language = null);
    Task<QuestionSolution> AddSolutionAsync(Guid questionId, CreateSolutionDto dto, Guid createdBy);
    Task<InterviewQuestion> ApproveQuestionAsync(Guid questionId, Guid approvedBy);
    Task<InterviewQuestion> RejectQuestionAsync(Guid questionId, Guid rejectedBy, string? rejectionReason = null);
    Task<IEnumerable<InterviewQuestion>> GetPendingQuestionsAsync();
    
    // Bookmark methods
    Task<QuestionBookmark> AddBookmarkAsync(Guid questionId, Guid userId, string? notes = null);
    Task<bool> RemoveBookmarkAsync(Guid questionId, Guid userId);
    Task<IEnumerable<InterviewQuestion>> GetBookmarkedQuestionsAsync(Guid userId);
    Task<bool> IsQuestionBookmarkedAsync(Guid questionId, Guid userId);
}

