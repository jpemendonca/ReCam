using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Recam.Server.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CameraMotion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MotionSensitivity",
                table: "Devices",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MotionSensitivity",
                table: "Devices");
        }
    }
}
