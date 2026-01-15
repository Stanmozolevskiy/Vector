using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vector.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionIdToWhiteboardData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "WhiteboardData",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "SessionId",
                table: "WhiteboardData",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WhiteboardData_SessionId",
                table: "WhiteboardData",
                column: "SessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WhiteboardData_SessionId",
                table: "WhiteboardData");

            migrationBuilder.DropColumn(
                name: "SessionId",
                table: "WhiteboardData");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "WhiteboardData",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
