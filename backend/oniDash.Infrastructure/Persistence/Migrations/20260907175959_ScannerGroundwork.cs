using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace oniDash.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ScannerGroundwork : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "LastScannedAtUtc",
                table: "Sources",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdentityKey",
                table: "Files",
                type: "TEXT",
                maxLength: 1100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "MissingSinceUtc",
                table: "Files",
                type: "INTEGER",
                nullable: true);

            // Backfill identity keys for rows indexed before this column existed
            // (rule 12: preserve user data; the format matches ScanService.BuildIdentityKey,
            // i.e. Guid "N" form + lowercase '/'-separated relative path).
            migrationBuilder.Sql("""
                UPDATE Files
                SET IdentityKey = lower(replace(upper(LibrarySourceId), '-', '')) || '/' || lower(RelativePath)
                WHERE IdentityKey = '';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Files_IdentityKey",
                table: "Files",
                column: "IdentityKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Files_IdentityKey",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "LastScannedAtUtc",
                table: "Sources");

            migrationBuilder.DropColumn(
                name: "IdentityKey",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "MissingSinceUtc",
                table: "Files");
        }
    }
}
