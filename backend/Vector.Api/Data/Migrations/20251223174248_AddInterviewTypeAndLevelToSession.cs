using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vector.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInterviewTypeAndLevelToSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InterviewLevel",
                table: "PeerInterviewSessions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InterviewType",
                table: "PeerInterviewSessions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PracticeType",
                table: "PeerInterviewSessions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InterviewLevel",
                table: "PeerInterviewSessions");

            migrationBuilder.DropColumn(
                name: "InterviewType",
                table: "PeerInterviewSessions");

            migrationBuilder.DropColumn(
                name: "PracticeType",
                table: "PeerInterviewSessions");
        }
    }
}
