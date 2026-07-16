using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HBP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLoginLockout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "failed_count",
                table: "admin_users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "first_failed_at",
                table: "admin_users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "locked_until",
                table: "admin_users",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "failed_count",
                table: "admin_users");

            migrationBuilder.DropColumn(
                name: "first_failed_at",
                table: "admin_users");

            migrationBuilder.DropColumn(
                name: "locked_until",
                table: "admin_users");
        }
    }
}
