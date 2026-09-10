using System;
using System.IO;
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

/// <summary>Destructive/file-system operations are explicit and validated server-side.</summary>
public static class MusicActionsEndpoints
{
    private const int MaxLyricsCharacters = 1_000_000;

    public static IEndpointRouteBuilder MapMusicActionsEndpoints(this IEndpointRouteBuilder app)
    {
        var music = app.MapGroup("/api/music").WithTags("Music actions");

        music.MapGet("/tracks/{trackId:guid}/rating", async (MusicDbContext db, Guid trackId, CancellationToken ct) =>
        {
            var rating = await db.Tracks.AsNoTracking().Where(t => t.Id == trackId).Select(t => (int?)t.Rating).SingleOrDefaultAsync(ct);
            return rating is null ? Results.NotFound() : Results.Ok(new { rating });
        });

        music.MapPut("/tracks/{trackId:guid}/rating", async (MusicDbContext db, Guid trackId, RatingRequest request, CancellationToken ct) =>
        {
            if (request.Rating is < 0 or > 5) return Results.BadRequest(new { error = "Rating must be between 0 and 5." });
            var track = await db.Tracks.SingleOrDefaultAsync(t => t.Id == trackId, ct);
            if (track is null) return Results.NotFound();
            track.Rating = request.Rating;
            track.UpdatedAtUtc = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { rating = track.Rating });
        });

        music.MapPost("/tracks/{trackId:guid}/lyrics", async (MusicDbContext db, IMediaFileLocator locator, Guid trackId, LyricsRequest request, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Text)) return Results.BadRequest(new { error = "Lyrics cannot be empty." });
            if (request.Text.Length > MaxLyricsCharacters) return Results.BadRequest(new { error = $"Lyrics cannot exceed {MaxLyricsCharacters} characters." });
            var track = await db.Tracks.AsNoTracking().SingleOrDefaultAsync(t => t.Id == trackId, ct);
            if (track is null) return Results.NotFound();
            var location = await locator.LocateAsync(track.FileId, ct);
            if (location is null) return Results.NotFound();
            var path = Path.ChangeExtension(location.AbsolutePath, ".lrc");
            try
            {
                await File.WriteAllTextAsync(path, request.Text.Replace("\r\n", "\n"), ct);
            }
            catch (UnauthorizedAccessException) { return Results.Problem("Lyrics file is not writable.", statusCode: StatusCodes.Status403Forbidden); }
            catch (IOException) { return Results.Problem("Lyrics file could not be written.", statusCode: StatusCodes.Status503ServiceUnavailable); }
            return Results.Ok(new { path, synchronized = request.Synchronized });
        });

        music.MapGet("/tracks/{trackId:guid}/lyrics", async (MusicDbContext db, IMediaFileLocator locator, Guid trackId, CancellationToken ct) =>
        {
            var track = await db.Tracks.AsNoTracking().SingleOrDefaultAsync(t => t.Id == trackId, ct);
            if (track is null) return Results.NotFound();
            var location = await locator.LocateAsync(track.FileId, ct);
            if (location is null) return Results.NotFound();
            var path = Path.ChangeExtension(location.AbsolutePath, ".lrc");
            if (!File.Exists(path)) return Results.NotFound();
            try
            {
                var info = new FileInfo(path);
                if (info.Length > MaxLyricsCharacters * 4L) return Results.Problem("Lyrics file is too large to read.", statusCode: StatusCodes.Status413PayloadTooLarge);
                var text = await File.ReadAllTextAsync(path, ct);
                if (text.Length > MaxLyricsCharacters) return Results.Problem("Lyrics file is too large to read.", statusCode: StatusCodes.Status413PayloadTooLarge);
                return Results.Ok(new { text, synchronized = HasLrcTimestamps(text) });
            }
            catch (UnauthorizedAccessException) { return Results.Problem("Lyrics file is not readable.", statusCode: StatusCodes.Status403Forbidden); }
            catch (IOException) { return Results.Problem("Lyrics file could not be read.", statusCode: StatusCodes.Status503ServiceUnavailable); }
        });

        music.MapGet("/tracks/{trackId:guid}/info", async (MusicDbContext db, IMediaFileLocator locator, Guid trackId, CancellationToken ct) =>
        {
            var track = await db.Tracks.AsNoTracking().SingleOrDefaultAsync(t => t.Id == trackId, ct);
            if (track is null) return Results.NotFound();
            var location = await locator.LocateAsync(track.FileId, ct);
            if (location is null) return Results.NotFound();
            var info = new FileInfo(location.AbsolutePath);
            return Results.Ok(new { track.Id, track.Title, track.ArtistName, album = track.Album!.Title, track.Rating, path = info.FullName, sizeBytes = info.Length, modifiedUtc = info.LastWriteTimeUtc, extension = info.Extension.TrimStart('.').ToLowerInvariant() });
        });

        music.MapPost("/tracks/{trackId:guid}/rename", async (MusicDbContext db, IMediaFileLocator locator, Guid trackId, RenameRequest request, CancellationToken ct) =>
        {
            var name = Path.GetFileNameWithoutExtension(request.FileName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name) || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) return Results.BadRequest(new { error = "Invalid file name." });
            var track = await db.Tracks.SingleOrDefaultAsync(t => t.Id == trackId, ct);
            if (track is null) return Results.NotFound();
            var location = await locator.LocateAsync(track.FileId, ct);
            if (location is null) return Results.NotFound();
            var directory = Path.GetDirectoryName(location.AbsolutePath)!;
            var destination = Path.Combine(directory, name + Path.GetExtension(location.AbsolutePath));
            if (File.Exists(destination)) return Results.Conflict(new { error = "A file with that name already exists.", path = destination });
            File.Move(location.AbsolutePath, destination);
            return Results.Ok(new { path = destination });
        });

        music.MapDelete("/tracks/{trackId:guid}", async (MusicDbContext db, IMediaFileLocator locator, Guid trackId, bool? deleteFile, CancellationToken ct) =>
        {
            var track = await db.Tracks.SingleOrDefaultAsync(t => t.Id == trackId, ct);
            if (track is null) return Results.NotFound();
            if (deleteFile == true)
            {
                var location = await locator.LocateAsync(track.FileId, ct);
                if (location is null) return Results.NotFound();
                File.Delete(location.AbsolutePath);
            }
            db.Tracks.Remove(track);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        return app;
    }

    private static bool HasLrcTimestamps(string text) => text.Split('\n').Any(line => line.Length >= 4 && line[0] == '[' && line.IndexOf(']') > 1);
    public sealed record RatingRequest(int Rating);
    public sealed record RenameRequest(string? FileName);
    public sealed record LyricsRequest(string Text, bool Synchronized);
}
