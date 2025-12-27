using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vector.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPeerInterviewModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PeerInterviewMatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PreferredDifficulty = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    PreferredCategories = table.Column<string>(type: "text", nullable: true),
                    Availability = table.Column<string>(type: "text", nullable: true),
                    IsAvailable = table.Column<bool>(type: "boolean", nullable: false),
                    LastMatchDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PeerInterviewMatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PeerInterviewMatches_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PeerInterviewSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InterviewerId = table.Column<Guid>(type: "uuid", nullable: false),
                    IntervieweeId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ScheduledTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Duration = table.Column<int>(type: "integer", nullable: false),
                    SessionRecordingUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PeerInterviewSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PeerInterviewSessions_InterviewQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "InterviewQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PeerInterviewSessions_Users_IntervieweeId",
                        column: x => x.IntervieweeId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PeerInterviewSessions_Users_InterviewerId",
                        column: x => x.InterviewerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PeerInterviewMatches_IsAvailable",
                table: "PeerInterviewMatches",
                column: "IsAvailable");

            migrationBuilder.CreateIndex(
                name: "IX_PeerInterviewMatches_UserId",
                table: "PeerInterviewMatches",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PeerInterviewSessions_IntervieweeId",
                table: "PeerInterviewSessions",
                column: "IntervieweeId");

            migrationBuilder.CreateIndex(
                name: "IX_PeerInterviewSessions_InterviewerId",
                table: "PeerInterviewSessions",
                column: "InterviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_PeerInterviewSessions_QuestionId",
                table: "PeerInterviewSessions",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_PeerInterviewSessions_ScheduledTime",
                table: "PeerInterviewSessions",
                column: "ScheduledTime");

            migrationBuilder.CreateIndex(
                name: "IX_PeerInterviewSessions_Status",
                table: "PeerInterviewSessions",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PeerInterviewMatches");

            migrationBuilder.DropTable(
                name: "PeerInterviewSessions");
        }
    }
}
