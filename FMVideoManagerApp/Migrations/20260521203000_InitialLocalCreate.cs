using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FMVideoManagerApp.Migrations
{
    /// <inheritdoc />
    public partial class InitialLocalCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "local_indexed_path",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    server_user_id = table.Column<long>(type: "INTEGER", nullable: false),
                    path = table.Column<string>(type: "TEXT", nullable: false),
                    is_enabled = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    include_subdirectories = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    created_at_utc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    last_scanned_at_utc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_local_indexed_path", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "preview_cache",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    server_user_id = table.Column<long>(type: "INTEGER", nullable: false),
                    server_file_item_id = table.Column<long>(type: "INTEGER", nullable: true),
                    content_hash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    preview_path = table.Column<string>(type: "TEXT", nullable: false),
                    width = table.Column<int>(type: "INTEGER", nullable: true),
                    height = table.Column<int>(type: "INTEGER", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    last_accessed_at_utc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_preview_cache", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "local_file_location",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    server_user_id = table.Column<long>(type: "INTEGER", nullable: false),
                    server_file_item_id = table.Column<long>(type: "INTEGER", nullable: true),
                    content_hash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    local_indexed_path_id = table.Column<long>(type: "INTEGER", nullable: true),
                    path = table.Column<string>(type: "TEXT", nullable: false),
                    filename = table.Column<string>(type: "TEXT", nullable: false),
                    size_bytes = table.Column<long>(type: "INTEGER", nullable: false),
                    last_modified_utc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    last_seen_utc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    exists_on_disk = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_local_file_location", x => x.id);
                    table.ForeignKey(
                        name: "FK_local_file_location_indexed_path",
                        column: x => x.local_indexed_path_id,
                        principalTable: "local_indexed_path",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_local_file_location_content_hash",
                table: "local_file_location",
                column: "content_hash");

            migrationBuilder.CreateIndex(
                name: "IX_local_file_location_local_indexed_path_id",
                table: "local_file_location",
                column: "local_indexed_path_id");

            migrationBuilder.CreateIndex(
                name: "IX_local_file_location_server_file_item_id",
                table: "local_file_location",
                column: "server_file_item_id");

            migrationBuilder.CreateIndex(
                name: "UQ_local_file_location_user_path",
                table: "local_file_location",
                columns: new[] { "server_user_id", "path" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_local_indexed_path_user_path",
                table: "local_indexed_path",
                columns: new[] { "server_user_id", "path" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_preview_cache_content_hash",
                table: "preview_cache",
                column: "content_hash");

            migrationBuilder.CreateIndex(
                name: "IX_preview_cache_server_file_item_id",
                table: "preview_cache",
                column: "server_file_item_id");

            migrationBuilder.CreateIndex(
                name: "UQ_preview_cache_path",
                table: "preview_cache",
                column: "preview_path",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "local_file_location");

            migrationBuilder.DropTable(
                name: "preview_cache");

            migrationBuilder.DropTable(
                name: "local_indexed_path");
        }
    }
}
