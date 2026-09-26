using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Recam.Server.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BrowserLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BrowserLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClaimHash = table.Column<byte[]>(type: "BLOB", maxLength: 32, nullable: false),
                    ApprovalHash = table.Column<byte[]>(type: "BLOB", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    ExpiresAt = table.Column<long>(type: "INTEGER", nullable: false),
                    ApprovedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    DeviceId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BrowserLinks", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BrowserLinks");
        }
    }
}
