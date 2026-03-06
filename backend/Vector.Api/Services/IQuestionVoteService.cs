using Vector.Api.Models;

namespace Vector.Api.Services;

public interface IQuestionVoteService
{
    Task<int> VoteQuestionAsync(Guid questionId, Guid userId, int voteType);
    Task<bool> RemoveVoteAsync(Guid questionId, Guid userId);
    Task<int> GetQuestionVoteCountAsync(Guid questionId);
    Task<int?> GetUserVoteAsync(Guid questionId, Guid userId);
}
