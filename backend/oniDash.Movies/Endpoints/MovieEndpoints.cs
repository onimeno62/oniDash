using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using oniDash.Movies.Cataloging;
using oniDash.Movies.Persistence;

namespace oniDash.Movies.Endpoints;

/// <summary>
/// Movie catalogue browsing and playback: browse (with watched filters), posters,
/// ranged video streaming, resume-position and watched updates, and a reindex command.
/// </summary>
public static class MovieEndpoints
{
    public static IEndpointRouteBuilder MapMovieEndpoints(this IEndpointRouteBuilder app)
    {
        var movies = app.MapGroup("/api/movies").WithTags("Movies");

        movies.MapGet("/", async (
            MoviesDbContext db, Guid? libraryId, bool? watched, CancellationToken ct) =>
        {
            var query = db.Movies.AsNoTracking();
            if (libraryId is not null)
            {
                query = query.Where(m => m.LibraryId == libraryId);
            }
            if (watched is not null)
            {
                query = query.Where(m => m.Watched == watched);
            }

            var list = await query
                .OrderBy(m => m.NormalizedTitle).ThenBy(m => m.Year)
                .Select(m => new MovieSummary(
                    m.Id, m.MediaItemId, m.Title, m.Year, m.DurationSeconds, m.Container,
                    m.PosterBlob != null, m.Watched, m.WatchProgressSeconds, m.WatchedAtUtc))
                .ToListAsync(ct);
            return Results.Ok(list);
        });

        movies.MapGet("/continue", async (
            MoviesDbContext db, Guid? libraryId, CancellationToken ct) =>
        {
            var query = db.Movies.AsNoTracking()
                .Where(m => !m.Watched && m.WatchProgressSeconds > 0);
            if (libraryId is not null)
            {
                query = query.Where(m => m.LibraryId == libraryId);
            }

            var list = await query
                // SQLite cannot ORDER BY DateTimeOffset; deepest progress first is the
                // closest supported proxy for "most recently played" (tie-break by title).
                .OrderByDescending(m => m.WatchProgressSeconds)
                .ThenBy(m => m.NormalizedTitle)
                .Select(m => new MovieSummary(
                    m.Id, m.MediaItemId, m.Title, m.Year, m.DurationSeconds, m.Container,
                    m.PosterBlob != null, m.Watched, m.WatchProgressSeconds, m.WatchedAtUtc))
                .ToListAsync(ct);
            return Results.Ok(list);
        });

        movies.MapGet("/{movieId:guid}/poster", async (
            MoviesDbContext db, Guid movieId, CancellationToken ct) =>
        {
            var movie = await db.Movies.AsNoTracking()
                .SingleOrDefaultAsync(m => m.Id == movieId, ct);
            if (movie?.PosterBlob is not { Length: > 0 } blob)
            {
                return Results.NotFound();
            }

            return Results.File(blob, movie.PosterContentType ?? "image/jpeg");
        });

        movies.MapGet("/{movieId:guid}/stream", async (
            MoviesDbContext db, IVideoFileLocator locator, Guid movieId, CancellationToken ct) =>
        {
            var movie = await db.Movies.AsNoTracking()
                .SingleOrDefaultAsync(m => m.Id == movieId, ct);
            if (movie is null)
            {
                return Results.NotFound();
            }

            var location = await locator.LocateAsync(movie.FileId, ct);
            return location is null
                ? Results.NotFound()
                : Results.File(
                    location.AbsolutePath,
                    location.ContentType,
                    enableRangeProcessing: true);
        });

        movies.MapPost("/{movieId:guid}/progress", async (
            MoviesDbContext db, Guid movieId, ProgressRequest request, CancellationToken ct) =>
        {
            var movie = await db.Movies.SingleOrDefaultAsync(m => m.Id == movieId, ct);
            if (movie is null)
            {
                return Results.NotFound();
            }

            movie.WatchProgressSeconds = Math.Max(0, request.PositionSeconds);
            // Reaching ~95% of the runtime counts as watched (resume no longer offered).
            if (movie.DurationSeconds is { } duration && request.PositionSeconds >= duration * 0.95)
            {
                movie.Watched = true;
                movie.WatchProgressSeconds = null;
            }

            movie.UpdatedAtUtc = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
            return Results.Ok(new MovieSummary(
                movie.Id, movie.MediaItemId, movie.Title, movie.Year, movie.DurationSeconds, movie.Container,
                movie.PosterBlob != null, movie.Watched, movie.WatchProgressSeconds, movie.WatchedAtUtc));
        });

        movies.MapPost("/{movieId:guid}/watched", async (
            MoviesDbContext db, Guid movieId, WatchedRequest request, CancellationToken ct) =>
        {
            var movie = await db.Movies.SingleOrDefaultAsync(m => m.Id == movieId, ct);
            if (movie is null)
            {
                return Results.NotFound();
            }

            movie.Watched = request.Watched;
            movie.WatchedAtUtc = request.Watched ? DateTimeOffset.UtcNow : null;
            // Either way the resume position resets: a finished movie offers no resume,
            // and re-watching starts fresh (mirrors MovieCatalogService.SetWatchedAsync).
            movie.WatchProgressSeconds = null;

            movie.UpdatedAtUtc = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
            return Results.Ok(new MovieSummary(
                movie.Id, movie.MediaItemId, movie.Title, movie.Year, movie.DurationSeconds, movie.Container,
                movie.PosterBlob != null, movie.Watched, movie.WatchProgressSeconds, movie.WatchedAtUtc));
        });

        movies.MapPost("/reindex", async (
            MovieReindexService reindex, Guid? libraryId, CancellationToken ct) =>
        {
            var indexed = await reindex.ReindexAsync(libraryId, ct);
            return Results.Ok(new { indexedMovies = indexed });
        });

        return app;
    }

    public sealed record ProgressRequest(double PositionSeconds);

    public sealed record WatchedRequest(bool Watched);

    public sealed record MovieSummary(
        Guid Id, Guid MediaItemId, string Title, int? Year, double? DurationSeconds, string? Container,
        bool HasPoster, bool Watched, double? WatchProgressSeconds, DateTimeOffset? WatchedAtUtc);
}
