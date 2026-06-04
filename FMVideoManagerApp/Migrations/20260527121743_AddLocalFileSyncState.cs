using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FMVideoManagerApp.Migrations
{
    /// <inheritdoc />
    public partial class AddLocalFileSyncState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LastSyncError",
                table: "local_file_location",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSyncedAtUtc",
                table: "local_file_location",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SyncState",
                table: "local_file_location",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastSyncError",
                table: "local_file_location");

            migrationBuilder.DropColumn(
                name: "LastSyncedAtUtc",
                table: "local_file_location");

            migrationBuilder.DropColumn(
                name: "SyncState",
                table: "local_file_location");
        }
    }
}
