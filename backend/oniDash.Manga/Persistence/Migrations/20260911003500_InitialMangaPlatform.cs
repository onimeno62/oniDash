using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace oniDash.Manga.Persistence.Migrations;

[DbContext(typeof(MangaDbContext))]
[Migration("20260911003500_InitialMangaPlatform")]
public sealed class InitialMangaPlatform : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "MangaPlugins",
            columns: table => new
            {
                Id = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                Version = table.Column<string>(type: "TEXT", nullable: false),
                Installed = table.Column<bool>(type: "INTEGER", nullable: false),
                Enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                State = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_MangaPlugins", x => x.Id));

        migrationBuilder.CreateTable(
            name: "MangaTitles",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                LibraryId = table.Column<Guid>(type: "TEXT", nullable: false),
                MediaItemId = table.Column<Guid>(type: "TEXT", nullable: true),
                SourceId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                ExternalId = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                Title = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                NormalizedTitle = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                Author = table.Column<string>(type: "TEXT", nullable: true),
                Description = table.Column<string>(type: "TEXT", nullable: true),
                Favorite = table.Column<bool>(type: "INTEGER", nullable: false),
                UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_MangaTitles", x => x.Id));

        migrationBuilder.CreateTable(
            name: "MangaChapters",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                MangaId = table.Column<Guid>(type: "TEXT", nullable: false),
                SourceChapterId = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                Title = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                Number = table.Column<double>(type: "REAL", nullable: false),
                LocalPath = table.Column<string>(type: "TEXT", nullable: true),
                PageCount = table.Column<int>(type: "INTEGER", nullable: true),
                CurrentPage = table.Column<int>(type: "INTEGER", nullable: false),
                Read = table.Column<bool>(type: "INTEGER", nullable: false),
                LastReadAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MangaChapters", x => x.Id);
                table.ForeignKey(
                    name: "FK_MangaChapters_MangaTitles_MangaId",
                    column: x => x.MangaId,
                    principalTable: "MangaTitles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "MangaBookmarks",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                ChapterId = table.Column<Guid>(type: "TEXT", nullable: false),
                Page = table.Column<int>(type: "INTEGER", nullable: false),
                Note = table.Column<string>(type: "TEXT", nullable: true),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MangaBookmarks", x => x.Id);
                table.ForeignKey(
                    name: "FK_MangaBookmarks_MangaChapters_ChapterId",
                    column: x => x.ChapterId,
                    principalTable: "MangaChapters",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "MangaDownloads",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                ChapterId = table.Column<Guid>(type: "TEXT", nullable: false),
                State = table.Column<string>(type: "TEXT", nullable: false),
                DestinationPath = table.Column<string>(type: "TEXT", nullable: true),
                Error = table.Column<string>(type: "TEXT", nullable: true),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MangaDownloads", x => x.Id);
                table.ForeignKey(
                    name: "FK_MangaDownloads_MangaChapters_ChapterId",
                    column: x => x.ChapterId,
                    principalTable: "MangaChapters",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "MangaNotifications",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                MangaId = table.Column<Guid>(type: "TEXT", nullable: false),
                MangaTitle = table.Column<string>(type: "TEXT", nullable: false),
                ChapterTitle = table.Column<string>(type: "TEXT", nullable: false),
                ChapterNumber = table.Column<double>(type: "REAL", nullable: false),
                Read = table.Column<bool>(type: "INTEGER", nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_MangaNotifications", x => x.Id));

        migrationBuilder.CreateTable(
            name: "MangaTrackingSyncs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                MangaId = table.Column<Guid>(type: "TEXT", nullable: false),
                TrackerName = table.Column<string>(type: "TEXT", nullable: false),
                ExternalTrackingId = table.Column<string>(type: "TEXT", nullable: false),
                LastSyncedChapter = table.Column<int>(type: "INTEGER", nullable: false),
                Status = table.Column<string>(type: "TEXT", nullable: false),
                Score = table.Column<int>(type: "INTEGER", nullable: false),
                LastSyncedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_MangaTrackingSyncs", x => x.Id));

        migrationBuilder.CreateIndex(name: "IX_MangaTitles_SourceId_ExternalId", table: "MangaTitles", columns: new[] { "SourceId", "ExternalId" }, unique: true);
        migrationBuilder.CreateIndex(name: "IX_MangaTitles_LibraryId_NormalizedTitle", table: "MangaTitles", columns: new[] { "LibraryId", "NormalizedTitle" });
        migrationBuilder.CreateIndex(name: "IX_MangaChapters_MangaId_SourceChapterId", table: "MangaChapters", columns: new[] { "MangaId", "SourceChapterId" }, unique: true);
        migrationBuilder.CreateIndex(name: "IX_MangaBookmarks_ChapterId_Page", table: "MangaBookmarks", columns: new[] { "ChapterId", "Page" }, unique: true);
        migrationBuilder.CreateIndex(name: "IX_MangaDownloads_ChapterId_State", table: "MangaDownloads", columns: new[] { "ChapterId", "State" });
        migrationBuilder.CreateIndex(name: "IX_MangaNotifications_MangaId", table: "MangaNotifications", column: "MangaId");
        migrationBuilder.CreateIndex(name: "IX_MangaNotifications_Read", table: "MangaNotifications", column: "Read");
        migrationBuilder.CreateIndex(name: "IX_MangaTrackingSyncs_MangaId_TrackerName", table: "MangaTrackingSyncs", columns: new[] { "MangaId", "TrackerName" }, unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "MangaTrackingSyncs");
        migrationBuilder.DropTable(name: "MangaNotifications");
        migrationBuilder.DropTable(name: "MangaDownloads");
        migrationBuilder.DropTable(name: "MangaBookmarks");
        migrationBuilder.DropTable(name: "MangaChapters");
        migrationBuilder.DropTable(name: "MangaTitles");
        migrationBuilder.DropTable(name: "MangaPlugins");
    }
}
