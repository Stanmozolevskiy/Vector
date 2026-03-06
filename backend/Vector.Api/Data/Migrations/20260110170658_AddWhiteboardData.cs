using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vector.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWhiteboardData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WhiteboardData",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Elements = table.Column<string>(type: "text", nullable: false),
                    AppState = table.Column<string>(type: "text", nullable: false),
                    Files = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WhiteboardData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WhiteboardData_InterviewQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "InterviewQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_WhiteboardData_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WhiteboardData_QuestionId",
                table: "WhiteboardData",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_WhiteboardData_UpdatedAt",
                table: "WhiteboardData",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WhiteboardData_UserId",
                table: "WhiteboardData",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WhiteboardData_UserId_QuestionId",
                table: "WhiteboardData",
                columns: new[] { "UserId", "QuestionId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WhiteboardData");
        }
    }
}
