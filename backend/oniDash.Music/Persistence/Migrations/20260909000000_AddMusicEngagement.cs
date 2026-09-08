using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using oniDash.Music.Persistence;

#nullable disable

namespace oniDash.Music.Persistence.Migrations;

[DbContext(typeof(MusicDbContext))]
[Migration("20260909000000_AddMusicEngagement")]
public partial class AddMusicEngagement : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(name: "MusicFavorites", columns: table => new { Id = table.Column<Guid>(type: "TEXT", nullable: false), EntityType = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false), EntityId = table.Column<Guid>(type: "TEXT", nullable: false), CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false) }, constraints: table => table.PrimaryKey("PK_MusicFavorites", x => x.Id));
        migrationBuilder.CreateTable(name: "MusicPlayHistory", columns: table => new { Id = table.Column<Guid>(type: "TEXT", nullable: false), TrackId = table.Column<Guid>(type: "TEXT", nullable: false), StartedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false), CompletedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true), PlayedSeconds = table.Column<double>(type: "REAL", nullable: false), CompletionRatio = table.Column<double>(type: "REAL", nullable: false), Source = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false) }, constraints: table => table.PrimaryKey("PK_MusicPlayHistory", x => x.Id));
        migrationBuilder.CreateIndex(name: "IX_MusicFavorites_EntityType_EntityId", table: "MusicFavorites", columns: new[] { "EntityType", "EntityId" }, unique: true);
        migrationBuilder.CreateIndex(name: "IX_MusicPlayHistory_TrackId_StartedAtUtc", table: "MusicPlayHistory", columns: new[] { "TrackId", "StartedAtUtc" });
    }
    protected override void Down(MigrationBuilder migrationBuilder) { migrationBuilder.DropTable(name: "MusicPlayHistory"); migrationBuilder.DropTable(name: "MusicFavorites"); }
}
