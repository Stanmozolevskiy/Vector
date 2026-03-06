using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vector.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestionType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "QuestionType",
                table: "InterviewQuestions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Coding");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewQuestions_QuestionType",
                table: "InterviewQuestions",
                column: "QuestionType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InterviewQuestions_QuestionType",
                table: "InterviewQuestions");

            migrationBuilder.DropColumn(
                name: "QuestionType",
                table: "InterviewQuestions");
        }
    }
}
