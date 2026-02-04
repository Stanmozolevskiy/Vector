using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using System.Text.Json;
using Vector.Api.Attributes;
using Vector.Api.DTOs.Question;
using Vector.Api.Services;

namespace Vector.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class QuestionController : ControllerBase
{
    private readonly IQuestionService _questionService;
    private readonly IS3Service _s3Service;
    private readonly ILogger<QuestionController> _logger;

    public QuestionController(IQuestionService questionService, IS3Service s3Service, ILogger<QuestionController> logger)
    {
        _questionService = questionService;
        _s3Service = s3Service;
        _logger = logger;
    }

    /// <summary>
    /// Get all questions with optional filters
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetQuestions([FromQuery] QuestionFilterDto? filter)
    {
        try
        {
            // Manually parse array parameters from query string if not bound correctly
            // ASP.NET Core doesn't bind difficulties[] automatically, so we need to parse it manually
            if (filter == null)
            {
                filter = new QuestionFilterDto();
            }

            // Handle difficulties[] parameter (URL encoded as difficulties%5B%5D)
            // Check all possible query key formats
            var difficultyKeys = Request.Query.Keys.Where(k => 
                k.Equals("difficulties[]", StringComparison.OrdinalIgnoreCase) ||
                k.Equals("difficulties", StringComparison.OrdinalIgnoreCase) ||
                k.StartsWith("difficulties[", StringComparison.OrdinalIgnoreCase));
            
            if (difficultyKeys.Any())
            {
                var difficultyValues = new List<string>();
                foreach (var key in difficultyKeys)
                {
                    difficultyValues.AddRange(Request.Query[key]);
                }
                if (difficultyValues.Any())
                {
                    filter.Difficulties = difficultyValues.Distinct().ToList();
                }
            }
            // If Difficulties is still null/empty but we have difficulty filter, initialize it
            if ((filter.Difficulties == null || !filter.Difficulties.Any()) && !string.IsNullOrEmpty(filter.Difficulty))
            {
                filter.Difficulties = new List<string> { filter.Difficulty };
            }

            // Handle categories[] parameter
            var categoryKeys = Request.Query.Keys.Where(k => 
                k.Equals("categories[]", StringComparison.OrdinalIgnoreCase) ||
                k.Equals("categories", StringComparison.OrdinalIgnoreCase) ||
                k.StartsWith("categories[", StringComparison.OrdinalIgnoreCase));
            
            if (categoryKeys.Any())
            {
                var categoryValues = new List<string>();
                foreach (var key in categoryKeys)
                {
                    categoryValues.AddRange(Request.Query[key]);
                }
                if (categoryValues.Any())
                {
                    filter.Categories = categoryValues.Distinct().ToList();
                }
            }
            // If Categories is still null/empty but we have category filter, initialize it
            if ((filter.Categories == null || !filter.Categories.Any()) && !string.IsNullOrEmpty(filter.Category))
            {
                filter.Categories = new List<string> { filter.Category };
            }

            // Handle companies[] parameter
            var companyKeys = Request.Query.Keys.Where(k => 
                k.Equals("companies[]", StringComparison.OrdinalIgnoreCase) ||
                k.Equals("companies", StringComparison.OrdinalIgnoreCase) ||
                k.StartsWith("companies[", StringComparison.OrdinalIgnoreCase));
            
            if (companyKeys.Any())
            {
                var companyValues = new List<string>();
                foreach (var key in companyKeys)
                {
                    companyValues.AddRange(Request.Query[key]);
                }
                if (companyValues.Any())
                {
                    filter.Companies = companyValues.Distinct().ToList();
                }
            }

            var questions = await _questionService.GetQuestionsAsync(filter);
            var questionDtos = questions.Select(q => MapToQuestionListDto(q));
            return Ok(questionDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting questions");
            return StatusCode(500, new { error = "An error occurred while retrieving questions." });
        }
    }

    /// <summary>
    /// Get question by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetQuestion(Guid id)
    {
        try
        {
            var question = await _questionService.GetQuestionByIdAsync(id);
            if (question == null)
            {
                return NotFound(new { error = "Question not found." });
            }

            var dto = MapToQuestionDto(question);

            // Attach related questions (id + title) if configured
            dto.RelatedQuestions = await GetRelatedQuestionsForQuestionAsync(question);
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting question {QuestionId}", id);
            return StatusCode(500, new { error = "An error occurred while retrieving the question." });
        }
    }

    /// <summary>
    /// Get comments for a question (community answers / discussion)
    /// </summary>
    [HttpGet("{id}/comments")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetQuestionComments(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string sort = "hot")
    {
        try
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 25;
            if (pageSize > 100) pageSize = 100;

            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new { error = "Invalid token" });
            }

            var exists = await _questionService.GetQuestionByIdAsync(id);
            if (exists == null)
            {
                return NotFound(new { error = "Question not found." });
            }

            // Fallback to DB context via service scope (keeps controller simple and avoids a new service just for comments)
            // NOTE: We use EF Core directly here because comment retrieval is presentation-specific (includes user name).
            var context = HttpContext.RequestServices.GetRequiredService<Vector.Api.Data.ApplicationDbContext>();

            var baseQuery = context.InterviewQuestionComments
                .AsNoTracking()
                .Where(c => c.QuestionId == id && c.ParentCommentId == null);

            var normalizedSort = (sort ?? "hot").Trim().ToLowerInvariant();
            if (normalizedSort is not ("hot" or "top" or "new"))
            {
                normalizedSort = "hot";
            }

            var projected = baseQuery
                .Select(c => new
                {
                    Comment = c,
                    User = c.User,
                    UpvoteCount = c.Votes.Count(),
                    HasUpvoted = c.Votes.Any(v => v.UserId == userId.Value)
                });

            projected = normalizedSort switch
            {
                "new" => projected.OrderByDescending(x => x.Comment.CreatedAt),
                "top" => projected.OrderByDescending(x => x.UpvoteCount).ThenByDescending(x => x.Comment.CreatedAt),
                _ => projected.OrderByDescending(x => x.UpvoteCount).ThenByDescending(x => x.Comment.CreatedAt),
            };

            var parents = await projected
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new QuestionCommentDto
                {
                    Id = x.Comment.Id,
                    QuestionId = x.Comment.QuestionId,
                    UserId = x.Comment.UserId,
                    UserName = ((x.User.FirstName ?? "") + " " + (x.User.LastName ?? "")).Trim(),
                    UserProfilePictureUrl = x.User.ProfilePictureUrl,
                    Content = x.Comment.Content,
                    CreatedAt = x.Comment.CreatedAt,
                    ParentCommentId = x.Comment.ParentCommentId,
                    UpvoteCount = x.UpvoteCount,
                    HasUpvoted = x.HasUpvoted,
                    Replies = new List<QuestionCommentDto>()
                })
                .ToListAsync();

