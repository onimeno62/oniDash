using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace oniDash.Music.Persistence.Migrations;

public partial class AddMusicTrackRatings : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "Rating",
            table: "Tracks",
            type: "INTEGER",
            nullable: false,
            defaultValue: 0);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "Rating", table: "Tracks");
    }
}
