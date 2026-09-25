using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Recam.Server.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CameraRecording : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RecordingEnabled",
                table: "Devices",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SupportsH264",
                table: "Devices",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RecordingEnabled",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "SupportsH264",
                table: "Devices");
        }
    }
}
