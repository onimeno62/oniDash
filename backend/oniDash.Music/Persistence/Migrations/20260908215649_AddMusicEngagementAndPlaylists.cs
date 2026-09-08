using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace oniDash.Music.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMusicEngagementAndPlaylists : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MusicFavorites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EntityType = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    EntityId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MusicFavorites", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MusicPlayHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TrackId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    PlayedSeconds = table.Column<double>(type: "REAL", nullable: false),
                    CompletionRatio = table.Column<double>(type: "REAL", nullable: false),
                    Source = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MusicPlayHistory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MusicPlaylists",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    IsSmart = table.Column<bool>(type: "INTEGER", nullable: false),
                    SmartQuery = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MusicPlaylists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MusicPlaylistItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PlaylistId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TrackId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Position = table.Column<int>(type: "INTEGER", nullable: false),
                    AddedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MusicPlaylistItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MusicPlaylistItems_MusicPlaylists_PlaylistId",
                        column: x => x.PlaylistId,
                        principalTable: "MusicPlaylists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MusicPlaylistItems_Tracks_TrackId",
                        column: x => x.TrackId,
                        principalTable: "Tracks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MusicFavorites_EntityType_EntityId",
                table: "MusicFavorites",
                columns: new[] { "EntityType", "EntityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MusicPlayHistory_TrackId_StartedAtUtc",
                table: "MusicPlayHistory",
                columns: new[] { "TrackId", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_MusicPlaylistItems_PlaylistId_Position",
                table: "MusicPlaylistItems",
                columns: new[] { "PlaylistId", "Position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MusicPlaylistItems_PlaylistId_TrackId",
                table: "MusicPlaylistItems",
                columns: new[] { "PlaylistId", "TrackId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MusicPlaylistItems_TrackId",
                table: "MusicPlaylistItems",
                column: "TrackId");

            migrationBuilder.CreateIndex(
                name: "IX_MusicPlaylists_Name",
                table: "MusicPlaylists",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MusicFavorites");

            migrationBuilder.DropTable(
                name: "MusicPlayHistory");

            migrationBuilder.DropTable(
                name: "MusicPlaylistItems");

            migrationBuilder.DropTable(
                name: "MusicPlaylists");
        }
    }
}
