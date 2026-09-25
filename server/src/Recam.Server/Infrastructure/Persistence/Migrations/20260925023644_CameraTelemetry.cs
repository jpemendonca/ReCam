using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Recam.Server.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CameraTelemetry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BatteryLevel",
                table: "Devices",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCharging",
                table: "Devices",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TelemetryAt",
                table: "Devices",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BatteryLevel",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "IsCharging",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "TelemetryAt",
                table: "Devices");
        }
    }
}
