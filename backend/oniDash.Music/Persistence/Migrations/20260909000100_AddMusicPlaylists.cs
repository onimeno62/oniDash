using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using oniDash.Music.Persistence;

#nullable disable

namespace oniDash.Music.Persistence.Migrations;

[DbContext(typeof(MusicDbContext))]
[Migration("20260909000100_AddMusicPlaylists")]
public partial class AddMusicPlaylists : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(name: "MusicPlaylists", columns: table => new { Id = table.Column<Guid>(type: "TEXT", nullable: false), Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false), Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true), IsSmart = table.Column<bool>(type: "INTEGER", nullable: false), SmartQuery = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true), CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false), UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false) }, constraints: table => table.PrimaryKey("PK_MusicPlaylists", x => x.Id));
        migrationBuilder.CreateTable(name: "MusicPlaylistItems", columns: table => new { Id = table.Column<Guid>(type: "TEXT", nullable: false), PlaylistId = table.Column<Guid>(type: "TEXT", nullable: false), TrackId = table.Column<Guid>(type: "TEXT", nullable: false), Position = table.Column<int>(type: "INTEGER", nullable: false), AddedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false) }, constraints: table => { table.PrimaryKey("PK_MusicPlaylistItems", x => x.Id); table.ForeignKey("FK_MusicPlaylistItems_MusicPlaylists_PlaylistId", x => x.PlaylistId, "MusicPlaylists", "Id", onDelete: ReferentialAction.Cascade); table.ForeignKey("FK_MusicPlaylistItems_Tracks_TrackId", x => x.TrackId, "Tracks", "Id", onDelete: ReferentialAction.Cascade); });
        migrationBuilder.CreateIndex(name: "IX_MusicPlaylists_Name", table: "MusicPlaylists", column: "Name", unique: true);
        migrationBuilder.CreateIndex(name: "IX_MusicPlaylistItems_PlaylistId_Position", table: "MusicPlaylistItems", columns: new[] { "PlaylistId", "Position" }, unique: true);
        migrationBuilder.CreateIndex(name: "IX_MusicPlaylistItems_PlaylistId_TrackId", table: "MusicPlaylistItems", columns: new[] { "PlaylistId", "TrackId" }, unique: true);
        migrationBuilder.CreateIndex(name: "IX_MusicPlaylistItems_TrackId", table: "MusicPlaylistItems", column: "TrackId");
    }
    protected override void Down(MigrationBuilder migrationBuilder) { migrationBuilder.DropTable(name: "MusicPlaylistItems"); migrationBuilder.DropTable(name: "MusicPlaylists"); }
}
