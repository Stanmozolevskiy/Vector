using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vector.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCommentVotesAndReplies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ParentCommentId",
                table: "InterviewQuestionComments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "InterviewQuestionCommentVotes",
                columns: table => new
                {
                    CommentId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterviewQuestionCommentVotes", x => new { x.CommentId, x.UserId });
                    table.ForeignKey(
                        name: "FK_InterviewQuestionCommentVotes_InterviewQuestionComments_Com~",
                        column: x => x.CommentId,
                        principalTable: "InterviewQuestionComments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InterviewQuestionCommentVotes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InterviewQuestionComments_ParentCommentId",
                table: "InterviewQuestionComments",
                column: "ParentCommentId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewQuestionCommentVotes_UserId",
                table: "InterviewQuestionCommentVotes",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_InterviewQuestionComments_InterviewQuestionComments_ParentC~",
                table: "InterviewQuestionComments",
                column: "ParentCommentId",
                principalTable: "InterviewQuestionComments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InterviewQuestionComments_InterviewQuestionComments_ParentC~",
                table: "InterviewQuestionComments");

            migrationBuilder.DropTable(
                name: "InterviewQuestionCommentVotes");

            migrationBuilder.DropIndex(
                name: "IX_InterviewQuestionComments_ParentCommentId",
                table: "InterviewQuestionComments");

            migrationBuilder.DropColumn(
                name: "ParentCommentId",
                table: "InterviewQuestionComments");
        }
    }
}
