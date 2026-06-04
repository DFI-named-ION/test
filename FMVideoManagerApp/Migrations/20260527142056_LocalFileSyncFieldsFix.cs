using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FMVideoManagerApp.Migrations
{
    /// <inheritdoc />
    public partial class LocalFileSyncFieldsFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SyncState",
                table: "local_file_location",
                newName: "sync_state");

            migrationBuilder.RenameColumn(
                name: "LastSyncedAtUtc",
                table: "local_file_location",
                newName: "last_synced_at_utc");

            migrationBuilder.RenameColumn(
                name: "LastSyncError",
                table: "local_file_location",
                newName: "last_sync_error");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "sync_state",
                table: "local_file_location",
                newName: "SyncState");

            migrationBuilder.RenameColumn(
                name: "last_synced_at_utc",
                table: "local_file_location",
                newName: "LastSyncedAtUtc");

            migrationBuilder.RenameColumn(
                name: "last_sync_error",
                table: "local_file_location",
                newName: "LastSyncError");
        }
    }
}
