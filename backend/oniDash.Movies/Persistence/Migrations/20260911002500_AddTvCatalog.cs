using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace oniDash.Movies.Persistence.Migrations;

[DbContext(typeof(MoviesDbContext))]
[Migration("20260911002500_AddTvCatalog")]
public sealed class AddTvCatalog : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "TvSeries",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                LibraryId = table.Column<Guid>(type: "TEXT", nullable: false),
                Title = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                NormalizedTitle = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                Year = table.Column<int>(type: "INTEGER", nullable: true),
                IsAnime = table.Column<bool>(type: "INTEGER", nullable: false),
                UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_TvSeries", x => x.Id));

        migrationBuilder.CreateTable(
            name: "TvSeasons",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                SeriesId = table.Column<Guid>(type: "TEXT", nullable: false),
                Number = table.Column<int>(type: "INTEGER", nullable: false),
                Title = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TvSeasons", x => x.Id);
                table.ForeignKey("FK_TvSeasons_TvSeries_SeriesId", x => x.SeriesId, "TvSeries", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "TvEpisodes",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                SeasonId = table.Column<Guid>(type: "TEXT", nullable: false),
                MediaItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                FileId = table.Column<Guid>(type: "TEXT", nullable: false),
                Number = table.Column<int>(type: "INTEGER", nullable: false),
                Title = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                DurationSeconds = table.Column<double>(type: "REAL", nullable: true),
                WatchProgressSeconds = table.Column<double>(type: "REAL", nullable: true),
                Watched = table.Column<bool>(type: "INTEGER", nullable: false),
                WatchedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TvEpisodes", x => x.Id);
                table.ForeignKey("FK_TvEpisodes_MediaItems_MediaItemId", x => x.MediaItemId, "MediaItems", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_TvEpisodes_TvSeasons_SeasonId", x => x.SeasonId, "TvSeasons", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_TvSeries_LibraryId_NormalizedTitle_Year", "TvSeries", new[] { "LibraryId", "NormalizedTitle", "Year" }, unique: true);
        migrationBuilder.CreateIndex("IX_TvSeasons_SeriesId_Number", "TvSeasons", new[] { "SeriesId", "Number" }, unique: true);
        migrationBuilder.CreateIndex("IX_TvEpisodes_MediaItemId", "TvEpisodes", "MediaItemId", unique: true);
        migrationBuilder.CreateIndex("IX_TvEpisodes_SeasonId_Number", "TvEpisodes", new[] { "SeasonId", "Number" }, unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("TvEpisodes");
        migrationBuilder.DropTable("TvSeasons");
        migrationBuilder.DropTable("TvSeries");
    }
}
