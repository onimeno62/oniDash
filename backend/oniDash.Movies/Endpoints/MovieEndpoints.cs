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

public static class MovieEndpoints
{
    private const int DefaultPageSize = 100;
    private const int MaxPageSize = 200;

    public static IEndpointRouteBuilder MapMovieEndpoints(this IEndpointRouteBuilder app)
    {
        var movies = app.MapGroup("/api/movies").WithTags("Movies");
        movies.MapGet("/", async (MoviesDbContext db, Guid? libraryId, bool? watched, int? limit, int? offset, CancellationToken ct) =>
        {
            var query = db.Movies.AsNoTracking();
            if (libraryId is not null) query = query.Where(x => x.LibraryId == libraryId);
            if (watched is not null) query = query.Where(x => x.Watched == watched);
            return Results.Ok(await query.OrderBy(x => x.NormalizedTitle).ThenBy(x => x.Id).Skip(Math.Max(0, offset ?? 0)).Take(Math.Clamp(limit ?? DefaultPageSize, 1, MaxPageSize)).Select(ToMovieSummary).ToListAsync(ct));
        });
        movies.MapGet("/continue", async (MoviesDbContext db, Guid? libraryId, CancellationToken ct) =>
        {
            var query = db.Movies.AsNoTracking().Where(x => !x.Watched && x.WatchProgressSeconds > 0);
            if (libraryId is not null) query = query.Where(x => x.LibraryId == libraryId);
            return Results.Ok(await query.OrderByDescending(x => x.UpdatedAtUtc).Take(20).Select(ToMovieSummary).ToListAsync(ct));
        });
        movies.MapGet("/{movieId:guid}/poster", async (MoviesDbContext db, Guid movieId, CancellationToken ct) =>
        {
            var movie = await db.Movies.AsNoTracking().SingleOrDefaultAsync(x => x.Id == movieId, ct);
            return movie?.PosterBlob is { Length: > 0 } blob ? Results.File(blob, movie.PosterContentType ?? "image/jpeg") : Results.NotFound();
        });
        movies.MapGet("/{movieId:guid}/stream", async (MoviesDbContext db, IVideoFileLocator locator, Guid movieId, CancellationToken ct) =>
        {
            var movie = await db.Movies.AsNoTracking().SingleOrDefaultAsync(x => x.Id == movieId, ct);
            if (movie is null) return Results.NotFound();
            var location = await locator.LocateAsync(movie.FileId, ct);
            return location is null ? Results.NotFound() : Results.File(location.AbsolutePath, location.ContentType, enableRangeProcessing: true);
        });
        movies.MapPost("/{movieId:guid}/progress", async (MoviesDbContext db, Guid movieId, ProgressRequest request, CancellationToken ct) =>
        {
            var movie = await db.Movies.SingleOrDefaultAsync(x => x.Id == movieId, ct);
            if (movie is null) return Results.NotFound();
            ApplyProgress(movie.DurationSeconds, request.PositionSeconds, out var watched, out var position);
            movie.Watched = watched; movie.WatchedAtUtc = watched ? DateTimeOffset.UtcNow : null; movie.WatchProgressSeconds = position; movie.UpdatedAtUtc = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
            return Results.Ok(ToMovieSummary.Compile()(movie));
        });
        movies.MapPost("/{movieId:guid}/watched", async (MoviesDbContext db, Guid movieId, WatchedRequest request, CancellationToken ct) =>
        {
            var movie = await db.Movies.SingleOrDefaultAsync(x => x.Id == movieId, ct);
            if (movie is null) return Results.NotFound();
            movie.Watched = request.Watched; movie.WatchedAtUtc = request.Watched ? DateTimeOffset.UtcNow : null; movie.WatchProgressSeconds = null; movie.UpdatedAtUtc = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
            return Results.Ok(ToMovieSummary.Compile()(movie));
        });
        movies.MapPost("/reindex", async (MovieReindexService reindex, Guid? libraryId, CancellationToken ct) => Results.Ok(new { indexedMovies = await reindex.ReindexAsync(libraryId, ct) }));

        var tv = app.MapGroup("/api/tv").WithTags("TV / Anime");
        tv.MapGet("/series", async (MoviesDbContext db, Guid? libraryId, int? limit, int? offset, CancellationToken ct) =>
        {
            var query = db.TvSeries.AsNoTracking();
            if (libraryId is not null) query = query.Where(x => x.LibraryId == libraryId);
            var rows = await query.OrderBy(x => x.NormalizedTitle).ThenBy(x => x.Year).Skip(Math.Max(0, offset ?? 0)).Take(Math.Clamp(limit ?? DefaultPageSize, 1, MaxPageSize))
                .Select(x => new SeriesSummary(x.Id, x.LibraryId, x.Title, x.Year, x.IsAnime, db.TvSeasons.Count(s => s.SeriesId == x.Id), db.TvEpisodes.Count(e => db.TvSeasons.Any(s => s.Id == e.SeasonId && s.SeriesId == x.Id)))).ToListAsync(ct);
            return Results.Ok(rows);
        });
        tv.MapGet("/series/{seriesId:guid}", async (MoviesDbContext db, Guid seriesId, CancellationToken ct) =>
        {
            var series = await db.TvSeries.AsNoTracking().SingleOrDefaultAsync(x => x.Id == seriesId, ct);
            if (series is null) return Results.NotFound();
            var seasons = await db.TvSeasons.AsNoTracking().Where(x => x.SeriesId == seriesId).OrderBy(x => x.Number)
                .Select(x => new SeasonSummary(x.Id, x.Number, x.Title, db.TvEpisodes.Where(e => e.SeasonId == x.Id).OrderBy(e => e.Number).Select(ToEpisodeSummary).ToList())).ToListAsync(ct);
            return Results.Ok(new SeriesDetail(series.Id, series.LibraryId, series.Title, series.Year, series.IsAnime, seasons));
        });
        tv.MapGet("/continue", async (MoviesDbContext db, Guid? libraryId, CancellationToken ct) =>
        {
            var query = from episode in db.TvEpisodes.AsNoTracking()
                        join season in db.TvSeasons.AsNoTracking() on episode.SeasonId equals season.Id
                        join series in db.TvSeries.AsNoTracking() on season.SeriesId equals series.Id
                        where !episode.Watched && episode.WatchProgressSeconds > 0 && (libraryId == null || series.LibraryId == libraryId)
                        orderby episode.UpdatedAtUtc descending
                        select new ContinueEpisode(series.Id, series.Title, season.Number, new EpisodeSummary(episode.Id, episode.MediaItemId, episode.Number, episode.Title, episode.DurationSeconds, episode.Watched, episode.WatchProgressSeconds, episode.WatchedAtUtc));
            return Results.Ok(await query.Take(20).ToListAsync(ct));
        });
        tv.MapPost("/episodes/{episodeId:guid}/progress", async (MoviesDbContext db, Guid episodeId, ProgressRequest request, CancellationToken ct) =>
        {
            var episode = await db.TvEpisodes.SingleOrDefaultAsync(x => x.Id == episodeId, ct);
            if (episode is null) return Results.NotFound();
            ApplyProgress(episode.DurationSeconds, request.PositionSeconds, out var watched, out var position);
            episode.Watched = watched; episode.WatchedAtUtc = watched ? DateTimeOffset.UtcNow : null; episode.WatchProgressSeconds = position; episode.UpdatedAtUtc = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
            return Results.Ok(ToEpisodeSummary.Compile()(episode));
        });
        tv.MapPost("/episodes/{episodeId:guid}/watched", async (MoviesDbContext db, Guid episodeId, WatchedRequest request, CancellationToken ct) =>
        {
            var episode = await db.TvEpisodes.SingleOrDefaultAsync(x => x.Id == episodeId, ct);
            if (episode is null) return Results.NotFound();
            episode.Watched = request.Watched; episode.WatchedAtUtc = request.Watched ? DateTimeOffset.UtcNow : null; episode.WatchProgressSeconds = null; episode.UpdatedAtUtc = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
            return Results.Ok(ToEpisodeSummary.Compile()(episode));
        });
        tv.MapGet("/episodes/{episodeId:guid}/stream", async (MoviesDbContext db, IVideoFileLocator locator, Guid episodeId, CancellationToken ct) =>
        {
            var episode = await db.TvEpisodes.AsNoTracking().SingleOrDefaultAsync(x => x.Id == episodeId, ct);
            if (episode is null) return Results.NotFound();
            var location = await locator.LocateAsync(episode.FileId, ct);
            return location is null ? Results.NotFound() : Results.File(location.AbsolutePath, location.ContentType, enableRangeProcessing: true);
        });
        return app;
    }

