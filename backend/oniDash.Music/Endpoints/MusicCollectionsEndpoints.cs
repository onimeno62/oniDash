using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using oniDash.Music.Persistence;

namespace oniDash.Music.Endpoints;

/// <summary>Bounded server-side collections used by Music Home and library collection views.</summary>
public static class MusicCollectionsEndpoints
{
    private const int MaxItems = 24;

    public static IEndpointRouteBuilder MapMusicCollectionsEndpoints(this IEndpointRouteBuilder app)
    {
        var music = app.MapGroup("/api/music").WithTags("Music collections");

        music.MapGet("/collections/home", async (MusicDbContext db, Guid? libraryId, int? limit, CancellationToken ct) =>
        {
            var take = Math.Clamp(limit ?? 12, 1, MaxItems);
            var libraryParameter = new SqliteParameter("@libraryId", libraryId.HasValue ? libraryId.Value : DBNull.Value);
            var takeParameter = new SqliteParameter("@take", take);
            var recentlyAddedIds = await db.Database.SqlQueryRaw<Guid>(
                "SELECT Id FROM Tracks WHERE (@libraryId IS NULL OR LibraryId = @libraryId) AND IsMissing = 0 ORDER BY AddedAtUtc DESC, Id LIMIT @take",
                libraryParameter, takeParameter).ToListAsync(ct);
            var recentlyAdded = await TrackQuery(db, recentlyAddedIds, ct);

            var recentHistoryIds = await db.Database.SqlQueryRaw<Guid>(
                "SELECT TrackId FROM MusicPlayHistory WHERE (@libraryId IS NULL OR TrackId IN (SELECT Id FROM Tracks WHERE LibraryId = @libraryId)) GROUP BY TrackId ORDER BY MAX(StartedAtUtc) DESC, TrackId LIMIT @take",
                libraryParameter, takeParameter).ToListAsync(ct);
            var recentlyPlayed = await TrackQuery(db, recentHistoryIds, ct);

            var tracks = db.Tracks.AsNoTracking();
            if (libraryId is not null) tracks = tracks.Where(t => t.LibraryId == libraryId);
            var mostPlayedIds = await db.PlayHistory
                .Where(h => libraryId == null || db.Tracks.Any(t => t.Id == h.TrackId && t.LibraryId == libraryId))
                .GroupBy(h => h.TrackId)
                .Select(g => new { TrackId = g.Key, Plays = g.Count(), Seconds = g.Sum(x => x.PlayedSeconds) })
                .OrderByDescending(x => x.Plays).ThenByDescending(x => x.Seconds).ThenBy(x => x.TrackId)
                .Take(take).Select(x => x.TrackId).ToListAsync(ct);
            var mostPlayed = await TrackQuery(db, mostPlayedIds, ct);

            var topRatedIds = await tracks.Where(t => t.Rating > 0).OrderByDescending(t => t.Rating).ThenBy(t => t.Title).Take(take).Select(t => t.Id).ToListAsync(ct);
            var topRated = await TrackQuery(db, topRatedIds, ct);

            var favoriteIds = await db.Favorites.Where(f => f.EntityType == "track" && (libraryId == null || db.Tracks.Any(t => t.Id == f.EntityId && t.LibraryId == libraryId)))
                .OrderByDescending(f => f.CreatedAtUtc).Take(take).Select(f => f.EntityId).ToListAsync(ct);
            var favorites = await TrackQuery(db, favoriteIds, ct);

            var playedIds = db.PlayHistory.Select(h => h.TrackId).Distinct();
            var neverPlayedIds = await tracks.Where(t => !playedIds.Contains(t.Id)).OrderByDescending(t => t.AddedAtUtc).Take(take).Select(t => t.Id).ToListAsync(ct);
            var neverPlayed = await TrackQuery(db, neverPlayedIds, ct);

            var continueItems = await db.PlaybackStates.AsNoTracking()
                .Where(s => s.PositionSeconds > 0 && !s.Completed && s.Track != null && !s.Track.IsMissing && (libraryId == null || s.Track.LibraryId == libraryId))
                .OrderByDescending(s => s.UpdatedAtUtc).Take(take)
                .Select(s => new ContinueItem(s.TrackId, s.PositionSeconds, s.Track!.DurationSeconds ?? 0, s.UpdatedAtUtc))
                .ToListAsync(ct);

            return Results.Ok(new HomeCollectionResponse(continueItems, recentlyPlayed, recentlyAdded, mostPlayed, favorites, topRated, neverPlayed));
        });

        music.MapPost("/playlists/{playlistId:guid}/save-queue", async (MusicDbContext db, Guid playlistId, SaveQueueRequest request, CancellationToken ct) =>
        {
            if (request.TrackIds.Count > 500) return Results.BadRequest(new { error = "A queue can contain at most 500 tracks." });
            var playlist = await db.Playlists.SingleOrDefaultAsync(p => p.Id == playlistId, ct);
            if (playlist is null) return Results.NotFound();
            if (playlist.IsSmart) return Results.Conflict(new { error = "Smart playlists cannot contain manual queue items." });
            var validIds = await db.Tracks.Where(t => request.TrackIds.Contains(t.Id) && !t.IsMissing).Select(t => t.Id).ToListAsync(ct);
            var existing = await db.PlaylistItems.Where(i => i.PlaylistId == playlistId).Select(i => i.TrackId).ToListAsync(ct);
            var position = await db.PlaylistItems.Where(i => i.PlaylistId == playlistId).Select(i => (int?)i.Position).MaxAsync(ct) ?? -1;
            var toAdd = request.TrackIds.Where(validIds.Contains).Where(id => !existing.Contains(id)).Distinct().ToList();
            foreach (var id in toAdd)
                db.PlaylistItems.Add(new Domain.MusicPlaylistItem { Id = Guid.NewGuid(), PlaylistId = playlistId, TrackId = id, Position = ++position, AddedAtUtc = DateTimeOffset.UtcNow });
            playlist.UpdatedAtUtc = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { added = toAdd.Count });
        });

