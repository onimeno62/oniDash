using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace oniDash.Music.Persistence.Migrations;

[DbContext(typeof(MusicDbContext))]
[Migration("20260911003000_AddReplayGain")]
public sealed class AddReplayGain : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<double>("ReplayGainTrackGainDb", "Tracks", "REAL", nullable: true);
        migrationBuilder.AddColumn<double>("ReplayGainTrackPeak", "Tracks", "REAL", nullable: true);
        migrationBuilder.AddColumn<double>("ReplayGainAlbumGainDb", "Tracks", "REAL", nullable: true);
        migrationBuilder.AddColumn<double>("ReplayGainAlbumPeak", "Tracks", "REAL", nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn("ReplayGainTrackGainDb", "Tracks");
        migrationBuilder.DropColumn("ReplayGainTrackPeak", "Tracks");
        migrationBuilder.DropColumn("ReplayGainAlbumGainDb", "Tracks");
        migrationBuilder.DropColumn("ReplayGainAlbumPeak", "Tracks");
    }
}
