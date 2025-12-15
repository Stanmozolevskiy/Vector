using Vector.Api.DTOs.Question;
using Vector.Api.Models;

namespace Vector.Api.Services;

public interface IQuestionService
{
    Task<InterviewQuestion?> GetQuestionByIdAsync(Guid questionId);
    Task<IEnumerable<InterviewQuestion>> GetQuestionsAsync(QuestionFilterDto? filter = null);
    Task<InterviewQuestion> CreateQuestionAsync(CreateQuestionDto dto, Guid createdBy);
    Task<InterviewQuestion> UpdateQuestionAsync(Guid questionId, UpdateQuestionDto dto, Guid updatedBy);
    Task<bool> DeleteQuestionAsync(Guid questionId);
    Task<IEnumerable<QuestionTestCase>> GetTestCasesAsync(Guid questionId, bool includeHidden = false);
    Task<QuestionTestCase> AddTestCaseAsync(Guid questionId, CreateTestCaseDto dto);
    Task<IEnumerable<QuestionSolution>> GetSolutionsAsync(Guid questionId, string? language = null);
    Task<QuestionSolution> AddSolutionAsync(Guid questionId, CreateSolutionDto dto, Guid createdBy);
}