    private static void ApplyProgress(double? duration, double requested, out bool watched, out double? position)
    {
        var value = Math.Max(0, requested);
        watched = duration is > 0 && value >= duration.Value * 0.95;
        position = watched ? null : value;
    }

    private static readonly System.Linq.Expressions.Expression<Func<Domain.Movie, MovieSummary>> ToMovieSummary = x => new(x.Id, x.MediaItemId, x.Title, x.Year, x.DurationSeconds, x.Container, x.PosterBlob != null, x.Watched, x.WatchProgressSeconds, x.WatchedAtUtc);
    private static readonly System.Linq.Expressions.Expression<Func<Domain.TvEpisode, EpisodeSummary>> ToEpisodeSummary = x => new(x.Id, x.MediaItemId, x.Number, x.Title, x.DurationSeconds, x.Watched, x.WatchProgressSeconds, x.WatchedAtUtc);

    public sealed record ProgressRequest(double PositionSeconds);
    public sealed record WatchedRequest(bool Watched);
    public sealed record MovieSummary(Guid Id, Guid MediaItemId, string Title, int? Year, double? DurationSeconds, string? Container, bool HasPoster, bool Watched, double? WatchProgressSeconds, DateTimeOffset? WatchedAtUtc);
    public sealed record SeriesSummary(Guid Id, Guid LibraryId, string Title, int? Year, bool IsAnime, int SeasonCount, int EpisodeCount);
    public sealed record SeriesDetail(Guid Id, Guid LibraryId, string Title, int? Year, bool IsAnime, System.Collections.Generic.List<SeasonSummary> Seasons);
    public sealed record SeasonSummary(Guid Id, int Number, string? Title, System.Collections.Generic.List<EpisodeSummary> Episodes);
    public sealed record EpisodeSummary(Guid Id, Guid MediaItemId, int Number, string Title, double? DurationSeconds, bool Watched, double? WatchProgressSeconds, DateTimeOffset? WatchedAtUtc);
    public sealed record ContinueEpisode(Guid SeriesId, string SeriesTitle, int SeasonNumber, EpisodeSummary Episode);
}
