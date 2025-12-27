using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vector.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSecondQuestionToPeerInterviewSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SecondQuestionId",
                table: "PeerInterviewSessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PeerInterviewSessions_SecondQuestionId",
                table: "PeerInterviewSessions",
                column: "SecondQuestionId");

            migrationBuilder.AddForeignKey(
                name: "FK_PeerInterviewSessions_InterviewQuestions_SecondQuestionId",
                table: "PeerInterviewSessions",
                column: "SecondQuestionId",
                principalTable: "InterviewQuestions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PeerInterviewSessions_InterviewQuestions_SecondQuestionId",
                table: "PeerInterviewSessions");

            migrationBuilder.DropIndex(
                name: "IX_PeerInterviewSessions_SecondQuestionId",
                table: "PeerInterviewSessions");

            migrationBuilder.DropColumn(
                name: "SecondQuestionId",
                table: "PeerInterviewSessions");
        }
    }
}
