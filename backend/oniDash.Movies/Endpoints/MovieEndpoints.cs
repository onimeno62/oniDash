using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using oniDash.Movies.Cataloging;
using oniDash.Movies.Domain;
using oniDash.Movies.Persistence;

namespace oniDash.Movies.Endpoints;

public static class MovieEndpoints
{
    public static IEndpointRouteBuilder MapMovieEndpoints(this IEndpointRouteBuilder app)
    {
        var movies = app.MapGroup("/api/movies").WithTags("Movies");
        movies.MapGet("/", async (MoviesDbContext db, Guid? libraryId, bool? watched, CancellationToken ct) =>
        {
            var query = db.Movies.AsNoTracking();
            if (libraryId is not null) query = query.Where(m => m.LibraryId == libraryId);
            if (watched is not null) query = query.Where(m => m.Watched == watched);
            var list = await query.OrderBy(m => m.NormalizedTitle).ThenBy(m => m.Year)
                .Select(m => ToSummary(m)).ToListAsync(ct);
            return Results.Ok(list);
        });
        movies.MapGet("/continue", async (MoviesDbContext db, Guid? libraryId, CancellationToken ct) =>
        {
            var query = db.Movies.AsNoTracking().Where(m => !m.Watched && m.WatchProgressSeconds > 0);
            if (libraryId is not null) query = query.Where(m => m.LibraryId == libraryId);
            var list = await query.OrderByDescending(m => m.WatchProgressSeconds).ThenBy(m => m.NormalizedTitle)
                .Select(m => ToSummary(m)).ToListAsync(ct);
            return Results.Ok(list);
        });
        movies.MapGet("/{movieId:guid}/poster", async (MoviesDbContext db, Guid movieId, CancellationToken ct) =>
        {
            var movie = await db.Movies.AsNoTracking().SingleOrDefaultAsync(m => m.Id == movieId, ct);
            if (movie?.PosterBlob is not { Length: > 0 } blob) return Results.NotFound();
            return Results.File(blob, movie.PosterContentType ?? "image/jpeg");
        });
        movies.MapGet("/{movieId:guid}/stream", async (MoviesDbContext db, IVideoFileLocator locator, Guid movieId, CancellationToken ct) =>
        {
            var movie = await db.Movies.AsNoTracking().SingleOrDefaultAsync(m => m.Id == movieId, ct);
            if (movie is null) return Results.NotFound();
            var location = await locator.LocateAsync(movie.FileId, ct);
            return location is null ? Results.NotFound() : Results.File(location.AbsolutePath, location.ContentType, enableRangeProcessing: true);
        });
        movies.MapPost("/{movieId:guid}/progress", async (MoviesDbContext db, Guid movieId, ProgressRequest request, CancellationToken ct) =>
        {
            var movie = await db.Movies.SingleOrDefaultAsync(m => m.Id == movieId, ct);
            if (movie is null) return Results.NotFound();
            var position = Math.Max(0, request.PositionSeconds);
            if (movie.DurationSeconds is { } duration && position >= duration * 0.95)
            {
                movie.Watched = true;
                movie.WatchedAtUtc = DateTimeOffset.UtcNow;
                movie.WatchProgressSeconds = null;
            }
            else movie.WatchProgressSeconds = position;
            movie.UpdatedAtUtc = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
            return Results.Ok(ToSummary(movie));
        });
        movies.MapPost("/{movieId:guid}/watched", async (MoviesDbContext db, Guid movieId, WatchedRequest request, CancellationToken ct) =>
        {
            var movie = await db.Movies.SingleOrDefaultAsync(m => m.Id == movieId, ct);
            if (movie is null) return Results.NotFound();
            movie.Watched = request.Watched;
            movie.WatchedAtUtc = request.Watched ? DateTimeOffset.UtcNow : null;
            movie.WatchProgressSeconds = null;
            movie.UpdatedAtUtc = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
            return Results.Ok(ToSummary(movie));
        });
        movies.MapPost("/reindex", async (MovieReindexService reindex, Guid? libraryId, CancellationToken ct) =>
        {
            var indexed = await reindex.ReindexAsync(libraryId, ct);
            return Results.Ok(new { indexedMovies = indexed });
        });
        return app;
    }

    private static MovieSummary ToSummary(Movie movie) => new(
        movie.Id, movie.MediaItemId, movie.Title, movie.Year, movie.DurationSeconds, movie.Container,
        movie.PosterBlob != null, movie.Watched, movie.WatchProgressSeconds, movie.WatchedAtUtc);

    public sealed record ProgressRequest(double PositionSeconds);
    public sealed record WatchedRequest(bool Watched);
    public sealed record MovieSummary(Guid Id, Guid MediaItemId, string Title, int? Year, double? DurationSeconds,
        string? Container, bool HasPoster, bool Watched, double? WatchProgressSeconds, DateTimeOffset? WatchedAtUtc);
}
