using Microsoft.EntityFrameworkCore;
using Vector.Api.Data;
using Vector.Api.DTOs.CodeExecution;
using Vector.Api.DTOs.Solution;
using Vector.Api.Models;

namespace Vector.Api.Services;

public class SolutionService : ISolutionService
{
    private readonly ApplicationDbContext _context;
    private readonly ICodeExecutionService _codeExecutionService;
    private readonly IAnalyticsService _analyticsService;
    private readonly ILogger<SolutionService> _logger;

    public SolutionService(
        ApplicationDbContext context,
        ICodeExecutionService codeExecutionService,
        IAnalyticsService analyticsService,
        ILogger<SolutionService> logger)
    {
        _context = context;
        _codeExecutionService = codeExecutionService;
        _analyticsService = analyticsService;
        _logger = logger;
    }

    public async Task SubmitSolutionAsync(Guid userId, SubmitSolutionDto dto)
    {
        // Verify question exists
        var question = await _context.InterviewQuestions
            .FirstOrDefaultAsync(q => q.Id == dto.QuestionId && q.IsActive);

        if (question == null)
        {
            throw new KeyNotFoundException("Question not found.");
        }

        // Get total test cases count for the question
        var totalTestCases = await _context.QuestionTestCases
            .Where(tc => tc.QuestionId == dto.QuestionId)
            .CountAsync();

        // Create UserSolution (validation already done in frontend, so status is "Accepted")
        var userSolution = new UserSolution
        {
            UserId = userId,
            QuestionId = dto.QuestionId,
            Language = dto.Language,
            Code = dto.Code,
            Status = "Accepted", // Only called when all tests pass
            ExecutionTime = 0, // Not calculated since we don't validate
            MemoryUsed = 0, // Not calculated since we don't validate
            TestCasesPassed = totalTestCases,
            TotalTestCases = totalTestCases,
            SubmittedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.UserSolutions.Add(userSolution);

        // Mark question as solved in optimized lookup table (upsert)
        var existingSolved = await _context.UserSolvedQuestions
            .FirstOrDefaultAsync(usq => usq.UserId == userId && usq.QuestionId == dto.QuestionId);
        
        if (existingSolved == null)
        {
            // Create new solved record
            var userSolvedQuestion = new UserSolvedQuestion
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                QuestionId = dto.QuestionId,
                Language = dto.Language,
                SolvedAt = DateTime.UtcNow
            };
            _context.UserSolvedQuestions.Add(userSolvedQuestion);
        }
        else
        {
            // Update existing record (update solved date and language if needed)
            existingSolved.SolvedAt = DateTime.UtcNow;
            existingSolved.Language = dto.Language;
        }

        await _context.SaveChangesAsync();

        // Update analytics (fire and forget - don't block on analytics update)
        _ = Task.Run(async () =>
        {
            try
            {
                await _analyticsService.UpdateAnalyticsAsync(
                    userId,
                    dto.QuestionId,
                    "Accepted",
                    0, // No execution time since we don't validate
                    0, // No memory since we don't validate
                    dto.Language);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating analytics for solution {SolutionId}", userSolution.Id);
            }
        });
    }

    public async Task<(List<UserSolutionDto> Solutions, int TotalCount)> GetUserSolutionsAsync(
        Guid userId, 
        SolutionFilterDto? filter = null)
    {
        var query = _context.UserSolutions
            .Include(s => s.Question)
            .Include(s => s.TestCaseResults)
            .Where(s => s.UserId == userId);

        if (filter != null)
        {
            if (filter.QuestionId.HasValue)
            {
                query = query.Where(s => s.QuestionId == filter.QuestionId.Value);
            }

            if (!string.IsNullOrEmpty(filter.Language))
            {
                query = query.Where(s => s.Language == filter.Language);
            }

            if (!string.IsNullOrEmpty(filter.Status))
            {
                query = query.Where(s => s.Status == filter.Status);
            }
        }

        var totalCount = await query.CountAsync();

        query = query.OrderByDescending(s => s.SubmittedAt);

        if (filter != null)
        {
            var page = filter.Page > 0 ? filter.Page : 1;
            var pageSize = filter.PageSize > 0 ? filter.PageSize : 20;
            query = query.Skip((page - 1) * pageSize).Take(pageSize);
        }

        var solutions = await query.ToListAsync();

        var solutionDtos = solutions.Select(s => MapToDto(s, s.TestCaseResults.ToList())).ToList();

        return (solutionDtos, totalCount);
    }

