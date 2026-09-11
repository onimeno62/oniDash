using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace oniDash.Manga.Persistence.Migrations;

[DbContext(typeof(MangaDbContext))]
[Migration("20260911003500_InitialMangaPlatform")]
public sealed class InitialMangaPlatform : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        m.CreateTable("MangaPlugins", t => new { Id = t.Column<string>("TEXT", maxLength: 128, nullable: false), Name = t.Column<string>("TEXT", maxLength: 256, nullable: false), Version = t.Column<string>("TEXT", nullable: false), Installed = t.Column<bool>("INTEGER", nullable: false), Enabled = t.Column<bool>("INTEGER", nullable: false), State = t.Column<string>("TEXT", nullable: false) }, c => c.PrimaryKey("PK_MangaPlugins", x => x.Id));
        m.CreateTable("MangaTitles", t => new { Id = t.Column<Guid>("TEXT", nullable: false), LibraryId = t.Column<Guid>("TEXT", nullable: false), MediaItemId = t.Column<Guid>("TEXT", nullable: true), SourceId = t.Column<string>("TEXT", maxLength: 128, nullable: false), ExternalId = t.Column<string>("TEXT", maxLength: 512, nullable: false), Title = t.Column<string>("TEXT", maxLength: 512, nullable: false), NormalizedTitle = t.Column<string>("TEXT", maxLength: 512, nullable: false), Author = t.Column<string>("TEXT", nullable: true), Description = t.Column<string>("TEXT", nullable: true), Favorite = t.Column<bool>("INTEGER", nullable: false), UpdatedAtUtc = t.Column<DateTimeOffset>("TEXT", nullable: false) }, c => c.PrimaryKey("PK_MangaTitles", x => x.Id));
        m.CreateTable("MangaChapters", t => new { Id = t.Column<Guid>("TEXT", nullable: false), MangaId = t.Column<Guid>("TEXT", nullable: false), SourceChapterId = t.Column<string>("TEXT", maxLength: 512, nullable: false), Title = t.Column<string>("TEXT", maxLength: 512, nullable: false), Number = t.Column<double>("REAL", nullable: false), LocalPath = t.Column<string>("TEXT", nullable: true), PageCount = t.Column<int>("INTEGER", nullable: true), CurrentPage = t.Column<int>("INTEGER", nullable: false), Read = t.Column<bool>("INTEGER", nullable: false), LastReadAtUtc = t.Column<DateTimeOffset>("TEXT", nullable: true), UpdatedAtUtc = t.Column<DateTimeOffset>("TEXT", nullable: false) }, c => { c.PrimaryKey("PK_MangaChapters", x => x.Id); c.ForeignKey("FK_MangaChapters_MangaTitles_MangaId", x => x.MangaId, "MangaTitles", "Id", onDelete: ReferentialAction.Cascade); });
        m.CreateTable("MangaBookmarks", t => new { Id = t.Column<Guid>("TEXT", nullable: false), ChapterId = t.Column<Guid>("TEXT", nullable: false), Page = t.Column<int>("INTEGER", nullable: false), Note = t.Column<string>("TEXT", nullable: true), CreatedAtUtc = t.Column<DateTimeOffset>("TEXT", nullable: false) }, c => { c.PrimaryKey("PK_MangaBookmarks", x => x.Id); c.ForeignKey("FK_MangaBookmarks_MangaChapters_ChapterId", x => x.ChapterId, "MangaChapters", "Id", onDelete: ReferentialAction.Cascade); });
        m.CreateTable("MangaDownloads", t => new { Id = t.Column<Guid>("TEXT", nullable: false), ChapterId = t.Column<Guid>("TEXT", nullable: false), State = t.Column<string>("TEXT", nullable: false), DestinationPath = t.Column<string>("TEXT", nullable: true), Error = t.Column<string>("TEXT", nullable: true), CreatedAtUtc = t.Column<DateTimeOffset>("TEXT", nullable: false), UpdatedAtUtc = t.Column<DateTimeOffset>("TEXT", nullable: false) }, c => { c.PrimaryKey("PK_MangaDownloads", x => x.Id); c.ForeignKey("FK_MangaDownloads_MangaChapters_ChapterId", x => x.ChapterId, "MangaChapters", "Id", onDelete: ReferentialAction.Cascade); });
        m.CreateIndex("IX_MangaTitles_SourceId_ExternalId", "MangaTitles", new[] { "SourceId", "ExternalId" }, unique: true); m.CreateIndex("IX_MangaTitles_LibraryId_NormalizedTitle", "MangaTitles", new[] { "LibraryId", "NormalizedTitle" });
        m.CreateIndex("IX_MangaChapters_MangaId_SourceChapterId", "MangaChapters", new[] { "MangaId", "SourceChapterId" }, unique: true); m.CreateIndex("IX_MangaBookmarks_ChapterId_Page", "MangaBookmarks", new[] { "ChapterId", "Page" }, unique: true); m.CreateIndex("IX_MangaDownloads_ChapterId_State", "MangaDownloads", new[] { "ChapterId", "State" });
    }

    protected override void Down(MigrationBuilder m) { m.DropTable("MangaBookmarks"); m.DropTable("MangaDownloads"); m.DropTable("MangaChapters"); m.DropTable("MangaPlugins"); m.DropTable("MangaTitles"); }
}
