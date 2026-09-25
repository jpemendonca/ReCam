using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Recam.Server.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CameraTemperature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "TemperatureC",
                table: "Devices",
                type: "REAL",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TemperatureC",
                table: "Devices");
        }
    }
}
