using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace oniDash.Movies.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialMovieCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // NOTE: the "MediaItems" table is owned by the core context and already exists
            // in the shared database; Movies reference it by FK without (re)creating it.

            migrationBuilder.CreateTable(
                name: "Movies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    LibraryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MediaItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    NormalizedTitle = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    Year = table.Column<int>(type: "INTEGER", nullable: true),
                    DurationSeconds = table.Column<double>(type: "REAL", nullable: true),
                    Container = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    PosterBlob = table.Column<byte[]>(type: "BLOB", nullable: true),
                    PosterContentType = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    WatchProgressSeconds = table.Column<double>(type: "REAL", nullable: true),
                    Watched = table.Column<bool>(type: "INTEGER", nullable: false),
                    WatchedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Movies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Movies_MediaItems_MediaItemId",
                        column: x => x.MediaItemId,
                        principalTable: "MediaItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Movies_LibraryId_NormalizedTitle",
                table: "Movies",
                columns: new[] { "LibraryId", "NormalizedTitle" });

            migrationBuilder.CreateIndex(
                name: "IX_Movies_MediaItemId",
                table: "Movies",
                column: "MediaItemId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Movies");
        }
    }
}
