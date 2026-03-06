using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vector.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserSolutionTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserSolutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Language = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ExecutionTime = table.Column<decimal>(type: "numeric", nullable: false),
                    MemoryUsed = table.Column<long>(type: "bigint", nullable: false),
                    TestCasesPassed = table.Column<int>(type: "integer", nullable: false),
                    TotalTestCases = table.Column<int>(type: "integer", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSolutions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSolutions_InterviewQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "InterviewQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserSolutions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SolutionSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserSolutionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TestCaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    TestCaseNumber = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Output = table.Column<string>(type: "text", nullable: true),
                    ExpectedOutput = table.Column<string>(type: "text", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    ExecutionTime = table.Column<decimal>(type: "numeric", nullable: false),
                    MemoryUsed = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolutionSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SolutionSubmissions_QuestionTestCases_TestCaseId",
                        column: x => x.TestCaseId,
                        principalTable: "QuestionTestCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SolutionSubmissions_UserSolutions_UserSolutionId",
                        column: x => x.UserSolutionId,
                        principalTable: "UserSolutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SolutionSubmissions_TestCaseId",
                table: "SolutionSubmissions",
                column: "TestCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_SolutionSubmissions_UserSolutionId",
                table: "SolutionSubmissions",
                column: "UserSolutionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSolutions_QuestionId",
                table: "UserSolutions",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSolutions_Status",
                table: "UserSolutions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_UserSolutions_SubmittedAt",
                table: "UserSolutions",
                column: "SubmittedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserSolutions_UserId",
                table: "UserSolutions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSolutions_UserId_QuestionId",
                table: "UserSolutions",
                columns: new[] { "UserId", "QuestionId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SolutionSubmissions");

            migrationBuilder.DropTable(
                name: "UserSolutions");
        }
    }
}
