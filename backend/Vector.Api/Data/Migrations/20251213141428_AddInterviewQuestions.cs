using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vector.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInterviewQuestions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InterviewQuestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Difficulty = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CompanyTags = table.Column<string>(type: "text", nullable: true),
                    Tags = table.Column<string>(type: "text", nullable: true),
                    Constraints = table.Column<string>(type: "text", nullable: true),
                    Examples = table.Column<string>(type: "text", nullable: true),
                    Hints = table.Column<string>(type: "text", nullable: true),
                    TimeComplexityHint = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SpaceComplexityHint = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    AcceptanceRate = table.Column<double>(type: "double precision", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterviewQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InterviewQuestions_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "QuestionSolutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Language = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Explanation = table.Column<string>(type: "text", nullable: true),
                    TimeComplexity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SpaceComplexity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsOfficial = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionSolutions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestionSolutions_InterviewQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "InterviewQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QuestionSolutions_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "QuestionTestCases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TestCaseNumber = table.Column<int>(type: "integer", nullable: false),
                    Input = table.Column<string>(type: "text", nullable: false),
                    ExpectedOutput = table.Column<string>(type: "text", nullable: false),
                    IsHidden = table.Column<bool>(type: "boolean", nullable: false),
                    Explanation = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionTestCases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestionTestCases_InterviewQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "InterviewQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InterviewQuestions_Category",
                table: "InterviewQuestions",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewQuestions_CreatedBy",
                table: "InterviewQuestions",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewQuestions_Difficulty",
                table: "InterviewQuestions",
                column: "Difficulty");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewQuestions_IsActive",
                table: "InterviewQuestions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionSolutions_CreatedBy",
                table: "QuestionSolutions",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionSolutions_Language",
                table: "QuestionSolutions",
                column: "Language");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionSolutions_QuestionId",
                table: "QuestionSolutions",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionTestCases_QuestionId",
                table: "QuestionTestCases",
                column: "QuestionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuestionSolutions");

            migrationBuilder.DropTable(
                name: "QuestionTestCases");

            migrationBuilder.DropTable(
                name: "InterviewQuestions");
        }
    }
}
