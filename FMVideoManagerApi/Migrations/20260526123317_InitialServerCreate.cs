using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FMVideoManagerApi.Migrations
{
    /// <inheritdoc />
    public partial class InitialServerCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "file_content",
                columns: table => new
                {
                    hash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    size_bytes = table.Column<long>(type: "INTEGER", nullable: false),
                    mime_type = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    duration_ms = table.Column<long>(type: "INTEGER", nullable: true),
                    width = table.Column<int>(type: "INTEGER", nullable: true),
                    height = table.Column<int>(type: "INTEGER", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_file_content", x => x.hash);
                });

            migrationBuilder.CreateTable(
                name: "user",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    login = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    alias = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    password_hash = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cloud_provider_account",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    user_id = table.Column<long>(type: "INTEGER", nullable: false),
                    provider = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    provider_account_id = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    display_name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    email = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    access_token_encrypted = table.Column<string>(type: "TEXT", nullable: false),
                    refresh_token_encrypted = table.Column<string>(type: "TEXT", nullable: true),
                    token_expires_at_utc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    scopes = table.Column<string>(type: "TEXT", nullable: true),
                    is_active = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    created_at_utc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    last_sync_at_utc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cloud_provider_account", x => x.id);
                    table.ForeignKey(
                        name: "FK_cloud_provider_account_user",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "hierarchy_node",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    user_id = table.Column<long>(type: "INTEGER", nullable: false),
                    parent_node_id = table.Column<long>(type: "INTEGER", nullable: true),
                    node_type = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    title = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    sort_order = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    created_at_utc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hierarchy_node", x => x.id);
                    table.UniqueConstraint("UQ_hierarchy_node_id_user", x => new { x.id, x.user_id });
                    table.CheckConstraint("CK_hierarchy_node_not_self_parent", "parent_node_id IS NULL OR parent_node_id <> id");
                    table.CheckConstraint("CK_hierarchy_node_type", "node_type IN ('Group', 'File')");
                    table.ForeignKey(
                        name: "FK_hierarchy_node_parent_same_user",
                        columns: x => new { x.parent_node_id, x.user_id },
                        principalTable: "hierarchy_node",
                        principalColumns: new[] { "id", "user_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_hierarchy_node_user",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tag",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    user_id = table.Column<long>(type: "INTEGER", nullable: false),
                    name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "TEXT", nullable: true),
                    background_color_hex = table.Column<string>(type: "TEXT", maxLength: 8, nullable: true),
                    foreground_color_hex = table.Column<string>(type: "TEXT", maxLength: 8, nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tag", x => x.id);
                    table.UniqueConstraint("UQ_tag_id_user", x => new { x.id, x.user_id });
                    table.ForeignKey(
                        name: "FK_tag_user",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "file_item",
                columns: table => new
                {
                    node_id = table.Column<long>(type: "INTEGER", nullable: false),
                    content_hash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    original_filename = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    notes = table.Column<string>(type: "TEXT", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_file_item", x => x.node_id);
                    table.ForeignKey(
                        name: "FK_file_item_content",
                        column: x => x.content_hash,
                        principalTable: "file_content",
                        principalColumn: "hash",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_file_item_node",
                        column: x => x.node_id,
                        principalTable: "hierarchy_node",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "group_item",
                columns: table => new
                {
                    node_id = table.Column<long>(type: "INTEGER", nullable: false),
                    description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    background_color_hex = table.Column<string>(type: "TEXT", maxLength: 8, nullable: true),
                    foreground_color_hex = table.Column<string>(type: "TEXT", maxLength: 8, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_group_item", x => x.node_id);
                    table.ForeignKey(
                        name: "FK_group_item_node",
                        column: x => x.node_id,
                        principalTable: "hierarchy_node",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "node_alias",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    node_id = table.Column<long>(type: "INTEGER", nullable: false),
                    alias = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_node_alias", x => x.id);
                    table.ForeignKey(
                        name: "FK_node_alias_node",
                        column: x => x.node_id,
                        principalTable: "hierarchy_node",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "node_tag",
                columns: table => new
                {
                    node_id = table.Column<long>(type: "INTEGER", nullable: false),
                    tag_id = table.Column<long>(type: "INTEGER", nullable: false),
                    user_id = table.Column<long>(type: "INTEGER", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_node_tag", x => new { x.node_id, x.tag_id });
                    table.ForeignKey(
                        name: "FK_node_tag_node",
                        columns: x => new { x.node_id, x.user_id },
                        principalTable: "hierarchy_node",
                        principalColumns: new[] { "id", "user_id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_node_tag_tag",
                        columns: x => new { x.tag_id, x.user_id },
                        principalTable: "tag",
                        principalColumns: new[] { "id", "user_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tag_alias",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    tag_id = table.Column<long>(type: "INTEGER", nullable: false),
                    user_id = table.Column<long>(type: "INTEGER", nullable: false),
                    alias = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tag_alias", x => x.id);
                    table.ForeignKey(
                        name: "FK_tag_alias_tag",
                        columns: x => new { x.tag_id, x.user_id },
                        principalTable: "tag",
                        principalColumns: new[] { "id", "user_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "storage_reference",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    user_id = table.Column<long>(type: "INTEGER", nullable: false),
                    file_node_id = table.Column<long>(type: "INTEGER", nullable: true),
                    content_hash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    cloud_provider_account_id = table.Column<long>(type: "INTEGER", nullable: false),
                    provider = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    provider_item_id = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    provider_path = table.Column<string>(type: "TEXT", nullable: true),
                    name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    provider_revision = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    mime_type = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    size_bytes = table.Column<long>(type: "INTEGER", nullable: true),
                    provider_modified_at_utc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    last_seen_at_utc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    state = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    metadata_json = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_storage_reference", x => x.id);
                    table.ForeignKey(
                        name: "FK_storage_reference_cloud_provider_account",
                        column: x => x.cloud_provider_account_id,
                        principalTable: "cloud_provider_account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_storage_reference_content",
                        column: x => x.content_hash,
                        principalTable: "file_content",
                        principalColumn: "hash",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_storage_reference_file_item",
                        column: x => x.file_node_id,
                        principalTable: "file_item",
                        principalColumn: "node_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_storage_reference_user",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "UQ_cloud_provider_account_user_provider_account",
                table: "cloud_provider_account",
                columns: new[] { "user_id", "provider", "provider_account_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_file_content_size_bytes",
                table: "file_content",
                column: "size_bytes");

            migrationBuilder.CreateIndex(
                name: "IX_file_item_content_hash",
                table: "file_item",
                column: "content_hash");

            migrationBuilder.CreateIndex(
                name: "IX_hierarchy_node_parent_node_id",
                table: "hierarchy_node",
                column: "parent_node_id");

            migrationBuilder.CreateIndex(
                name: "IX_hierarchy_node_parent_node_id_user_id",
                table: "hierarchy_node",
                columns: new[] { "parent_node_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "IX_hierarchy_node_title",
                table: "hierarchy_node",
                column: "title");

            migrationBuilder.CreateIndex(
                name: "IX_hierarchy_node_user_id",
                table: "hierarchy_node",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_hierarchy_node_user_parent",
                table: "hierarchy_node",
                columns: new[] { "user_id", "parent_node_id" });

            migrationBuilder.CreateIndex(
                name: "IX_hierarchy_node_user_type",
                table: "hierarchy_node",
                columns: new[] { "user_id", "node_type" });

            migrationBuilder.CreateIndex(
                name: "IX_node_alias_alias",
                table: "node_alias",
                column: "alias");

            migrationBuilder.CreateIndex(
                name: "UQ_node_alias_node_alias",
                table: "node_alias",
                columns: new[] { "node_id", "alias" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_node_tag_node_id",
                table: "node_tag",
                column: "node_id");

            migrationBuilder.CreateIndex(
                name: "IX_node_tag_node_id_user_id",
                table: "node_tag",
                columns: new[] { "node_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "IX_node_tag_tag_id",
                table: "node_tag",
                column: "tag_id");

            migrationBuilder.CreateIndex(
                name: "IX_node_tag_tag_id_user_id",
                table: "node_tag",
                columns: new[] { "tag_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "IX_node_tag_user_tag",
                table: "node_tag",
                columns: new[] { "user_id", "tag_id" });

            migrationBuilder.CreateIndex(
                name: "IX_storage_reference_cloud_provider_account_id",
                table: "storage_reference",
                column: "cloud_provider_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_storage_reference_content_hash",
                table: "storage_reference",
                column: "content_hash");

            migrationBuilder.CreateIndex(
                name: "IX_storage_reference_file_node_id",
                table: "storage_reference",
                column: "file_node_id");

            migrationBuilder.CreateIndex(
                name: "IX_storage_reference_provider_item",
                table: "storage_reference",
                columns: new[] { "provider", "provider_item_id" });

            migrationBuilder.CreateIndex(
                name: "IX_storage_reference_user_id",
                table: "storage_reference",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "UQ_storage_reference_provider_item",
                table: "storage_reference",
                columns: new[] { "cloud_provider_account_id", "provider_item_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tag_user_id",
                table: "tag",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "UQ_tag_user_name",
                table: "tag",
                columns: new[] { "user_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tag_alias_tag_id_user_id",
                table: "tag_alias",
                columns: new[] { "tag_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "UQ_tag_alias_user_alias",
                table: "tag_alias",
                columns: new[] { "user_id", "alias" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_user_login",
                table: "user",
                column: "login",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "group_item");

            migrationBuilder.DropTable(
                name: "node_alias");

            migrationBuilder.DropTable(
                name: "node_tag");

            migrationBuilder.DropTable(
                name: "storage_reference");

            migrationBuilder.DropTable(
                name: "tag_alias");

            migrationBuilder.DropTable(
                name: "cloud_provider_account");

            migrationBuilder.DropTable(
                name: "file_item");

            migrationBuilder.DropTable(
                name: "tag");

            migrationBuilder.DropTable(
                name: "file_content");

            migrationBuilder.DropTable(
                name: "hierarchy_node");

            migrationBuilder.DropTable(
                name: "user");
        }
    }
}
