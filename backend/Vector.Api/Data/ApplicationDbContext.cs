using Microsoft.EntityFrameworkCore;
using Vector.Api.Models;

namespace Vector.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Subscription> Subscriptions { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<EmailVerification> EmailVerifications { get; set; }
    public DbSet<PasswordReset> PasswordResets { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<MockInterview> MockInterviews { get; set; }
    public DbSet<CoachApplication> CoachApplications { get; set; }
    public DbSet<InterviewQuestion> InterviewQuestions { get; set; }
    public DbSet<QuestionTestCase> QuestionTestCases { get; set; }
    public DbSet<QuestionSolution> QuestionSolutions { get; set; }
    public DbSet<UserSolution> UserSolutions { get; set; }
    public DbSet<SolutionSubmission> SolutionSubmissions { get; set; }
    public DbSet<UserCodeDraft> UserCodeDrafts { get; set; }
    public DbSet<LearningAnalytics> LearningAnalytics { get; set; }
    public DbSet<UserSolvedQuestion> UserSolvedQuestions { get; set; }
    public DbSet<PeerInterviewSession> PeerInterviewSessions { get; set; }
    public DbSet<PeerInterviewMatch> PeerInterviewMatches { get; set; }
    public DbSet<InterviewMatchingRequest> InterviewMatchingRequests { get; set; }
    public DbSet<UserSessionParticipant> UserSessionParticipants { get; set; }
    public DbSet<VideoSession> VideoSessions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.Role).HasMaxLength(20).HasDefaultValue("student");
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.Location).HasMaxLength(200);
        });

        // Configure Subscription entity
        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.StripeSubscriptionId).IsUnique();
        });

        // Configure Payment entity
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Subscription)
                .WithMany()
                .HasForeignKey(e => e.SubscriptionId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(e => e.StripePaymentIntentId).IsUnique();
        });

        // Configure EmailVerification entity
        modelBuilder.Entity<EmailVerification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.Token).IsUnique();
        });

        // Configure PasswordReset entity
        modelBuilder.Entity<PasswordReset>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.Token).IsUnique();
        });

        // Configure RefreshToken entity
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.Token).IsUnique();
        });

        // Configure MockInterview entity
        modelBuilder.Entity<MockInterview>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
            entity.Property(e => e.VideoUrl).IsRequired();
            entity.Property(e => e.Category).HasMaxLength(50);
            entity.Property(e => e.Difficulty).HasMaxLength(20).HasDefaultValue("Medium");
        });

        // Configure CoachApplication entity
        modelBuilder.Entity<CoachApplication>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Reviewer)
                .WithMany()
                .HasForeignKey(e => e.ReviewedBy)
                .OnDelete(DeleteBehavior.SetNull);
            entity.Property(e => e.Motivation).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Experience).HasMaxLength(1000);
            entity.Property(e => e.Specialization).HasMaxLength(500);
            entity.Property(e => e.ImageUrls).HasMaxLength(2000); // Comma-separated URLs
            entity.Property(e => e.Status).HasMaxLength(20).HasDefaultValue("pending");
            entity.Property(e => e.AdminNotes).HasMaxLength(500);
            entity.HasIndex(e => e.UserId).IsUnique(); // One application per user
        });

        // Configure InterviewQuestion entity
        modelBuilder.Entity<InterviewQuestion>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).IsRequired();
            entity.Property(e => e.Difficulty).IsRequired().HasMaxLength(20);
            entity.Property(e => e.QuestionType).IsRequired().HasMaxLength(50).HasDefaultValue("Coding");
            entity.Property(e => e.Category).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.QuestionType); // For filtering by question type
            entity.Property(e => e.TimeComplexityHint).HasMaxLength(50);
            entity.Property(e => e.SpaceComplexityHint).HasMaxLength(50);
            entity.HasOne(e => e.Creator)
                .WithMany()
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.Approver)
                .WithMany()
                .HasForeignKey(e => e.ApprovedBy)
                .OnDelete(DeleteBehavior.SetNull);
            entity.Property(e => e.ApprovalStatus).IsRequired().HasMaxLength(20).HasDefaultValue("Pending");
            entity.HasIndex(e => e.Category); // For filtering
            entity.HasIndex(e => e.ApprovalStatus); // For filtering by approval status
            entity.HasIndex(e => e.Difficulty); // For filtering
            entity.HasIndex(e => e.IsActive); // For filtering active questions
        });

        // Configure QuestionTestCase entity
        modelBuilder.Entity<QuestionTestCase>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Question)
                .WithMany(q => q.TestCases)
                .HasForeignKey(e => e.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.Input).IsRequired();
            entity.Property(e => e.ExpectedOutput).IsRequired();
            entity.HasIndex(e => e.QuestionId); // For querying test cases by question
        });

        // Configure QuestionSolution entity
        modelBuilder.Entity<QuestionSolution>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Question)
                .WithMany(q => q.Solutions)
                .HasForeignKey(e => e.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.Language).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Code).IsRequired();
            entity.Property(e => e.TimeComplexity).HasMaxLength(50);
            entity.Property(e => e.SpaceComplexity).HasMaxLength(50);
            entity.HasOne(e => e.Creator)
                .WithMany()
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(e => e.QuestionId); // For querying solutions by question
            entity.HasIndex(e => e.Language); // For filtering by language
        });

        // Configure UserSolution entity
        modelBuilder.Entity<UserSolution>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Question)
                .WithMany()
                .HasForeignKey(e => e.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.Language).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Code).IsRequired();
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.UserId); // For querying user's solutions
            entity.HasIndex(e => e.QuestionId); // For querying solutions by question
            entity.HasIndex(e => new { e.UserId, e.QuestionId }); // Composite index for user-question queries
            entity.HasIndex(e => e.Status); // For filtering by status
            entity.HasIndex(e => e.SubmittedAt); // For sorting by submission date
        });

        // Configure SolutionSubmission entity
        modelBuilder.Entity<SolutionSubmission>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.UserSolution)
                .WithMany(s => s.TestCaseResults)
                .HasForeignKey(e => e.UserSolutionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.TestCase)
                .WithMany()
                .HasForeignKey(e => e.TestCaseId)
                .OnDelete(DeleteBehavior.Restrict); // Don't delete test case if submission exists
            entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
            entity.HasIndex(e => e.UserSolutionId); // For querying test case results
            entity.HasIndex(e => e.TestCaseId); // For querying by test case
        });

        // Configure LearningAnalytics entity
        modelBuilder.Entity<LearningAnalytics>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.UserId).IsUnique(); // One analytics record per user
            entity.HasIndex(e => e.LastActivityDate); // For streak calculations
        });

        // Configure UserSolvedQuestion entity
        modelBuilder.Entity<UserSolvedQuestion>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Question)
                .WithMany()
                .HasForeignKey(e => e.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.Language).HasMaxLength(50);
            // Unique constraint: one record per user-question pair
            entity.HasIndex(e => new { e.UserId, e.QuestionId }).IsUnique();
            entity.HasIndex(e => e.UserId); // For querying user's solved questions
            entity.HasIndex(e => e.QuestionId); // For querying question statistics
            entity.HasIndex(e => e.SolvedAt); // For sorting by solve date
        });

        // Configure PeerInterviewSession entity
        modelBuilder.Entity<PeerInterviewSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Interviewer)
                .WithMany()
                .HasForeignKey(e => e.InterviewerId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Interviewee)
                .WithMany()
                .HasForeignKey(e => e.IntervieweeId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Question)
                .WithMany()
                .HasForeignKey(e => e.QuestionId)
                .OnDelete(DeleteBehavior.SetNull);
            
            entity.HasIndex(e => e.InterviewerId);
            entity.HasIndex(e => e.IntervieweeId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.ScheduledTime);
        });

        // Configure PeerInterviewMatch entity
        modelBuilder.Entity<PeerInterviewMatch>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.HasIndex(e => e.IsAvailable);
        });

        // Configure InterviewMatchingRequest entity
        modelBuilder.Entity<InterviewMatchingRequest>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.ScheduledSession)
                .WithMany()
                .HasForeignKey(e => e.ScheduledSessionId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.MatchedUser)
                .WithMany()
                .HasForeignKey(e => e.MatchedUserId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);
            entity.HasOne(e => e.MatchedRequest)
                .WithMany()
                .HasForeignKey(e => e.MatchedRequestId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50).HasDefaultValue("Pending");
            entity.HasIndex(e => e.ScheduledSessionId);
            entity.HasIndex(e => e.Status);
        });

        // Configure UserSessionParticipant entity
        modelBuilder.Entity<UserSessionParticipant>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Session)
                .WithMany()
                .HasForeignKey(e => e.SessionId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.Role).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50).HasDefaultValue("Active");
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.SessionId);
            entity.HasIndex(e => new { e.UserId, e.SessionId }).IsUnique();
        });

        // Configure VideoSession entity
        modelBuilder.Entity<VideoSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Session)
                .WithMany()
                .HasForeignKey(e => e.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.Token).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50).HasDefaultValue("Active");
            entity.Property(e => e.SignalingData).HasMaxLength(1000);
            entity.HasIndex(e => e.SessionId);
            entity.HasIndex(e => e.Token).IsUnique();
        });
    }
}

