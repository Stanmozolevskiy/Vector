using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vector.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AllowNullableIntervieweeId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "VideoSessions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Active",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.CreateTable(
                name: "InterviewMatchingRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScheduledSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MatchedUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    MatchedRequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    MatchedUserConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterviewMatchingRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InterviewMatchingRequests_InterviewMatchingRequests_Matched~",
                        column: x => x.MatchedRequestId,
                        principalTable: "InterviewMatchingRequests",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InterviewMatchingRequests_PeerInterviewSessions_ScheduledSe~",
                        column: x => x.ScheduledSessionId,
                        principalTable: "PeerInterviewSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InterviewMatchingRequests_Users_MatchedUserId",
                        column: x => x.MatchedUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InterviewMatchingRequests_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VideoSessions_Token",
                table: "VideoSessions",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InterviewMatchingRequests_MatchedRequestId",
                table: "InterviewMatchingRequests",
                column: "MatchedRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewMatchingRequests_MatchedUserId",
                table: "InterviewMatchingRequests",
                column: "MatchedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewMatchingRequests_ScheduledSessionId",
                table: "InterviewMatchingRequests",
                column: "ScheduledSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewMatchingRequests_UserId",
                table: "InterviewMatchingRequests",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InterviewMatchingRequests");

            migrationBuilder.DropIndex(
                name: "IX_VideoSessions_Token",
                table: "VideoSessions");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "VideoSessions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldDefaultValue: "Active");
        }
    }
}
