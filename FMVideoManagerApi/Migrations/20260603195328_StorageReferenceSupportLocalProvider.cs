using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FMVideoManagerApi.Migrations
{
    /// <inheritdoc />
    public partial class StorageReferenceSupportLocalProvider : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UQ_storage_reference_provider_item",
                table: "storage_reference");

            migrationBuilder.AlterColumn<long>(
                name: "cloud_provider_account_id",
                table: "storage_reference",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.CreateIndex(
                name: "UQ_storage_reference_cloud_account_provider_item",
                table: "storage_reference",
                columns: new[] { "cloud_provider_account_id", "provider_item_id" },
                unique: true,
                filter: "cloud_provider_account_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UQ_storage_reference_local_user_provider_item",
                table: "storage_reference",
                columns: new[] { "user_id", "provider", "provider_item_id" },
                unique: true,
                filter: "provider = 'Local'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UQ_storage_reference_cloud_account_provider_item",
                table: "storage_reference");

            migrationBuilder.DropIndex(
                name: "UQ_storage_reference_local_user_provider_item",
                table: "storage_reference");

            migrationBuilder.AlterColumn<long>(
                name: "cloud_provider_account_id",
                table: "storage_reference",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "UQ_storage_reference_provider_item",
                table: "storage_reference",
                columns: new[] { "cloud_provider_account_id", "provider_item_id" },
                unique: true);
        }
    }
}
