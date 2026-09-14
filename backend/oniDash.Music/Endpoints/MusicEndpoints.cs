using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using oniDash.Music.Cataloging;
using oniDash.Music.Contracts;
using oniDash.Music.Persistence;

namespace oniDash.Music.Endpoints;

/// <summary>Server-side music browsing/search surface. Queries stay paged and deterministic.</summary>
public static class MusicEndpoints
{
    private const int DefaultLimit = 50;
    private const int MaxLimit = 200;

    public static IEndpointRouteBuilder MapMusicEndpoints(this IEndpointRouteBuilder app)
    {
        var music = app.MapGroup("/api/music").WithTags("Music");

        music.MapGet("/artists", async (MusicDbContext db, Guid? libraryId, string? q, string? sort, bool? desc, int? limit, int? offset, CancellationToken ct) =>
        {
            var (take, skip) = Page(limit, offset);
            var query = db.Artists.AsNoTracking();
            if (libraryId is not null) query = query.Where(a => a.LibraryId == libraryId);
            if (!string.IsNullOrWhiteSpace(q)) query = query.Where(a => EF.Functions.Like(a.Name, $"%{q.Trim()}%"));
            var total = await query.LongCountAsync(ct);
            var ordered = sort?.ToLowerInvariant() == "id" ? query.OrderBy(a => a.Id) : query.OrderBy(a => a.Name);
            if (desc == true) ordered = sort?.ToLowerInvariant() == "id" ? query.OrderByDescending(a => a.Id) : query.OrderByDescending(a => a.Name);
            var items = await ordered.ThenBy(a => a.Id).Skip(skip).Take(take)
                .Select(a => new MusicArtistDto(a.Id, a.LibraryId, a.Name, db.Tracks.Count(t => t.ArtistId == a.Id || t.AlbumArtistId == a.Id), db.Albums.Count(al => al.ArtistId == a.Id)))
                .ToListAsync(ct);
            return Results.Ok(new MusicPage<MusicArtistDto>(items, skip, take, total));
        });

        music.MapGet("/albums", async (MusicDbContext db, Guid? libraryId, Guid? artistId, string? q, int? year, bool? missing, string? sort, bool? desc, int? limit, int? offset, CancellationToken ct) =>
        {
            var (take, skip) = Page(limit, offset);
            var query = db.Albums.AsNoTracking();
            if (libraryId is not null) query = query.Where(a => a.LibraryId == libraryId);
            if (artistId is not null) query = query.Where(a => a.ArtistId == artistId);
            if (year is not null) query = query.Where(a => a.Year == year);
            if (!string.IsNullOrWhiteSpace(q)) query = query.Where(a => EF.Functions.Like(a.Title, $"%{q.Trim()}%") || (a.ArtistName != null && EF.Functions.Like(a.ArtistName, $"%{q.Trim()}%")));
            if (missing == true) query = query.Where(a => !db.Tracks.Any(t => t.AlbumId == a.Id && !t.IsMissing));
            var total = await query.LongCountAsync(ct);
            var key = sort?.ToLowerInvariant() ?? "title";
            var ordered = key == "year" ? query.OrderBy(a => a.Year).ThenBy(a => a.Title) : key == "artist" ? query.OrderBy(a => a.ArtistName).ThenBy(a => a.Title) : query.OrderBy(a => a.Title);
            if (desc == true) ordered = key == "year" ? query.OrderByDescending(a => a.Year).ThenByDescending(a => a.Title) : key == "artist" ? query.OrderByDescending(a => a.ArtistName).ThenByDescending(a => a.Title) : query.OrderByDescending(a => a.Title);
            var items = await ordered.ThenBy(a => a.Id).Skip(skip).Take(take)
                .Select(a => new MusicAlbumDto(a.Id, a.LibraryId, a.Title, a.ArtistName, a.Year, a.CoverBlob != null, db.Tracks.Count(t => t.AlbumId == a.Id && !t.IsMissing)))
                .ToListAsync(ct);
            return Results.Ok(new MusicPage<MusicAlbumDto>(items, skip, take, total));
        });

        music.MapGet("/tracks", async (MusicDbContext db, Guid? libraryId, Guid? albumId, Guid? artistId, string? q, string? genre, bool? missing, bool? favorite, int? ratingMin, string? sort, bool? desc, int? limit, int? offset, CancellationToken ct) =>
        {
            var (take, skip) = Page(limit, offset);
            var query = db.Tracks.AsNoTracking();
            if (libraryId is not null) query = query.Where(t => t.LibraryId == libraryId);
            if (albumId is not null) query = query.Where(t => t.AlbumId == albumId);
            if (artistId is not null) query = query.Where(t => t.ArtistId == artistId || t.AlbumArtistId == artistId);
            if (!string.IsNullOrWhiteSpace(q)) query = query.Where(t => EF.Functions.Like(t.Title, $"%{q.Trim()}%") || (t.ArtistName != null && EF.Functions.Like(t.ArtistName, $"%{q.Trim()}%")) || (t.AlbumArtistName != null && EF.Functions.Like(t.AlbumArtistName, $"%{q.Trim()}%")));
            if (!string.IsNullOrWhiteSpace(genre)) query = query.Where(t => t.Genre != null && EF.Functions.Like(t.Genre, genre.Trim()));
            if (missing is not null) query = query.Where(t => t.IsMissing == missing);
            if (favorite == true) query = query.Where(t => db.Favorites.Any(f => f.EntityType == "track" && f.EntityId == t.Id));
            if (ratingMin is not null) query = query.Where(t => t.Rating >= ratingMin);
            var total = await query.LongCountAsync(ct);
            var key = sort?.ToLowerInvariant() ?? "title";
            var ordered = key switch
            {
                "added" => query.OrderBy(t => t.AddedAtUtc), "updated" => query.OrderBy(t => t.UpdatedAtUtc), "rating" => query.OrderBy(t => t.Rating),
                "year" => query.OrderBy(t => t.Year), "duration" => query.OrderBy(t => t.DurationSeconds), "artist" => query.OrderBy(t => t.ArtistName),
                "album" => query.OrderBy(t => t.AlbumArtistName).ThenBy(t => t.AlbumId), _ => query.OrderBy(t => t.Title)
            };
            if (desc == true)
            {
                ordered = key switch
                {
                    "added" => query.OrderByDescending(t => t.AddedAtUtc), "updated" => query.OrderByDescending(t => t.UpdatedAtUtc), "rating" => query.OrderByDescending(t => t.Rating),
                    "year" => query.OrderByDescending(t => t.Year), "duration" => query.OrderByDescending(t => t.DurationSeconds), "artist" => query.OrderByDescending(t => t.ArtistName),
                    "album" => query.OrderByDescending(t => t.AlbumArtistName).ThenByDescending(t => t.AlbumId), _ => query.OrderByDescending(t => t.Title)
                };
            }
            var items = await ordered.ThenBy(t => t.Id).Skip(skip).Take(take)
                .Select(t => new MusicTrackDto(t.Id, t.LibraryId, t.MediaItemId, t.FileId, t.Title, t.ArtistName, t.AlbumArtistName,
                    t.Album != null ? t.Album.Title : null, t.AlbumId, t.TrackNumber, t.DiscNumber, t.Year, t.DurationSeconds, t.Genre, t.Rating,
                    db.Favorites.Any(f => f.EntityType == "track" && f.EntityId == t.Id), t.IsMissing, t.AddedAtUtc, t.UpdatedAtUtc))
                .ToListAsync(ct);
            return Results.Ok(new MusicPage<MusicTrackDto>(items, skip, take, total));
        });

        music.MapGet("/genres", async (MusicDbContext db, Guid? libraryId, int? limit, int? offset, CancellationToken ct) =>
        {
            var (take, skip) = Page(limit, offset);
            var query = db.Tracks.AsNoTracking().Where(t => t.Genre != null && t.Genre != "");
            if (libraryId is not null) query = query.Where(t => t.LibraryId == libraryId);
            var items = await query.GroupBy(t => t.Genre!).Select(g => new { Genre = g.Key, TrackCount = g.Count() }).OrderBy(g => g.Genre).Skip(skip).Take(take).ToListAsync(ct);
            return Results.Ok(items);
        });

        music.MapGet("/tracks/{trackId:guid}", async (MusicDbContext db, Guid trackId, CancellationToken ct) =>
        {
            var item = await db.Tracks.AsNoTracking().Where(t => t.Id == trackId)
                .Select(t => new MusicTrackDto(t.Id, t.LibraryId, t.MediaItemId, t.FileId, t.Title, t.ArtistName, t.AlbumArtistName,
                    t.Album != null ? t.Album.Title : null, t.AlbumId, t.TrackNumber, t.DiscNumber, t.Year, t.DurationSeconds, t.Genre, t.Rating,
                    db.Favorites.Any(f => f.EntityType == "track" && f.EntityId == t.Id), t.IsMissing, t.AddedAtUtc, t.UpdatedAtUtc))
                .SingleOrDefaultAsync(ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        });

        music.MapGet("/albums/{albumId:guid}/cover", async (MusicDbContext db, Guid albumId, CancellationToken ct) =>
        {
            var album = await db.Albums.AsNoTracking().SingleOrDefaultAsync(a => a.Id == albumId, ct);
            if (album?.CoverBlob is not { Length: > 0 } blob) return Results.NotFound();
            return Results.File(blob, album.CoverContentType ?? "image/jpeg");
        });

        music.MapGet("/tracks/{trackId:guid}/stream", async (MusicDbContext db, IMediaFileLocator locator, Guid trackId, CancellationToken ct) =>
        {
            var track = await db.Tracks.AsNoTracking().SingleOrDefaultAsync(t => t.Id == trackId, ct);
            if (track is null || track.IsMissing) return Results.NotFound();
            var location = await locator.LocateAsync(track.FileId, ct);
            return location is null ? Results.NotFound() : Results.File(location.AbsolutePath, location.ContentType, enableRangeProcessing: true);
        });

        music.MapPost("/reindex", async (MusicReindexService reindex, Guid? libraryId, CancellationToken ct) => Results.Ok(new { indexedTracks = await reindex.ReindexAsync(libraryId, ct) }));
        return app;
    }

    private static (int Take, int Skip) Page(int? limit, int? offset) => (Math.Clamp(limit ?? DefaultLimit, 1, MaxLimit), Math.Max(0, offset ?? 0));
}