            var parentIds = parents.Select(p => p.Id).ToList();
            if (!parentIds.Any())
            {
                return Ok(parents);
            }

            var replies = await context.InterviewQuestionComments
                .AsNoTracking()
                .Where(c => c.QuestionId == id && c.ParentCommentId != null && parentIds.Contains(c.ParentCommentId.Value))
                .OrderBy(c => c.CreatedAt)
                .Select(c => new QuestionCommentDto
                {
                    Id = c.Id,
                    QuestionId = c.QuestionId,
                    UserId = c.UserId,
                    UserName = ((c.User.FirstName ?? "") + " " + (c.User.LastName ?? "")).Trim(),
                    UserProfilePictureUrl = c.User.ProfilePictureUrl,
                    Content = c.Content,
                    CreatedAt = c.CreatedAt,
                    ParentCommentId = c.ParentCommentId,
                    UpvoteCount = c.Votes.Count(),
                    HasUpvoted = c.Votes.Any(v => v.UserId == userId.Value),
                    Replies = new List<QuestionCommentDto>()
                })
                .ToListAsync();

            var repliesByParent = replies
                .Where(r => r.ParentCommentId.HasValue)
                .GroupBy(r => r.ParentCommentId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var parent in parents)
            {
                if (repliesByParent.TryGetValue(parent.Id, out var childReplies))
                {
                    parent.Replies = childReplies;
                }
            }

