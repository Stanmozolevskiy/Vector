using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vector.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestionApprovalWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApprovalStatus",
                table: "InterviewQuestions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Pending");

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "InterviewQuestions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ApprovedBy",
                table: "InterviewQuestions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "InterviewQuestions",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_InterviewQuestions_ApprovalStatus",
                table: "InterviewQuestions",
                column: "ApprovalStatus");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewQuestions_ApprovedBy",
                table: "InterviewQuestions",
                column: "ApprovedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_InterviewQuestions_Users_ApprovedBy",
                table: "InterviewQuestions",
                column: "ApprovedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InterviewQuestions_Users_ApprovedBy",
                table: "InterviewQuestions");

            migrationBuilder.DropIndex(
                name: "IX_InterviewQuestions_ApprovalStatus",
                table: "InterviewQuestions");

            migrationBuilder.DropIndex(
                name: "IX_InterviewQuestions_ApprovedBy",
                table: "InterviewQuestions");

            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                table: "InterviewQuestions");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "InterviewQuestions");

            migrationBuilder.DropColumn(
                name: "ApprovedBy",
                table: "InterviewQuestions");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "InterviewQuestions");
        }
    }
}
