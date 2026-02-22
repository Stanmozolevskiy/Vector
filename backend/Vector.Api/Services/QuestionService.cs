using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Vector.Api.Data;
using Vector.Api.DTOs.Question;
using Vector.Api.Models;
using Vector.Api.Constants;

namespace Vector.Api.Services;

public class QuestionService : IQuestionService
{
    private readonly ApplicationDbContext _context;
    private readonly ICoinService _coinService;
    private readonly ILogger<QuestionService> _logger;

    public QuestionService(
        ApplicationDbContext context, 
        ICoinService coinService,
        ILogger<QuestionService> logger)
    {
        _context = context;
        _coinService = coinService;
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

            // Filter by approval status - if not specified, only show approved questions
            if (!string.IsNullOrEmpty(filter.ApprovalStatus))
            {
                query = query.Where(q => q.ApprovalStatus == filter.ApprovalStatus);
            }
            else
            {
                // By default, only show approved questions
                query = query.Where(q => q.ApprovalStatus == "Approved");
            }

            if (!string.IsNullOrEmpty(filter.Search))
            {
                var searchTerm = filter.Search.Trim();
                // Use case-insensitive search that translates to SQL
                query = query.Where(q => 
                    EF.Functions.Like(q.Title, $"%{searchTerm}%") || 
                    EF.Functions.Like(q.Description, $"%{searchTerm}%"));
            }

            // Map role to question type if role is provided and QuestionType is not
            if (!string.IsNullOrEmpty(filter.Role) && string.IsNullOrEmpty(filter.QuestionType))
            {
                var roleLower = filter.Role.Trim().ToLowerInvariant();
                var mappedTypes = roleLower switch
                {
                    "data-engineer" or "data engineer" or "de" => new[] { "SQL" },
                    "software-engineer" or "software engineer" or "swe" or "developer" => new[] { "Coding" },
                    "system-designer" or "system designer" or "architect" => new[] { "System Design" },
                    // PM roles can include both Product Management and Behavioral questions
                    "product-manager" or "product manager" or "pm" => new[] { "Product Management", "Behavioral" },
                    "engineering-manager" or "engineering manager" or "em" => new[] { "Behavioral" },
                    "technical-program-manager" or "technical program manager" or "tpm" => new[] { "Product Management", "Behavioral" },
                    _ => Array.Empty<string>()
                };

                if (mappedTypes.Length > 0)
                {
                    query = query.Where(q => mappedTypes.Contains(q.QuestionType));
                }
            }
            else if (!string.IsNullOrEmpty(filter.QuestionType))
            {
                query = query.Where(q => q.QuestionType == filter.QuestionType);
            }

            // Support both single category and multiple categories
            // Use array directly in Contains - EF Core can translate this to SQL IN clause
            if (filter.Categories != null && filter.Categories.Any())
            {
                // Normalize categories for case-insensitive matching
                var normalizedCategories = filter.Categories
                    .Select(c => c?.Trim())
                    .Where(c => !string.IsNullOrEmpty(c))
                    .ToList();
                
                // EF Core can translate array.Contains() to SQL IN clause
                query = query.Where(q => normalizedCategories.Contains(q.Category));
            }
            else if (!string.IsNullOrEmpty(filter.Category))
            {
                query = query.Where(q => q.Category == filter.Category);
            }

            // Support both single difficulty and multiple difficulties
            // Use array directly in Contains - EF Core can translate this to SQL IN clause
            if (filter.Difficulties != null && filter.Difficulties.Any())
            {
                // Normalize difficulties for case-insensitive matching
                var normalizedDifficulties = filter.Difficulties
                    .Select(d => d?.Trim())
                    .Where(d => !string.IsNullOrEmpty(d))
                    .ToList();
                
                // EF Core can translate array.Contains() to SQL IN clause
                // But we need to handle case-insensitive matching
                // Use a helper to build OR conditions that EF Core can translate
                query = query.Where(q => normalizedDifficulties.Contains(q.Difficulty));
            }
            else if (!string.IsNullOrEmpty(filter.Difficulty))
            {
                query = query.Where(q => q.Difficulty == filter.Difficulty);
            }

        }
        else
        {
            // If no filter, only show approved questions
            query = query.Where(q => q.ApprovalStatus == "Approved");
        }

        // Apply company filtering in memory after fetching other filters
        // This is necessary because JSON deserialization doesn't translate well to SQL
        
        // Get total count before pagination for potential future use
        var totalCount = await query.CountAsync();
        