            return Ok(parents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting comments for question {QuestionId}", id);
            return StatusCode(500, new { error = "An error occurred while retrieving comments." });
        }
    }

    /// <summary>
    /// Add a comment to a question
    /// </summary>
    [HttpPost("{id}/comments")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddQuestionComment(Guid id, [FromBody] CreateQuestionCommentDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new { error = "Invalid token" });
            }

            if (string.IsNullOrWhiteSpace(dto.Content))
            {
                return BadRequest(new { error = "Comment content is required." });
            }

            var question = await _questionService.GetQuestionByIdAsync(id);
            if (question == null)
            {
                return NotFound(new { error = "Question not found." });
            }

            var context = HttpContext.RequestServices.GetRequiredService<Vector.Api.Data.ApplicationDbContext>();
            var user = await context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId.Value);
            if (user == null)
            {
                return Unauthorized(new { error = "User not found." });
            }

            if (dto.ParentCommentId.HasValue)
            {
                var parentExists = await context.InterviewQuestionComments
                    .AsNoTracking()
                    .AnyAsync(c => c.Id == dto.ParentCommentId.Value && c.QuestionId == id);
                if (!parentExists)
                {
                    return BadRequest(new { error = "Parent comment not found." });
                }
            }

            var comment = new Vector.Api.Models.InterviewQuestionComment
            {
                Id = Guid.NewGuid(),
                QuestionId = id,
                UserId = userId.Value,
                Content = dto.Content.Trim(),
                ParentCommentId = dto.ParentCommentId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.InterviewQuestionComments.Add(comment);
            await context.SaveChangesAsync();

            var result = new QuestionCommentDto
            {
                Id = comment.Id,
                QuestionId = comment.QuestionId,
                UserId = comment.UserId,
                UserName = ((user.FirstName ?? "") + " " + (user.LastName ?? "")).Trim(),
                UserProfilePictureUrl = user.ProfilePictureUrl,
                Content = comment.Content,
                CreatedAt = comment.CreatedAt,
                ParentCommentId = comment.ParentCommentId,
                UpvoteCount = 0,
                HasUpvoted = false,
                Replies = new List<QuestionCommentDto>()
            };

            return CreatedAtAction(nameof(GetQuestionComments), new { id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding comment to question {QuestionId}", id);
            return StatusCode(500, new { error = "An error occurred while adding the comment." });
        }
    }

    /// <summary>
    /// Toggle upvote for a comment
    /// </summary>
    [HttpPost("{id}/comments/{commentId}/upvote")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleCommentUpvote(Guid id, Guid commentId)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new { error = "Invalid token" });
            }

            var context = HttpContext.RequestServices.GetRequiredService<Vector.Api.Data.ApplicationDbContext>();
            var commentExists = await context.InterviewQuestionComments
                .AsNoTracking()
                .AnyAsync(c => c.Id == commentId && c.QuestionId == id);
            if (!commentExists)
            {
                return NotFound(new { error = "Comment not found." });
            }

            var existing = await context.InterviewQuestionCommentVotes
                .FirstOrDefaultAsync(v => v.CommentId == commentId && v.UserId == userId.Value);

            if (existing == null)
            {
                context.InterviewQuestionCommentVotes.Add(new Vector.Api.Models.InterviewQuestionCommentVote
                {
                    CommentId = commentId,
                    UserId = userId.Value,
                    CreatedAt = DateTime.UtcNow
                });
            }
            else
            {
                context.InterviewQuestionCommentVotes.Remove(existing);
            }

            await context.SaveChangesAsync();

            var upvoteCount = await context.InterviewQuestionCommentVotes.CountAsync(v => v.CommentId == commentId);
            var hasUpvoted = await context.InterviewQuestionCommentVotes.AnyAsync(v => v.CommentId == commentId && v.UserId == userId.Value);

            return Ok(new { commentId, upvoteCount, hasUpvoted });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling upvote for comment {CommentId}", commentId);
            return StatusCode(500, new { error = "An error occurred while updating the upvote." });
        }
    }

    /// <summary>
    /// Create a new question (admin or coach)
    /// Admin-created questions are automatically approved
    /// Coach-created questions require admin approval
    /// </summary>
    [HttpPost]
    [AuthorizeRole("admin", "coach")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateQuestion([FromBody] CreateQuestionDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new { error = "Invalid token" });
            }

            var question = await _questionService.CreateQuestionAsync(dto, userId.Value);
            var questionDto = MapToQuestionDto(question);

            return CreatedAtAction(nameof(GetQuestion), new { id = question.Id }, questionDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating question");
            return StatusCode(500, new { error = "An error occurred while creating the question." });
        }
    }

    /// <summary>
    /// Submit a new question for review (any authenticated user)
    /// Non-admin submissions are created as Pending and will not be visible until approved.
    /// </summary>
    [HttpPost("submit")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitQuestion([FromBody] CreateQuestionDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new { error = "Invalid token" });
            }

            var question = await _questionService.CreateQuestionAsync(dto, userId.Value);
            var questionDto = MapToQuestionDto(question);

            return CreatedAtAction(nameof(GetQuestion), new { id = question.Id }, questionDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting question");
            return StatusCode(500, new { error = "An error occurred while submitting the question." });
        }
    }

    /// <summary>
    /// Upload a question video (mp4/webm). Returns a public URL.
    /// </summary>
    [HttpPost("upload-video")]
    [DisableRequestSizeLimit]
    [RequestFormLimits(MultipartBodyLengthLimit = 250 * 1024 * 1024)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadQuestionVideo(IFormFile file)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new { error = "Invalid token" });
            }

            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "No file uploaded" });
            }

            var allowedTypes = new[] { "video/mp4", "video/webm", "video/quicktime" };
            if (!allowedTypes.Contains(file.ContentType.ToLowerInvariant()))
            {
                return BadRequest(new { error = "Invalid file type. Only MP4, WebM, and MOV are allowed" });
            }

            // 250MB max
            if (file.Length > 250L * 1024 * 1024)
            {
                return BadRequest(new { error = "File size exceeds 250MB limit" });
            }

            using var stream = file.OpenReadStream();
            var url = await _s3Service.UploadFileAsync(stream, file.FileName, file.ContentType, "question-videos");

            return Ok(new { videoUrl = url });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload question video");
            return StatusCode(500, new { error = "Failed to upload video" });
        }
    }

    /// <summary>
    /// Update a question (admin only)
    /// </summary>
    [HttpPut("{id}")]
    [AuthorizeRole("admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateQuestion(Guid id, [FromBody] UpdateQuestionDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new { error = "Invalid token" });
            }

            var question = await _questionService.UpdateQuestionAsync(id, dto, userId.Value);
            if (question == null)
            {
                return NotFound(new { error = "Question not found or you don't have permission to update it." });
            }

            var questionDto = MapToQuestionDto(question);

            return Ok(questionDto);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = "Question not found." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating question {QuestionId}", id);
            return StatusCode(500, new { error = "An error occurred while updating the question." });
        }
    }

    /// <summary>
    /// Delete a question (admin only)
    /// </summary>
    [HttpDelete("{id}")]
    [AuthorizeRole("admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteQuestion(Guid id)
    {
        try
        {
            var deleted = await _questionService.DeleteQuestionAsync(id);
            if (!deleted)
            {
                return NotFound(new { error = "Question not found." });
            }

            return Ok(new { message = "Question deleted successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting question {QuestionId}", id);
            return StatusCode(500, new { error = "An error occurred while deleting the question." });
        }
    }

    /// <summary>
    /// Get test cases for a question
    /// </summary>
    [HttpGet("{id}/test-cases")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTestCases(Guid id, [FromQuery] bool includeHidden = false)
    {
        try
        {
            var testCases = await _questionService.GetTestCasesAsync(id, includeHidden);
            var dtos = testCases.Select(t => new QuestionTestCaseDto
            {
                Id = t.Id,
                TestCaseNumber = t.TestCaseNumber,
                Input = t.Input,
                ExpectedOutput = t.ExpectedOutput,
                IsHidden = t.IsHidden,
                Explanation = t.Explanation
            });

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting test cases for question {QuestionId}", id);
            return StatusCode(500, new { error = "An error occurred while retrieving test cases." });
        }
    }

    /// <summary>
    /// Add a test case to a question (admin or coach)
    /// </summary>
    [HttpPost("{id}/test-cases")]
    [AuthorizeRole("admin", "coach")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> AddTestCase(Guid id, [FromBody] CreateTestCaseDto dto)
    {
        try
        {
            var testCase = await _questionService.AddTestCaseAsync(id, dto);
            var testCaseDto = new QuestionTestCaseDto
            {
                Id = testCase.Id,
                TestCaseNumber = testCase.TestCaseNumber,
                Input = testCase.Input,
                ExpectedOutput = testCase.ExpectedOutput,
                IsHidden = testCase.IsHidden,
                Explanation = testCase.Explanation
            };

            return CreatedAtAction(nameof(GetTestCases), new { id = id }, testCaseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding test case to question {QuestionId}", id);
            return StatusCode(500, new { error = "An error occurred while adding the test case." });
        }
    }

    /// <summary>
    /// Get solutions for a question
    /// </summary>
    [HttpGet("{id}/solutions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSolutions(Guid id, [FromQuery] string? language = null)
    {
        try
        {
            var solutions = await _questionService.GetSolutionsAsync(id, language);
            var dtos = solutions.Select(s => new QuestionSolutionDto
            {
                Id = s.Id,
                Language = s.Language,
                Code = s.Code,
                Explanation = s.Explanation,
                TimeComplexity = s.TimeComplexity,
                SpaceComplexity = s.SpaceComplexity,
                IsOfficial = s.IsOfficial,
                CreatedAt = s.CreatedAt
            });

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting solutions for question {QuestionId}", id);
            return StatusCode(500, new { error = "An error occurred while retrieving solutions." });
        }
    }

    /// <summary>
    /// Add a solution to a question (admin only)
    /// </summary>
    [HttpPost("{id}/solutions")]
    [AuthorizeRole("admin")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> AddSolution(Guid id, [FromBody] CreateSolutionDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new { error = "Invalid token" });
            }

            var solution = await _questionService.AddSolutionAsync(id, dto, userId.Value);
            var solutionDto = new QuestionSolutionDto
            {
                Id = solution.Id,
                Language = solution.Language,
                Code = solution.Code,
                Explanation = solution.Explanation,
                TimeComplexity = solution.TimeComplexity,
                SpaceComplexity = solution.SpaceComplexity,
                IsOfficial = solution.IsOfficial,
                CreatedAt = solution.CreatedAt
            };

            return CreatedAtAction(nameof(GetSolutions), new { id = id }, solutionDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding solution to question {QuestionId}", id);
            return StatusCode(500, new { error = "An error occurred while adding the solution." });
        }
    }

    private Guid? GetCurrentUserId()
    {
        // JWT tokens may use different claim names
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value
            ?? User.FindFirst("userId")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return null;
        }

        return userId;
    }

    private QuestionListDto MapToQuestionListDto(Models.InterviewQuestion question)
    {
        return new QuestionListDto
        {
            Id = question.Id,
            Title = question.Title,
            Difficulty = question.Difficulty,
            QuestionType = question.QuestionType,
            Category = question.Category,
            Tags = question.Tags != null ? JsonSerializer.Deserialize<List<string>>(question.Tags) : null,
            CompanyTags = question.CompanyTags != null ? JsonSerializer.Deserialize<List<string>>(question.CompanyTags) : null,
            AcceptanceRate = question.AcceptanceRate,
            IsActive = question.IsActive,
            ApprovalStatus = question.ApprovalStatus
        };
    }

    private InterviewQuestionDto MapToQuestionDto(Models.InterviewQuestion question)
    {
        return new InterviewQuestionDto
        {
            Id = question.Id,
            Title = question.Title,
            Description = question.Description,
            Difficulty = question.Difficulty,
            QuestionType = question.QuestionType,
            Category = question.Category,
            CompanyTags = question.CompanyTags != null ? JsonSerializer.Deserialize<List<string>>(question.CompanyTags) : null,
            Tags = question.Tags != null ? JsonSerializer.Deserialize<List<string>>(question.Tags) : null,
            Constraints = question.Constraints,
            Examples = question.Examples != null ? JsonSerializer.Deserialize<List<ExampleDto>>(question.Examples) : null,
            Hints = question.Hints != null ? JsonSerializer.Deserialize<List<string>>(question.Hints) : null,
            VideoUrl = question.VideoUrl,
            RoleTags = question.RoleTags != null ? JsonSerializer.Deserialize<List<string>>(question.RoleTags) : null,
            RelatedQuestionIds = question.RelatedQuestionIds != null ? JsonSerializer.Deserialize<List<Guid>>(question.RelatedQuestionIds) : null,
            RelatedCourseIds = question.RelatedCourseIds != null ? JsonSerializer.Deserialize<List<Guid>>(question.RelatedCourseIds) : null,
            TimeComplexityHint = question.TimeComplexityHint,
            SpaceComplexityHint = question.SpaceComplexityHint,
            AcceptanceRate = question.AcceptanceRate,
            IsActive = question.IsActive,
            ApprovalStatus = question.ApprovalStatus,
            ApprovedBy = question.ApprovedBy,
            ApprovedAt = question.ApprovedAt,
            RejectionReason = question.RejectionReason,
            CreatedBy = question.CreatedBy,
            CreatedAt = question.CreatedAt,
            UpdatedAt = question.UpdatedAt
        };
    }

    private async Task<List<RelatedQuestionDto>> GetRelatedQuestionsForQuestionAsync(Models.InterviewQuestion question)
    {
        if (string.IsNullOrWhiteSpace(question.RelatedQuestionIds))
        {
            return new List<RelatedQuestionDto>();
        }

        try
        {
            var ids = JsonSerializer.Deserialize<List<Guid>>(question.RelatedQuestionIds) ?? new List<Guid>();
            if (!ids.Any())
            {
                return new List<RelatedQuestionDto>();
            }

            return await _questionService.GetRelatedQuestionsAsync(ids);
        }
        catch
        {
            return new List<RelatedQuestionDto>();
        }
    }

    /// <summary>
    /// Get pending questions (admin only)
    /// </summary>
    [HttpGet("pending")]
    [AuthorizeRole("admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingQuestions()
    {
        try
        {
            var questions = await _questionService.GetPendingQuestionsAsync();
            var dtos = questions.Select(q => MapToQuestionListDto(q));
            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending questions");
            return StatusCode(500, new { error = "An error occurred while retrieving pending questions." });
        }
    }

    /// <summary>
    /// Approve a question (admin only)
    /// </summary>
    [HttpPost("{id}/approve")]
    [AuthorizeRole("admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApproveQuestion(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new { error = "Invalid token" });
            }

            var question = await _questionService.ApproveQuestionAsync(id, userId.Value);
            var questionDto = MapToQuestionDto(question);

            return Ok(questionDto);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving question {QuestionId}", id);
            return StatusCode(500, new { error = "An error occurred while approving the question." });
        }
    }

    /// <summary>
    /// Reject a question (admin only)
    /// </summary>
    [HttpPost("{id}/reject")]
    [AuthorizeRole("admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejectQuestion(Guid id, [FromBody] RejectQuestionDto? dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new { error = "Invalid token" });
            }

            var question = await _questionService.RejectQuestionAsync(id, userId.Value, dto?.RejectionReason);
            var questionDto = MapToQuestionDto(question);

            return Ok(questionDto);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting question {QuestionId}", id);
            return StatusCode(500, new { error = "An error occurred while rejecting the question." });
        }
    }
}