        return app;
    }

    private static async Task<IReadOnlyList<TrackItem>> TrackQuery(MusicDbContext db, IReadOnlyList<Guid> ids, CancellationToken ct)
    {
        if (ids.Count == 0) return Array.Empty<TrackItem>();
        var rows = await db.Tracks.AsNoTracking().Where(t => ids.Contains(t.Id))
            .Select(t => new TrackItem(t.Id, t.MediaItemId, t.Title, t.ArtistName, t.Album != null ? t.Album.Title : "", t.AlbumId, t.Album != null && t.Album.CoverBlob != null, t.TrackNumber, t.DiscNumber, t.Year, t.DurationSeconds, t.Genre, t.Rating))
            .ToListAsync(ct);
        var order = ids.Select((id, index) => (id, index)).ToDictionary(x => x.id, x => x.index);
        return rows.OrderBy(x => order[x.Id]).ToList();
    }

    public sealed record TrackItem(Guid Id, Guid MediaItemId, string Title, string? ArtistName, string AlbumTitle, Guid? AlbumId, bool HasCover, int? TrackNumber, int? DiscNumber, int? Year, double? DurationSeconds, string? Genre, int Rating);
    public sealed record ContinueItem(Guid TrackId, double PositionSeconds, double DurationSeconds, DateTimeOffset UpdatedAtUtc);
    public sealed record HomeCollectionResponse(IReadOnlyList<ContinueItem> ContinueListening, IReadOnlyList<TrackItem> RecentlyPlayed, IReadOnlyList<TrackItem> RecentlyAdded, IReadOnlyList<TrackItem> MostPlayed, IReadOnlyList<TrackItem> Favorites, IReadOnlyList<TrackItem> TopRated, IReadOnlyList<TrackItem> NeverPlayed);
    public sealed record SaveQueueRequest(IReadOnlyList<Guid> TrackIds);
}
