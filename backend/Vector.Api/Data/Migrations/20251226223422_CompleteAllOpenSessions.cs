using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vector.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class CompleteAllOpenSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Mark all InProgress and Scheduled sessions as Completed
            migrationBuilder.Sql(@"
                UPDATE ""PeerInterviewSessions""
                SET ""Status"" = 'Completed', ""UpdatedAt"" = NOW()
                WHERE ""Status"" IN ('InProgress', 'Scheduled');
            ");

            // Mark all Active participants as Completed
            migrationBuilder.Sql(@"
                UPDATE ""UserSessionParticipants""
                SET ""Status"" = 'Completed', ""UpdatedAt"" = NOW()
                WHERE ""Status"" = 'Active';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
