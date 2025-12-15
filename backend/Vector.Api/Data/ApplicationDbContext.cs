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
            entity.HasIndex(e => e.Category); // For filtering
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
    }
}

