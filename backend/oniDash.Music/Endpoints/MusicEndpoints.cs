using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using oniDash.Music.Cataloging;
using oniDash.Music.Persistence;

namespace oniDash.Music.Endpoints;

/// <summary>
/// Music catalogue browsing and playback: artists, albums (with embedded cover art),
/// tracks, ranged audio streaming from the indexed files, and a reindex command.
/// </summary>
public static class MusicEndpoints
{
    public static IEndpointRouteBuilder MapMusicEndpoints(this IEndpointRouteBuilder app)
    {
        var music = app.MapGroup("/api/music").WithTags("Music");

        music.MapGet("/artists", async (
            MusicDbContext db, Guid? libraryId, CancellationToken ct) =>
        {
            var query = db.Artists.AsNoTracking();
            if (libraryId is not null)
            {
                query = query.Where(a => a.LibraryId == libraryId);
            }

            var artists = await query
                .OrderBy(a => a.Name)
                .Select(a => new ArtistSummary(a.Id, a.Name))
                .ToListAsync(ct);
            return Results.Ok(artists);
        });

        music.MapGet("/albums", async (
            MusicDbContext db, Guid? libraryId, Guid? artistId, CancellationToken ct) =>
        {
            var query = db.Albums.AsNoTracking();
            if (libraryId is not null)
            {
                query = query.Where(a => a.LibraryId == libraryId);
            }
            if (artistId is not null)
            {
                query = query.Where(a => a.ArtistId == artistId);
            }

            var albums = await query
                .OrderBy(a => a.ArtistName).ThenBy(a => a.Year).ThenBy(a => a.Title)
                .Select(a => new AlbumSummary(
                    a.Id, a.Title, a.ArtistName, a.Year, a.CoverBlob != null))
                .ToListAsync(ct);
            return Results.Ok(albums);
        });

        music.MapGet("/tracks", async (
            MusicDbContext db, Guid? libraryId, Guid? albumId, Guid? artistId, CancellationToken ct) =>
        {
            var query = db.Tracks.AsNoTracking();
            if (libraryId is not null)
            {
                query = query.Where(t => t.LibraryId == libraryId);
            }
            if (albumId is not null)
            {
                query = query.Where(t => t.AlbumId == albumId);
            }
            if (artistId is not null)
            {
                query = query.Where(t => t.ArtistId == artistId || t.Album!.ArtistId == artistId);
            }

            var tracks = await query
                .OrderBy(t => t.Album!.ArtistName).ThenBy(t => t.Album!.Year)
                .ThenBy(t => t.Album!.Title).ThenBy(t => t.DiscNumber).ThenBy(t => t.TrackNumber).ThenBy(t => t.Title)
                .Select(t => new TrackSummary(
                    t.Id, t.MediaItemId, t.Title, t.ArtistName, t.Album!.Title, t.AlbumId,
                    t.Album!.CoverBlob != null, t.TrackNumber, t.DiscNumber, t.Year, t.DurationSeconds, t.Genre))
                .ToListAsync(ct);
            return Results.Ok(tracks);
        });

        music.MapGet("/albums/{albumId:guid}/cover", async (
            MusicDbContext db, Guid albumId, CancellationToken ct) =>
        {
            var album = await db.Albums.AsNoTracking()
                .SingleOrDefaultAsync(a => a.Id == albumId, ct);
            if (album?.CoverBlob is not { Length: > 0 } blob)
            {
                return Results.NotFound();
            }

            return Results.File(blob, album.CoverContentType ?? "image/jpeg");
        });

        music.MapGet("/tracks/{trackId:guid}/stream", async (
            MusicDbContext db, IMediaFileLocator locator, Guid trackId, CancellationToken ct) =>
        {
            var track = await db.Tracks.AsNoTracking()
                .SingleOrDefaultAsync(t => t.Id == trackId, ct);
            if (track is null)
            {
                return Results.NotFound();
            }

            var location = await locator.LocateAsync(track.FileId, ct);
            return location is null
                ? Results.NotFound()
                : Results.File(
                    location.AbsolutePath,
                    location.ContentType,
                    enableRangeProcessing: true);
        });

        music.MapPost("/reindex", async (
            MusicReindexService reindex, Guid? libraryId, CancellationToken ct) =>
        {
            var indexed = await reindex.ReindexAsync(libraryId, ct);
            return Results.Ok(new { indexedTracks = indexed });
        });

        return app;
    }

    public sealed record ArtistSummary(Guid Id, string Name);

    public sealed record AlbumSummary(Guid Id, string Title, string? ArtistName, int? Year, bool HasCover);

    public sealed record TrackSummary(
        Guid Id, Guid MediaItemId, string Title, string? ArtistName, string AlbumTitle, Guid? AlbumId,
        bool HasCover, int? TrackNumber, int? DiscNumber, int? Year, double? DurationSeconds, string? Genre);
}
