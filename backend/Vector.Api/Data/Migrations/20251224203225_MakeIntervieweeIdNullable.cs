using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vector.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class MakeIntervieweeIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InterviewMatchingRequests_InterviewMatchingRequests_Matched~",
                table: "InterviewMatchingRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_InterviewMatchingRequests_PeerInterviewSessions_ScheduledSe~",
                table: "InterviewMatchingRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_InterviewMatchingRequests_Users_MatchedUserId",
                table: "InterviewMatchingRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_InterviewMatchingRequests_Users_UserId",
                table: "InterviewMatchingRequests");

            migrationBuilder.AlterColumn<Guid>(
                name: "IntervieweeId",
                table: "PeerInterviewSessions",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "InterviewMatchingRequests",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Pending",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.CreateIndex(
                name: "IX_InterviewMatchingRequests_Status",
                table: "InterviewMatchingRequests",
                column: "Status");

            migrationBuilder.AddForeignKey(
                name: "FK_InterviewMatchingRequests_InterviewMatchingRequests_Matched~",
                table: "InterviewMatchingRequests",
                column: "MatchedRequestId",
                principalTable: "InterviewMatchingRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InterviewMatchingRequests_PeerInterviewSessions_ScheduledSe~",
                table: "InterviewMatchingRequests",
                column: "ScheduledSessionId",
                principalTable: "PeerInterviewSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InterviewMatchingRequests_Users_MatchedUserId",
                table: "InterviewMatchingRequests",
                column: "MatchedUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InterviewMatchingRequests_Users_UserId",
                table: "InterviewMatchingRequests",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InterviewMatchingRequests_InterviewMatchingRequests_Matched~",
                table: "InterviewMatchingRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_InterviewMatchingRequests_PeerInterviewSessions_ScheduledSe~",
                table: "InterviewMatchingRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_InterviewMatchingRequests_Users_MatchedUserId",
                table: "InterviewMatchingRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_InterviewMatchingRequests_Users_UserId",
                table: "InterviewMatchingRequests");

            migrationBuilder.DropIndex(
                name: "IX_InterviewMatchingRequests_Status",
                table: "InterviewMatchingRequests");

            migrationBuilder.AlterColumn<Guid>(
                name: "IntervieweeId",
                table: "PeerInterviewSessions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "InterviewMatchingRequests",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldDefaultValue: "Pending");

            migrationBuilder.AddForeignKey(
                name: "FK_InterviewMatchingRequests_InterviewMatchingRequests_Matched~",
                table: "InterviewMatchingRequests",
                column: "MatchedRequestId",
                principalTable: "InterviewMatchingRequests",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InterviewMatchingRequests_PeerInterviewSessions_ScheduledSe~",
                table: "InterviewMatchingRequests",
                column: "ScheduledSessionId",
                principalTable: "PeerInterviewSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_InterviewMatchingRequests_Users_MatchedUserId",
                table: "InterviewMatchingRequests",
                column: "MatchedUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InterviewMatchingRequests_Users_UserId",
                table: "InterviewMatchingRequests",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
