using Vector.Api.Models;

namespace Vector.Api.Services;

public interface ICommentService
{
    Task<InterviewQuestionComment> CreateCommentAsync(Guid questionId, Guid userId, string content);
    Task<IEnumerable<InterviewQuestionComment>> GetCommentsForQuestionAsync(Guid questionId);
    Task<InterviewQuestionComment> UpdateCommentAsync(Guid commentId, Guid userId, string content);
    Task<bool> DeleteCommentAsync(Guid commentId, Guid userId);
    Task<bool> UpvoteCommentAsync(Guid commentId, Guid userId);
    Task<bool> RemoveUpvoteAsync(Guid commentId, Guid userId);
}
