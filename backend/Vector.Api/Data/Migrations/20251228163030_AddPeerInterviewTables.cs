using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vector.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPeerInterviewTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScheduledInterviewSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    InterviewType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PracticeType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    InterviewLevel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ScheduledStartAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Scheduled"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledInterviewSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduledInterviewSessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LiveInterviewSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScheduledSessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    FirstQuestionId = table.Column<Guid>(type: "uuid", nullable: true),
                    SecondQuestionId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActiveQuestionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "InProgress"),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LiveInterviewSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LiveInterviewSessions_InterviewQuestions_FirstQuestionId",
                        column: x => x.FirstQuestionId,
                        principalTable: "InterviewQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_LiveInterviewSessions_InterviewQuestions_SecondQuestionId",
                        column: x => x.SecondQuestionId,
                        principalTable: "InterviewQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_LiveInterviewSessions_ScheduledInterviewSessions_ScheduledS~",
                        column: x => x.ScheduledSessionId,
                        principalTable: "ScheduledInterviewSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "InterviewFeedbacks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LiveSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewerId = table.Column<Guid>(type: "uuid", nullable: false),
                    RevieweeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProblemSolvingRating = table.Column<int>(type: "integer", nullable: true),
                    ProblemSolvingDescription = table.Column<string>(type: "text", nullable: true),
                    CodingSkillsRating = table.Column<int>(type: "integer", nullable: true),
                    CodingSkillsDescription = table.Column<string>(type: "text", nullable: true),
                    CommunicationRating = table.Column<int>(type: "integer", nullable: true),
                    CommunicationDescription = table.Column<string>(type: "text", nullable: true),
                    ThingsDidWell = table.Column<string>(type: "text", nullable: true),
                    AreasForImprovement = table.Column<string>(type: "text", nullable: true),
                    InterviewerPerformanceRating = table.Column<int>(type: "integer", nullable: true),
                    InterviewerPerformanceDescription = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterviewFeedbacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InterviewFeedbacks_LiveInterviewSessions_LiveSessionId",
                        column: x => x.LiveSessionId,
                        principalTable: "LiveInterviewSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InterviewFeedbacks_Users_RevieweeId",
                        column: x => x.RevieweeId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InterviewFeedbacks_Users_ReviewerId",
                        column: x => x.ReviewerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InterviewMatchingRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScheduledSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    InterviewType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PracticeType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    InterviewLevel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ScheduledStartAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    MatchedUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LiveSessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    MatchedUserConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterviewMatchingRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InterviewMatchingRequests_LiveInterviewSessions_LiveSession~",
                        column: x => x.LiveSessionId,
                        principalTable: "LiveInterviewSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_InterviewMatchingRequests_ScheduledInterviewSessions_Schedu~",
                        column: x => x.ScheduledSessionId,
                        principalTable: "ScheduledInterviewSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InterviewMatchingRequests_Users_MatchedUserId",
                        column: x => x.MatchedUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_InterviewMatchingRequests_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LiveInterviewParticipants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LiveSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Interviewee"),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LeftAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LiveInterviewParticipants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LiveInterviewParticipants_LiveInterviewSessions_LiveSession~",
                        column: x => x.LiveSessionId,
                        principalTable: "LiveInterviewSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LiveInterviewParticipants_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InterviewFeedbacks_LiveSessionId",
                table: "InterviewFeedbacks",
                column: "LiveSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewFeedbacks_LiveSessionId_ReviewerId_RevieweeId",
                table: "InterviewFeedbacks",
                columns: new[] { "LiveSessionId", "ReviewerId", "RevieweeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InterviewFeedbacks_RevieweeId",
                table: "InterviewFeedbacks",
                column: "RevieweeId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewFeedbacks_ReviewerId",
                table: "InterviewFeedbacks",
                column: "ReviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewMatchingRequests_ExpiresAt",
                table: "InterviewMatchingRequests",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewMatchingRequests_InterviewType_PracticeType_Status~",
                table: "InterviewMatchingRequests",
                columns: new[] { "InterviewType", "PracticeType", "Status", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_InterviewMatchingRequests_LiveSessionId",
                table: "InterviewMatchingRequests",
                column: "LiveSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewMatchingRequests_MatchedUserId",
                table: "InterviewMatchingRequests",
                column: "MatchedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewMatchingRequests_ScheduledSessionId",
                table: "InterviewMatchingRequests",
                column: "ScheduledSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewMatchingRequests_Status",
                table: "InterviewMatchingRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewMatchingRequests_UserId",
                table: "InterviewMatchingRequests",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LiveInterviewParticipants_LiveSessionId",
                table: "LiveInterviewParticipants",
                column: "LiveSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_LiveInterviewParticipants_LiveSessionId_UserId",
                table: "LiveInterviewParticipants",
                columns: new[] { "LiveSessionId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LiveInterviewParticipants_Role",
                table: "LiveInterviewParticipants",
                column: "Role");

            migrationBuilder.CreateIndex(
                name: "IX_LiveInterviewParticipants_UserId",
                table: "LiveInterviewParticipants",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LiveInterviewSessions_FirstQuestionId",
                table: "LiveInterviewSessions",
                column: "FirstQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_LiveInterviewSessions_ScheduledSessionId",
                table: "LiveInterviewSessions",
                column: "ScheduledSessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LiveInterviewSessions_SecondQuestionId",
                table: "LiveInterviewSessions",
                column: "SecondQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_LiveInterviewSessions_StartedAt",
                table: "LiveInterviewSessions",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LiveInterviewSessions_Status",
                table: "LiveInterviewSessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledInterviewSessions_InterviewType_PracticeType_Inter~",
                table: "ScheduledInterviewSessions",
                columns: new[] { "InterviewType", "PracticeType", "InterviewLevel" });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledInterviewSessions_ScheduledStartAt",
                table: "ScheduledInterviewSessions",
                column: "ScheduledStartAt");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledInterviewSessions_Status",
                table: "ScheduledInterviewSessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledInterviewSessions_UserId",
                table: "ScheduledInterviewSessions",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InterviewFeedbacks");

            migrationBuilder.DropTable(
                name: "InterviewMatchingRequests");

            migrationBuilder.DropTable(
                name: "LiveInterviewParticipants");

            migrationBuilder.DropTable(
                name: "LiveInterviewSessions");

            migrationBuilder.DropTable(
                name: "ScheduledInterviewSessions");
        }
    }
}
