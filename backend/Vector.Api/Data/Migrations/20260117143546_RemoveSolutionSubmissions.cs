using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vector.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSolutionSubmissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SolutionSubmissions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SolutionSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TestCaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserSolutionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    ExecutionTime = table.Column<decimal>(type: "numeric", nullable: false),
                    ExpectedOutput = table.Column<string>(type: "text", nullable: true),
                    MemoryUsed = table.Column<long>(type: "bigint", nullable: false),
                    Output = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TestCaseNumber = table.Column<int>(type: "integer", nullable: false)
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
        }
    }
}
