using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using oniDash.Music.Persistence;

namespace oniDash.Music.Endpoints;

public static class MusicInsightsEndpoints
{
    public static IEndpointRouteBuilder MapMusicInsightsEndpoints(this IEndpointRouteBuilder app)
    {
        var music = app.MapGroup("/api/music").WithTags("Music insights");
        music.MapGet("/statistics/overview", async (MusicDbContext db, CancellationToken ct) => Results.Ok(new
        {
            tracks = await db.Tracks.CountAsync(ct),
            albums = await db.Albums.CountAsync(ct),
            artists = await db.Artists.CountAsync(ct),
            playlists = await db.Playlists.CountAsync(ct),
            favorites = await db.Favorites.CountAsync(ct),
            listeningSeconds = await db.PlayHistory.SumAsync(h => (double?)h.PlayedSeconds, ct) ?? 0,
        }));
        music.MapGet("/statistics/top-tracks", async (MusicDbContext db, int? limit, CancellationToken ct) => Results.Ok(await db.PlayHistory.GroupBy(h => h.TrackId).Select(g => new { trackId = g.Key, plays = g.Count(), playedSeconds = g.Sum(h => h.PlayedSeconds) }).OrderByDescending(x => x.plays).ThenBy(x => x.trackId).Take(Math.Clamp(limit ?? 20, 1, 100)).ToListAsync(ct)));
        music.MapGet("/statistics/genres", async (MusicDbContext db, CancellationToken ct) => Results.Ok(await db.Tracks.Where(t => t.Genre != null && t.Genre != "").GroupBy(t => t.Genre!).Select(g => new { genre = g.Key, tracks = g.Count() }).OrderByDescending(x => x.tracks).ThenBy(x => x.genre).ToListAsync(ct)));
        music.MapGet("/health", async (MusicDbContext db, CancellationToken ct) =>
        {
            // SQLite cannot ORDER BY the TEXT timestamp column, so the newest track stamp
            // is taken from a single-column read (same limitation as /history).
            var stamps = await db.Tracks.Select(t => t.UpdatedAtUtc).ToListAsync(ct);
            return Results.Ok(new
            {
                database = await db.Database.CanConnectAsync(ct),
                tracks = await db.Tracks.CountAsync(ct),
                lastUpdatedUtc = stamps.Count > 0 ? stamps.Max() : (DateTimeOffset?)null,
            });
        });
        music.MapGet("/health/duplicates", async (MusicDbContext db, int? limit, CancellationToken ct) =>
        {
            var groups = await db.Tracks.AsNoTracking()
                .GroupBy(track => new { track.LibraryId, track.Title, track.ArtistName, track.AlbumId })
                .Where(group => group.Count() > 1)
                .OrderByDescending(group => group.Count())
                .Take(Math.Clamp(limit ?? 100, 1, 500))
                .Select(group => new
                {
                    libraryId = group.Key.LibraryId,
                    title = group.Key.Title,
                    artistName = group.Key.ArtistName,
                    albumId = group.Key.AlbumId,
                    count = group.Count(),
                    trackIds = group.Select(track => track.Id).ToList(),
                })
                .ToListAsync(ct);
            return Results.Ok(groups);
        });
        return app;
    }
}
