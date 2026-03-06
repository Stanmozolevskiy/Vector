using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vector.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLearningAnalytics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LearningAnalytics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionsSolved = table.Column<int>(type: "integer", nullable: false),
                    QuestionsByCategory = table.Column<string>(type: "text", nullable: true),
                    QuestionsByDifficulty = table.Column<string>(type: "text", nullable: true),
                    AverageExecutionTime = table.Column<decimal>(type: "numeric", nullable: false),
                    AverageMemoryUsed = table.Column<long>(type: "bigint", nullable: false),
                    SuccessRate = table.Column<decimal>(type: "numeric", nullable: false),
                    CurrentStreak = table.Column<int>(type: "integer", nullable: false),
                    LongestStreak = table.Column<int>(type: "integer", nullable: false),
                    LastActivityDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TotalSubmissions = table.Column<int>(type: "integer", nullable: false),
                    SolutionsByLanguage = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningAnalytics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LearningAnalytics_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LearningAnalytics_LastActivityDate",
                table: "LearningAnalytics",
                column: "LastActivityDate");

            migrationBuilder.CreateIndex(
                name: "IX_LearningAnalytics_UserId",
                table: "LearningAnalytics",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LearningAnalytics");
        }
    }
}
