using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vector.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserSessionParticipant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserSessionParticipants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Active"),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LeftAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsConnected = table.Column<bool>(type: "boolean", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSessionParticipants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSessionParticipants_PeerInterviewSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "PeerInterviewSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserSessionParticipants_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserSessionParticipants_SessionId",
                table: "UserSessionParticipants",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessionParticipants_UserId",
                table: "UserSessionParticipants",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessionParticipants_UserId_SessionId",
                table: "UserSessionParticipants",
                columns: new[] { "UserId", "SessionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserSessionParticipants");
        }
    }
}
