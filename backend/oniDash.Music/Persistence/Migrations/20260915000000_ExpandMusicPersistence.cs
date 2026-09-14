using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace oniDash.Music.Persistence.Migrations;

[DbContext(typeof(MusicDbContext))]
[Migration("20260915000000_ExpandMusicPersistence")]
public sealed class ExpandMusicPersistence : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>("AlbumArtistName", "Tracks", "TEXT", maxLength: 512, nullable: true);
        migrationBuilder.AddColumn<Guid>("AlbumArtistId", "Tracks", "TEXT", nullable: true);
        migrationBuilder.AddColumn<bool>("IsMissing", "Tracks", "INTEGER", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<DateTimeOffset>("AddedAtUtc", "Tracks", "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP");

        migrationBuilder.CreateTable(
            name: "MusicPlaybackStates",
            columns: table => new
            {
                Id = table.Column<Guid>("TEXT", nullable: false),
                TrackId = table.Column<Guid>("TEXT", nullable: false),
                PositionSeconds = table.Column<double>("REAL", nullable: false),
                Completed = table.Column<bool>("INTEGER", nullable: false),
                UpdatedAtUtc = table.Column<DateTimeOffset>("TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MusicPlaybackStates", x => x.Id);
                table.ForeignKey("FK_MusicPlaybackStates_Tracks_TrackId", x => x.TrackId, "Tracks", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "MusicLyrics",
            columns: table => new
            {
                Id = table.Column<Guid>("TEXT", nullable: false),
                TrackId = table.Column<Guid>("TEXT", nullable: false),
                Text = table.Column<string>("TEXT", nullable: false),
                Kind = table.Column<int>("INTEGER", nullable: false),
                Source = table.Column<string>("TEXT", maxLength: 128, nullable: false),
                IsLocalEdit = table.Column<bool>("INTEGER", nullable: false),
                UpdatedAtUtc = table.Column<DateTimeOffset>("TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MusicLyrics", x => x.Id);
                table.ForeignKey("FK_MusicLyrics_Tracks_TrackId", x => x.TrackId, "Tracks", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "MusicArtwork",
            columns: table => new
            {
                Id = table.Column<Guid>("TEXT", nullable: false),
                TrackId = table.Column<Guid>("TEXT", nullable: true),
                AlbumId = table.Column<Guid>("TEXT", nullable: true),
                Source = table.Column<int>("INTEGER", nullable: false),
                Provider = table.Column<string>("TEXT", maxLength: 128, nullable: true),
                ContentType = table.Column<string>("TEXT", maxLength: 128, nullable: false),
                Data = table.Column<byte[]>("BLOB", nullable: false),
                UpdatedAtUtc = table.Column<DateTimeOffset>("TEXT", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_MusicArtwork", x => x.Id));

        migrationBuilder.CreateIndex("IX_Tracks_LibraryId_IsMissing", "Tracks", new[] { "LibraryId", "IsMissing" });
        migrationBuilder.CreateIndex("IX_Tracks_LibraryId_AddedAtUtc", "Tracks", new[] { "LibraryId", "AddedAtUtc" });
        migrationBuilder.CreateIndex("IX_Tracks_AlbumArtistId", "Tracks", "AlbumArtistId");
        migrationBuilder.CreateIndex("IX_MusicPlaybackStates_TrackId", "MusicPlaybackStates", "TrackId", unique: true);
        migrationBuilder.CreateIndex("IX_MusicLyrics_TrackId_Kind_Source", "MusicLyrics", new[] { "TrackId", "Kind", "Source" }, unique: true);
        migrationBuilder.CreateIndex("IX_MusicArtwork_AlbumId_Source_Provider", "MusicArtwork", new[] { "AlbumId", "Source", "Provider" });
        migrationBuilder.CreateIndex("IX_MusicArtwork_TrackId_Source_Provider", "MusicArtwork", new[] { "TrackId", "Source", "Provider" });
        migrationBuilder.CreateIndex("IX_MusicPlayHistory_StartedAtUtc", "MusicPlayHistory", "StartedAtUtc");

        migrationBuilder.AddForeignKey("FK_Tracks_Artists_AlbumArtistId", "Tracks", "AlbumArtistId", "Artists", "Id", onDelete: ReferentialAction.SetNull);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey("FK_Tracks_Artists_AlbumArtistId", "Tracks");
        migrationBuilder.DropTable("MusicArtwork");
        migrationBuilder.DropTable("MusicLyrics");
        migrationBuilder.DropTable("MusicPlaybackStates");
        migrationBuilder.DropIndex("IX_Tracks_LibraryId_IsMissing", "Tracks");
        migrationBuilder.DropIndex("IX_Tracks_LibraryId_AddedAtUtc", "Tracks");
        migrationBuilder.DropIndex("IX_Tracks_AlbumArtistId", "Tracks");
        migrationBuilder.DropIndex("IX_MusicPlayHistory_StartedAtUtc", "MusicPlayHistory");
        migrationBuilder.DropColumn("AlbumArtistName", "Tracks");
        migrationBuilder.DropColumn("AlbumArtistId", "Tracks");
        migrationBuilder.DropColumn("IsMissing", "Tracks");
        migrationBuilder.DropColumn("AddedAtUtc", "Tracks");
    }
}