    public async Task<UserSolutionDto?> GetSolutionByIdAsync(Guid solutionId, Guid userId)
    {
        var solution = await _context.UserSolutions
            .Include(s => s.Question)
            .Include(s => s.TestCaseResults)
            .ThenInclude(ss => ss.TestCase)
            .FirstOrDefaultAsync(s => s.Id == solutionId && s.UserId == userId);

        if (solution == null)
        {
            return null;
        }

        return MapToDto(solution, solution.TestCaseResults.ToList());
    }

    public async Task<List<UserSolutionDto>> GetSolutionsForQuestionAsync(Guid questionId, Guid userId)
    {
        var solutions = await _context.UserSolutions
            .Include(s => s.Question)
            .Include(s => s.TestCaseResults)
            .Where(s => s.QuestionId == questionId && s.UserId == userId)
            .OrderByDescending(s => s.SubmittedAt)
            .ToListAsync();

        return solutions.Select(s => MapToDto(s, s.TestCaseResults.ToList())).ToList();
    }

    public async Task<SolutionStatisticsDto> GetSolutionStatisticsAsync(Guid userId)
    {
        var solutions = await _context.UserSolutions
            .Where(s => s.UserId == userId)
            .ToListAsync();

        var statistics = new SolutionStatisticsDto
        {
            TotalSubmissions = solutions.Count,
            AcceptedSolutions = solutions.Count(s => s.Status == "Accepted"),
            QuestionsSolved = solutions.Where(s => s.Status == "Accepted")
                .Select(s => s.QuestionId)
                .Distinct()
                .Count(),
            AverageExecutionTime = solutions.Any() 
                ? (decimal)solutions.Average(s => (double)s.ExecutionTime) 
                : 0,
            AverageMemoryUsed = solutions.Any() 
                ? (long)solutions.Average(s => (double)s.MemoryUsed) 
                : 0
        };

        // Solutions by language
        statistics.SolutionsByLanguage = solutions
            .GroupBy(s => s.Language)
            .ToDictionary(g => g.Key, g => g.Count());

        // Solutions by status
        statistics.SolutionsByStatus = solutions
            .GroupBy(s => s.Status)
            .ToDictionary(g => g.Key, g => g.Count());

        return statistics;
    }

    private UserSolutionDto MapToDto(UserSolution solution, List<SolutionSubmission> submissions)
    {
        return new UserSolutionDto
        {
            Id = solution.Id,
            UserId = solution.UserId,
            QuestionId = solution.QuestionId,
            QuestionTitle = solution.Question?.Title ?? string.Empty,
            Language = solution.Language,
            Code = solution.Code,
            Status = solution.Status,
            ExecutionTime = solution.ExecutionTime,
            MemoryUsed = solution.MemoryUsed,
            TestCasesPassed = solution.TestCasesPassed,
            TotalTestCases = solution.TotalTestCases,
            SubmittedAt = solution.SubmittedAt,
            TestCaseResults = submissions.Select(ss => new SolutionSubmissionDto
            {
                Id = ss.Id,
                TestCaseId = ss.TestCaseId,
                TestCaseNumber = ss.TestCaseNumber,
                Status = ss.Status,
                Output = ss.Output,
                ExpectedOutput = ss.ExpectedOutput,
                ErrorMessage = ss.ErrorMessage,
                ExecutionTime = ss.ExecutionTime,
                MemoryUsed = ss.MemoryUsed
            }).ToList()
        };
    }

    public async Task<bool> HasUserSolvedQuestionAsync(Guid userId, Guid questionId)
    {
        return await _context.UserSolvedQuestions
            .AnyAsync(usq => usq.UserId == userId && usq.QuestionId == questionId);
    }
}

