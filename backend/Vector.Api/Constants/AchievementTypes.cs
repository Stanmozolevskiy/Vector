namespace Vector.Api.Constants;

/// <summary>
/// Constants for achievement activity types
/// </summary>
public static class AchievementTypes
{
    // Interview Activities (10-15 coins)
    public const string InterviewCompleted = "InterviewCompleted";
    public const string GreatMockInterviewPartner = "GreatMockInterviewPartner";
    
    // Question Activities (5-25 coins)
    public const string QuestionPublished = "QuestionPublished";
    public const string QuestionUpvoted = "QuestionUpvoted";
    public const string QuestionInAnotherInterview = "QuestionInAnotherInterview";
    
    // Engagement (1-10 coins)
    public const string LessonCompleted = "LessonCompleted";
    public const string CommentUpvoted = "CommentUpvoted";
    public const string ProfileCompleted = "ProfileCompleted";
    
    // Referral (100 coins)
    public const string ReferralSuccess = "ReferralSuccess";
    
    // Feedback (10 coins)
    public const string FeedbackSubmitted = "FeedbackSubmitted";
}
