using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using oniDash.Music.Cataloging;
using oniDash.Music.Tagging;
using oniDash.Music.Persistence;

namespace oniDash.Music.Endpoints;

public static class MusicMetadataEndpoints
{
    public static IEndpointRouteBuilder MapMusicMetadataEndpoints(this IEndpointRouteBuilder app)
    {
        var music = app.MapGroup("/api/music").WithTags("Music metadata");
        music.MapGet("/tracks/{trackId:guid}/metadata", async (MusicDbContext db, IAudioTagReader reader, IMediaFileLocator locator, Guid trackId, CancellationToken ct) =>
        {
            var track = await db.Tracks.AsNoTracking().SingleOrDefaultAsync(t => t.Id == trackId, ct);
            if (track is null) return Results.NotFound();
            var location = await locator.LocateAsync(track.FileId, ct);
            if (location is null) return Results.NotFound();
            var tags = reader.Read(location.AbsolutePath);
            return tags is null ? Results.Problem("The audio file could not be read.", statusCode: 422) : Results.Ok(new MetadataResponse(tags.Title, tags.TrackArtist, tags.AlbumArtist, tags.Album, tags.TrackNumber, tags.DiscNumber, tags.Year, tags.DurationSeconds, tags.Genre));
        });
        music.MapPut("/tracks/{trackId:guid}/metadata", async (MusicDbContext db, MusicCatalogService catalog, IAudioTagWriter writer, IMediaFileLocator locator, Guid trackId, MetadataRequest request, CancellationToken ct) =>
        {
            if (!request.Confirmed) return Results.Conflict(new { error = "Metadata writes require explicit confirmation." });
            var track = await db.Tracks.SingleOrDefaultAsync(t => t.Id == trackId, ct);
            if (track is null) return Results.NotFound();
            var location = await locator.LocateAsync(track.FileId, ct);
            if (location is null) return Results.NotFound();
            var update = new AudioMetadataUpdate(request.Title, request.TrackArtist, request.AlbumArtist, request.Album, request.TrackNumber, request.DiscNumber, request.Year, request.Genre);
            if (!writer.Write(location.AbsolutePath, update, out var error)) return Results.Problem(error ?? "Metadata write failed.", statusCode: 422);
            if (!await catalog.IndexFileAsync(track.LibraryId, track.MediaItemId, track.FileId, location.AbsolutePath, ct)) return Results.Problem("Metadata was written, but the catalogue refresh failed.", statusCode: 503);
            return Results.Accepted();
        });
        return app;
    }
    public sealed record MetadataRequest(string? Title, string? TrackArtist, string? AlbumArtist, string? Album, int? TrackNumber, int? DiscNumber, int? Year, string? Genre, bool Confirmed);
    public sealed record MetadataResponse(string? Title, string? TrackArtist, string? AlbumArtist, string? Album, int? TrackNumber, int? DiscNumber, int? Year, double? DurationSeconds, string? Genre);
}
