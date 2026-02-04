using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vector.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLegacyUnusedTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // These tables are legacy leftovers from earlier iterations and are not part of the current EF model.
            // Drop them defensively (IF EXISTS) so older databases can be cleaned up without failing.

            migrationBuilder.Sql("DROP TABLE IF EXISTS \"MockInterviews\" CASCADE;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS \"VideoSessions\" CASCADE;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS \"PeerInterviewMatches\" CASCADE;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS \"PeerInterviewSessions\" CASCADE;");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally left blank. These legacy tables are not used by the application.
        }
    }
}
