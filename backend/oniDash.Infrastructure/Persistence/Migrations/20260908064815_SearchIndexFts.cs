using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace oniDash.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SearchIndexFts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // FTS5 index over media items. ItemId/LibraryId are UNINDEXED payload columns;
            // DisplayName is the searchable text. A standalone (non-external-content)
            // table keeps triggers simple and works with TEXT GUID ids.
            migrationBuilder.Sql(
                """
                CREATE VIRTUAL TABLE IF NOT EXISTS "MediaItemsFts" USING fts5(
                    "ItemId" UNINDEXED,
                    "LibraryId" UNINDEXED,
                    "DisplayName"
                );
                """);

            migrationBuilder.Sql(
                """
                CREATE TRIGGER IF NOT EXISTS "MediaItems_FtsInsert"
                AFTER INSERT ON "MediaItems" BEGIN
                    INSERT INTO "MediaItemsFts"("ItemId", "LibraryId", "DisplayName")
                    SELECT new."Id", new."LibraryId", new."DisplayName"
                    WHERE NOT EXISTS (SELECT 1 FROM "MediaItemsFts" WHERE "ItemId" = new."Id");
                END;
                """);

            migrationBuilder.Sql(
                """
                CREATE TRIGGER IF NOT EXISTS "MediaItems_FtsDelete"
                AFTER DELETE ON "MediaItems" BEGIN
                    DELETE FROM "MediaItemsFts" WHERE "ItemId" = old."Id";
                END;
                """);

            migrationBuilder.Sql(
                """
                CREATE TRIGGER IF NOT EXISTS "MediaItems_FtsUpdate"
                AFTER UPDATE ON "MediaItems" BEGIN
                    DELETE FROM "MediaItemsFts" WHERE "ItemId" = old."Id";
                    INSERT INTO "MediaItemsFts"("ItemId", "LibraryId", "DisplayName")
                    VALUES (new."Id", new."LibraryId", new."DisplayName");
                END;
                """);

            // Existing items indexed once at migration time; triggers keep it live after.
            migrationBuilder.Sql(
                """
                INSERT INTO "MediaItemsFts"("ItemId", "LibraryId", "DisplayName")
                SELECT "Id", "LibraryId", "DisplayName" FROM "MediaItems";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TRIGGER IF EXISTS ""MediaItems_FtsInsert"";");
            migrationBuilder.Sql(@"DROP TRIGGER IF EXISTS ""MediaItems_FtsDelete"";");
            migrationBuilder.Sql(@"DROP TRIGGER IF EXISTS ""MediaItems_FtsUpdate"";");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS ""MediaItemsFts"";");
        }
    }
}