        // Apply sorting before pagination
        query = query.OrderBy(q => q.Title);
        
        // Apply pagination if specified
        if (filter?.Page > 0 && filter?.PageSize > 0)
        {
            var skip = (filter.Page - 1) * filter.PageSize;
            query = query.Skip(skip).Take(filter.PageSize);
        }
        
        var results = await query.ToListAsync();

        if (filter?.Companies != null && filter.Companies.Any())
        {
            var companyList = filter.Companies.Select(c => c.Trim()).ToList();
            results = results.Where(q =>
            {
                if (string.IsNullOrEmpty(q.CompanyTags)) return false;
                try
                {
                    var tags = JsonSerializer.Deserialize<List<string>>(q.CompanyTags);
                    if (tags == null || !tags.Any()) return false;
                    // Check if any of the filter companies match any of the question's company tags (case-insensitive)
                    return tags.Any(tag => 
                        companyList.Any(filterCompany => 
                            string.Equals(tag?.Trim(), filterCompany, StringComparison.OrdinalIgnoreCase)));
                }
                catch
                {
                    return false;
                }
            }).ToList();
        }

        return results;
    }

    public async Task<InterviewQuestion> CreateQuestionAsync(CreateQuestionDto dto, Guid createdBy)
    {
        // Validation
        ValidateQuestion(dto);

        // Check if creator is admin or coach
        var creator = await _context.Users.FindAsync(createdBy);
        var isAdmin = creator?.Role?.ToLower() == "admin";
        
        // Admin-created questions are automatically approved
        // Coach-created questions require approval
        var approvalStatus = isAdmin ? "Approved" : "Pending";
        var approvedBy = isAdmin ? createdBy : (Guid?)null;
        var approvedAt = isAdmin ? DateTime.UtcNow : (DateTime?)null;

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
            VideoUrl = dto.VideoUrl,
            RoleTags = dto.RoleTags != null ? JsonSerializer.Serialize(dto.RoleTags) : null,
            RelatedQuestionIds = dto.RelatedQuestionIds != null ? JsonSerializer.Serialize(dto.RelatedQuestionIds) : null,
            RelatedCourseIds = dto.RelatedCourseIds != null ? JsonSerializer.Serialize(dto.RelatedCourseIds) : null,
            TimeComplexityHint = dto.TimeComplexityHint,
            SpaceComplexityHint = dto.SpaceComplexityHint,
            AcceptanceRate = dto.AcceptanceRate,
            IsActive = isAdmin,
            ApprovalStatus = approvalStatus,
            ApprovedBy = approvedBy,
            ApprovedAt = approvedAt,
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
        var validQuestionTypes = new[] { "Coding", "System Design", "Behavioral", "SQL", "Product Management" };
        if (!validQuestionTypes.Contains(dto.QuestionType, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Invalid question type. Must be one of: {string.Join(", ", validQuestionTypes)}");
        }

        // Validate category (common categories)
        var validCategories = new[] { "Arrays", "Strings", "Trees", "Graphs", "Dynamic Programming", "Greedy", "Backtracking", "Math", "Bit Manipulation", "Sorting", "Searching", "Hash Tables", "Linked Lists", "Stacks", "Queues", "Heaps", "System Design", "Behavioral", "Database" };
        if (!string.IsNullOrEmpty(dto.Category) && !validCategories.Contains(dto.Category, StringComparer.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Category '{Category}' is not in the standard list. Allowing custom category.", dto.Category);
        }
    }

    public async Task<InterviewQuestion?> UpdateQuestionAsync(Guid questionId, UpdateQuestionDto dto, Guid updatedBy)
    {
        var question = await _context.InterviewQuestions.FindAsync(questionId);
        if (question == null)
        {
            return null;
        }

        // Allow admin to update any question; otherwise only the creator can update
        var updater = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == updatedBy);
        var isAdmin = updater?.Role?.ToLower() == "admin";
        if (!isAdmin && question.CreatedBy != updatedBy)
        {
            return null;
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
        if (dto.VideoUrl != null)
            question.VideoUrl = dto.VideoUrl;
        if (dto.RoleTags != null)
            question.RoleTags = JsonSerializer.Serialize(dto.RoleTags);
        if (dto.RelatedQuestionIds != null)
            question.RelatedQuestionIds = JsonSerializer.Serialize(dto.RelatedQuestionIds);
        if (dto.RelatedCourseIds != null)
            question.RelatedCourseIds = JsonSerializer.Serialize(dto.RelatedCourseIds);
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

    public async Task<List<RelatedQuestionDto>> GetRelatedQuestionsAsync(IEnumerable<Guid> questionIds)
    {
        var ids = questionIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        if (!ids.Any())
        {
            return new List<RelatedQuestionDto>();
        }

        return await _context.InterviewQuestions
            .Where(q => ids.Contains(q.Id) && q.IsActive)
            .Select(q => new RelatedQuestionDto
            {
                Id = q.Id,
                Title = q.Title
            })
            .ToListAsync();
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

    public async Task<InterviewQuestion> ApproveQuestionAsync(Guid questionId, Guid approvedBy)
    {
        var question = await _context.InterviewQuestions.FindAsync(questionId);
        if (question == null)
        {
            throw new ArgumentException($"Question with ID {questionId} not found.");
        }

        question.ApprovalStatus = "Approved";
        question.ApprovedBy = approvedBy;
        question.ApprovedAt = DateTime.UtcNow;
        question.RejectionReason = null;
        question.IsActive = true;
        question.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Award coins to the question creator for getting their question published
        if (question.CreatedBy.HasValue)
        {
            try
            {
                await _coinService.AwardCoinsAsync(
                    question.CreatedBy.Value,
                    AchievementTypes.QuestionPublished,
                    "Your interview question was published",
                    questionId,
                    "InterviewQuestion");
                
                _logger.LogInformation("Awarded coins to user {UserId} for question {QuestionId} approval", 
                    question.CreatedBy.Value, questionId);
            }
            catch (Exception ex)
            {
                // Log but don't fail the approval operation
                _logger.LogError(ex, "Failed to award coins to user {UserId} for question {QuestionId} approval", 
                    question.CreatedBy.Value, questionId);
            }
        }

        return question;
    }

    public async Task<InterviewQuestion> RejectQuestionAsync(Guid questionId, Guid rejectedBy, string? rejectionReason = null)
    {
        var question = await _context.InterviewQuestions.FindAsync(questionId);
        if (question == null)
        {
            throw new ArgumentException($"Question with ID {questionId} not found.");
        }

        question.ApprovalStatus = "Rejected";
        question.ApprovedBy = rejectedBy;
        question.ApprovedAt = DateTime.UtcNow;
        question.RejectionReason = rejectionReason;
        question.IsActive = false;
        question.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return question;
    }

    public async Task<IEnumerable<InterviewQuestion>> GetPendingQuestionsAsync()
    {
        return await _context.InterviewQuestions
            .Where(q => q.ApprovalStatus == "Pending")
            .OrderBy(q => q.CreatedAt)
            .ToListAsync();
    }

    public async Task<QuestionBookmark> AddBookmarkAsync(Guid questionId, Guid userId, string? notes = null)
    {
        // Check if question exists
        var question = await _context.InterviewQuestions.FindAsync(questionId);
        if (question == null)
        {
            throw new ArgumentException($"Question with ID {questionId} not found.");
        }

        // Check if already bookmarked
        var existing = await _context.QuestionBookmarks
            .FirstOrDefaultAsync(b => b.UserId == userId && b.QuestionId == questionId);

        if (existing != null)
        {
            // Update notes if provided
            if (notes != null)
            {
                existing.Notes = notes;
                await _context.SaveChangesAsync();
            }
            return existing;
        }

        // Create new bookmark
        var bookmark = new QuestionBookmark
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            QuestionId = questionId,
            Notes = notes,
            CreatedAt = DateTime.UtcNow
        };

        _context.QuestionBookmarks.Add(bookmark);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} bookmarked question {QuestionId}", userId, questionId);
        return bookmark;
    }

    public async Task<bool> RemoveBookmarkAsync(Guid questionId, Guid userId)
    {
        var bookmark = await _context.QuestionBookmarks
            .FirstOrDefaultAsync(b => b.UserId == userId && b.QuestionId == questionId);

        if (bookmark == null)
        {
            return false;
        }

        _context.QuestionBookmarks.Remove(bookmark);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} removed bookmark for question {QuestionId}", userId, questionId);
        return true;
    }

    public async Task<IEnumerable<InterviewQuestion>> GetBookmarkedQuestionsAsync(Guid userId)
    {
        return await _context.QuestionBookmarks
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => b.Question)
            .ToListAsync();
    }

    public async Task<bool> IsQuestionBookmarkedAsync(Guid questionId, Guid userId)
    {
        return await _context.QuestionBookmarks
            .AnyAsync(b => b.UserId == userId && b.QuestionId == questionId);
    }
}

