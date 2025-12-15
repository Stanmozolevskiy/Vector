using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Vector.Api.Data;
using Vector.Api.DTOs.Question;
using Vector.Api.Models;

namespace Vector.Api.Services;

public class QuestionService : IQuestionService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<QuestionService> _logger;

    public QuestionService(ApplicationDbContext context, ILogger<QuestionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<InterviewQuestion?> GetQuestionByIdAsync(Guid questionId)
    {
        return await _context.InterviewQuestions
            .Include(q => q.TestCases.Where(t => !t.IsHidden))
            .Include(q => q.Solutions.Where(s => s.IsOfficial))
            .FirstOrDefaultAsync(q => q.Id == questionId && q.IsActive);
    }

    public async Task<IEnumerable<InterviewQuestion>> GetQuestionsAsync(QuestionFilterDto? filter = null)
    {
        var query = _context.InterviewQuestions.AsQueryable();

        if (filter != null)
        {
            if (filter.IsActive.HasValue)
            {
                query = query.Where(q => q.IsActive == filter.IsActive.Value);
            }

            if (!string.IsNullOrEmpty(filter.Search))
            {
                query = query.Where(q => 
                    q.Title.Contains(filter.Search) || 
                    q.Description.Contains(filter.Search));
            }

            if (!string.IsNullOrEmpty(filter.QuestionType))
            {
                query = query.Where(q => q.QuestionType == filter.QuestionType);
            }

            if (!string.IsNullOrEmpty(filter.Category))
            {
                query = query.Where(q => q.Category == filter.Category);
            }

            if (!string.IsNullOrEmpty(filter.Difficulty))
            {
                query = query.Where(q => q.Difficulty == filter.Difficulty);
            }

            if (filter.Companies != null && filter.Companies.Any())
            {
                // Filter by company tags (stored as JSON)
                foreach (var company in filter.Companies)
                {
                    query = query.Where(q => q.CompanyTags != null && q.CompanyTags.Contains(company));
                }
            }
        }

        return await query
            .OrderBy(q => q.Title)
            .ToListAsync();
    }

    public async Task<InterviewQuestion> CreateQuestionAsync(CreateQuestionDto dto, Guid createdBy)
    {
        // Validation
        ValidateQuestion(dto);

        var question = new InterviewQuestion
        {
            Id = Guid.NewGuid(),
            Title = dto.Title,
            Description = dto.Description,
            Difficulty = dto.Difficulty,
            QuestionType = dto.QuestionType,
            Category = dto.Category,
            CompanyTags = dto.CompanyTags != null ? JsonSerializer.Serialize(dto.CompanyTags) : null,
            Tags = dto.Tags != null ? JsonSerializer.Serialize(dto.Tags) : null,
            Constraints = dto.Constraints,
            Examples = dto.Examples != null ? JsonSerializer.Serialize(dto.Examples) : null,
            Hints = dto.Hints != null ? JsonSerializer.Serialize(dto.Hints) : null,
            TimeComplexityHint = dto.TimeComplexityHint,
            SpaceComplexityHint = dto.SpaceComplexityHint,
            AcceptanceRate = dto.AcceptanceRate,
            IsActive = true,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.InterviewQuestions.Add(question);
        await _context.SaveChangesAsync();

        return question;
    }

    private void ValidateQuestion(CreateQuestionDto dto)
    {
        // Validate difficulty
        var validDifficulties = new[] { "Easy", "Medium", "Hard" };
        if (!validDifficulties.Contains(dto.Difficulty, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Invalid difficulty. Must be one of: {string.Join(", ", validDifficulties)}");
        }

        // Validate question type
        var validQuestionTypes = new[] { "Coding", "System Design", "Behavioral" };
        if (!validQuestionTypes.Contains(dto.QuestionType, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Invalid question type. Must be one of: {string.Join(", ", validQuestionTypes)}");
        }

        // Validate category (common categories)
        var validCategories = new[] { "Arrays", "Strings", "Trees", "Graphs", "Dynamic Programming", "Greedy", "Backtracking", "Math", "Bit Manipulation", "Sorting", "Searching", "Hash Tables", "Linked Lists", "Stacks", "Queues", "Heaps", "System Design", "Behavioral" };
        if (!string.IsNullOrEmpty(dto.Category) && !validCategories.Contains(dto.Category, StringComparer.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Category '{Category}' is not in the standard list. Allowing custom category.", dto.Category);
        }
    }

    public async Task<InterviewQuestion> UpdateQuestionAsync(Guid questionId, UpdateQuestionDto dto, Guid updatedBy)
    {
        var question = await _context.InterviewQuestions.FindAsync(questionId);
        if (question == null)
        {
            throw new KeyNotFoundException($"Question with ID {questionId} not found");
        }

        if (!string.IsNullOrEmpty(dto.Title))
            question.Title = dto.Title;
        if (!string.IsNullOrEmpty(dto.Description))
            question.Description = dto.Description;
        if (!string.IsNullOrEmpty(dto.Difficulty))
            question.Difficulty = dto.Difficulty;
        if (!string.IsNullOrEmpty(dto.QuestionType))
            question.QuestionType = dto.QuestionType;
        if (!string.IsNullOrEmpty(dto.Category))
            question.Category = dto.Category;
        if (dto.CompanyTags != null)
            question.CompanyTags = JsonSerializer.Serialize(dto.CompanyTags);
        if (dto.Tags != null)
            question.Tags = JsonSerializer.Serialize(dto.Tags);
        if (dto.Constraints != null)
            question.Constraints = dto.Constraints;
        if (dto.Examples != null)
            question.Examples = JsonSerializer.Serialize(dto.Examples);
        if (dto.Hints != null)
            question.Hints = JsonSerializer.Serialize(dto.Hints);
        if (dto.TimeComplexityHint != null)
            question.TimeComplexityHint = dto.TimeComplexityHint;
        if (dto.SpaceComplexityHint != null)
            question.SpaceComplexityHint = dto.SpaceComplexityHint;
        if (dto.AcceptanceRate.HasValue)
            question.AcceptanceRate = dto.AcceptanceRate;
        if (dto.IsActive.HasValue)
            question.IsActive = dto.IsActive.Value;

        question.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return question;
    }

    public async Task<bool> DeleteQuestionAsync(Guid questionId)
    {
        var question = await _context.InterviewQuestions.FindAsync(questionId);
        if (question == null)
        {
            return false;
        }

        _context.InterviewQuestions.Remove(question);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<QuestionTestCase>> GetTestCasesAsync(Guid questionId, bool includeHidden = false)
    {
        var query = _context.QuestionTestCases
            .Where(t => t.QuestionId == questionId);

        if (!includeHidden)
        {
            query = query.Where(t => !t.IsHidden);
        }

        return await query
            .OrderBy(t => t.TestCaseNumber)
            .ToListAsync();
    }

    public async Task<QuestionTestCase> AddTestCaseAsync(Guid questionId, CreateTestCaseDto dto)
    {
        // Validate test case
        ValidateTestCase(dto);

        // Ensure question exists
        var question = await _context.InterviewQuestions.FindAsync(questionId);
        if (question == null)
        {
            throw new KeyNotFoundException($"Question with ID {questionId} not found");
        }

        var testCase = new QuestionTestCase
        {
            Id = Guid.NewGuid(),
            QuestionId = questionId,
            TestCaseNumber = dto.TestCaseNumber,
            // Store as plain string for Judge0 compatibility (stdin format)
            Input = dto.Input.Trim(),
            // Store as plain string for Judge0 compatibility (expected_output format)
            ExpectedOutput = dto.ExpectedOutput.Trim(),
            IsHidden = dto.IsHidden,
            Explanation = dto.Explanation,
            CreatedAt = DateTime.UtcNow
        };

        _context.QuestionTestCases.Add(testCase);
        await _context.SaveChangesAsync();

        return testCase;
    }

    private void ValidateTestCase(CreateTestCaseDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Input))
        {
            throw new ArgumentException("Test case input cannot be empty");
        }

        if (string.IsNullOrWhiteSpace(dto.ExpectedOutput))
        {
            throw new ArgumentException("Test case expected output cannot be empty");
        }

        if (dto.TestCaseNumber < 1)
        {
            throw new ArgumentException("Test case number must be greater than 0");
        }
    }

    public async Task<IEnumerable<QuestionSolution>> GetSolutionsAsync(Guid questionId, string? language = null)
    {
        var query = _context.QuestionSolutions
            .Where(s => s.QuestionId == questionId);

        if (!string.IsNullOrEmpty(language))
        {
            query = query.Where(s => s.Language == language);
        }

        return await query
            .OrderByDescending(s => s.IsOfficial)
            .ThenBy(s => s.Language)
            .ToListAsync();
    }

    public async Task<QuestionSolution> AddSolutionAsync(Guid questionId, CreateSolutionDto dto, Guid createdBy)
    {
        var solution = new QuestionSolution
        {
            Id = Guid.NewGuid(),
            QuestionId = questionId,
            Language = dto.Language,
            Code = dto.Code,
            Explanation = dto.Explanation,
            TimeComplexity = dto.TimeComplexity,
            SpaceComplexity = dto.SpaceComplexity,
            IsOfficial = dto.IsOfficial,
            CreatedBy = dto.IsOfficial ? null : createdBy,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.QuestionSolutions.Add(solution);
        await _context.SaveChangesAsync();

        return solution;
    }
}

