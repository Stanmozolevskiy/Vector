using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vector.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddExcalidrawRoomIdsToLiveInterviewSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IntervieweeRoomId",
                table: "LiveInterviewSessions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InterviewerRoomId",
                table: "LiveInterviewSessions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IntervieweeRoomId",
                table: "LiveInterviewSessions");

            migrationBuilder.DropColumn(
                name: "InterviewerRoomId",
                table: "LiveInterviewSessions");
        }
    }
}
