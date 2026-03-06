using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vector.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestionMetadataAndComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RelatedCourseIds",
                table: "InterviewQuestions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RelatedQuestionIds",
                table: "InterviewQuestions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RoleTags",
                table: "InterviewQuestions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VideoUrl",
                table: "InterviewQuestions",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "InterviewQuestionComments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterviewQuestionComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InterviewQuestionComments_InterviewQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "InterviewQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InterviewQuestionComments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InterviewQuestionComments_CreatedAt",
                table: "InterviewQuestionComments",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewQuestionComments_QuestionId",
                table: "InterviewQuestionComments",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewQuestionComments_UserId",
                table: "InterviewQuestionComments",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InterviewQuestionComments");

            migrationBuilder.DropColumn(
                name: "RelatedCourseIds",
                table: "InterviewQuestions");

            migrationBuilder.DropColumn(
                name: "RelatedQuestionIds",
                table: "InterviewQuestions");

            migrationBuilder.DropColumn(
                name: "RoleTags",
                table: "InterviewQuestions");

            migrationBuilder.DropColumn(
                name: "VideoUrl",
                table: "InterviewQuestions");
        }
    }
}
