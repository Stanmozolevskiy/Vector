using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vector.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAssignedQuestionIdToScheduledInterviewSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AssignedQuestionId",
                table: "ScheduledInterviewSessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledInterviewSessions_AssignedQuestionId",
                table: "ScheduledInterviewSessions",
                column: "AssignedQuestionId");

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduledInterviewSessions_InterviewQuestions_AssignedQuest~",
                table: "ScheduledInterviewSessions",
                column: "AssignedQuestionId",
                principalTable: "InterviewQuestions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScheduledInterviewSessions_InterviewQuestions_AssignedQuest~",
                table: "ScheduledInterviewSessions");

            migrationBuilder.DropIndex(
                name: "IX_ScheduledInterviewSessions_AssignedQuestionId",
                table: "ScheduledInterviewSessions");

            migrationBuilder.DropColumn(
                name: "AssignedQuestionId",
                table: "ScheduledInterviewSessions");
        }
    }
}
