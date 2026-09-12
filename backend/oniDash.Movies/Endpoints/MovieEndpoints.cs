using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using oniDash.Application.Catalogue;
using oniDash.Movies.Cataloging;
using oniDash.Movies.Domain;
using oniDash.Movies.Persistence;

namespace oniDash.Movies.Endpoints;

public static class MovieEndpoints
{
    public static IEndpointRouteBuilder MapMovieEndpoints(this IEndpointRouteBuilder app)
    {
        var movies = app.MapGroup("/api/movies").WithTags("Movies");
        movies.MapGet("/", async (MoviesDbContext db, Guid? libraryId, bool? watched, int? limit, int? offset, CancellationToken ct) =>
        {
            var query = db.Movies.AsNoTracking();
            if (libraryId is not null) query = query.Where(x => x.LibraryId == libraryId);
            if (watched is not null) query = query.Where(x => x.Watched == watched);
            var rows = await query.OrderBy(x => x.NormalizedTitle).ThenBy(x => x.Id).Skip(Math.Max(0, offset ?? 0)).Take(Math.Clamp(limit ?? 100, 1, 200))
                .Select(x => new MovieSummary(x.Id, x.MediaItemId, x.Title, x.Year, x.DurationSeconds, x.Container, x.PosterBlob != null, x.Watched, x.WatchProgressSeconds, x.WatchedAtUtc)).ToListAsync(ct);
            return Results.Ok(rows);
        });
        movies.MapGet("/continue", async (MoviesDbContext db, Guid? libraryId, CancellationToken ct) =>
        {
            var query = db.Movies.AsNoTracking().Where(x => !x.Watched && x.WatchProgressSeconds > 0);
            if (libraryId is not null) query = query.Where(x => x.LibraryId == libraryId);
            return Results.Ok(await query.OrderByDescending(x => x.UpdatedAtUtc).Take(20).Select(x => new MovieSummary(x.Id, x.MediaItemId, x.Title, x.Year, x.DurationSeconds, x.Container, x.PosterBlob != null, x.Watched, x.WatchProgressSeconds, x.WatchedAtUtc)).ToListAsync(ct));
        });
        movies.MapGet("/{movieId:guid}/poster", async (MoviesDbContext db, Guid movieId, CancellationToken ct) => { var movie = await db.Movies.AsNoTracking().SingleOrDefaultAsync(x => x.Id == movieId, ct); return movie?.PosterBlob is { Length: > 0 } blob ? Results.File(blob, movie.PosterContentType ?? "image/jpeg") : Results.NotFound(); });
        movies.MapGet("/{movieId:guid}/stream", async (MoviesDbContext db, IVideoFileLocator locator, Guid movieId, CancellationToken ct) => { var movie = await db.Movies.AsNoTracking().SingleOrDefaultAsync(x => x.Id == movieId, ct); if (movie is null) return Results.NotFound(); var file = await locator.LocateAsync(movie.FileId, ct); return file is null ? Results.NotFound() : Results.File(file.AbsolutePath, file.ContentType, enableRangeProcessing: true); });
        movies.MapPost("/{movieId:guid}/progress", async (MoviesDbContext db, Guid movieId, ProgressRequest request, CancellationToken ct) => { var movie = await db.Movies.SingleOrDefaultAsync(x => x.Id == movieId, ct); if (movie is null) return Results.NotFound(); ApplyProgress(movie.DurationSeconds, request.PositionSeconds, out var watched, out var position); movie.Watched = watched; movie.WatchedAtUtc = watched ? DateTimeOffset.UtcNow : null; movie.WatchProgressSeconds = position; movie.UpdatedAtUtc = DateTimeOffset.UtcNow; await db.SaveChangesAsync(ct); return Results.Ok(MovieDto(movie)); });
        movies.MapPost("/{movieId:guid}/watched", async (MoviesDbContext db, Guid movieId, WatchedRequest request, CancellationToken ct) => { var movie = await db.Movies.SingleOrDefaultAsync(x => x.Id == movieId, ct); if (movie is null) return Results.NotFound(); movie.Watched = request.Watched; movie.WatchedAtUtc = request.Watched ? DateTimeOffset.UtcNow : null; movie.WatchProgressSeconds = null; movie.UpdatedAtUtc = DateTimeOffset.UtcNow; await db.SaveChangesAsync(ct); return Results.Ok(MovieDto(movie)); });
        movies.MapPost("/reindex", async (MovieReindexService service, Guid? libraryId, CancellationToken ct) => Results.Ok(new { indexedMovies = await service.ReindexAsync(libraryId, ct) }));

        var tv = app.MapGroup("/api/tv").WithTags("TV / Anime");
        tv.MapGet("/series", async (MoviesDbContext db, Guid? libraryId, CancellationToken ct) => { var query = db.TvSeries.AsNoTracking(); if (libraryId is not null) query = query.Where(x => x.LibraryId == libraryId); var rows = await query.OrderBy(x => x.NormalizedTitle).Select(x => new SeriesSummary(x.Id, x.LibraryId, x.Title, x.Year, x.IsAnime, db.TvSeasons.Count(s => s.SeriesId == x.Id))).ToListAsync(ct); return Results.Ok(rows); });
        tv.MapGet("/series/{seriesId:guid}", async (MoviesDbContext db, Guid seriesId, CancellationToken ct) => { var series = await db.TvSeries.AsNoTracking().SingleOrDefaultAsync(x => x.Id == seriesId, ct); if (series is null) return Results.NotFound(); var seasons = await db.TvSeasons.AsNoTracking().Where(x => x.SeriesId == seriesId).OrderBy(x => x.Number).Select(x => new TvSeasonDto(x.Id, x.Number, x.Title)).ToListAsync(ct); var episodes = await db.TvEpisodes.AsNoTracking().Where(x => db.TvSeasons.Any(s => s.Id == x.SeasonId && s.SeriesId == seriesId)).OrderBy(x => x.Number).Select(x => new EpisodeDto(x.Id, x.SeasonId, x.MediaItemId, x.Number, x.Title, x.DurationSeconds, x.Watched, x.WatchProgressSeconds, x.WatchedAtUtc)).ToListAsync(ct); return Results.Ok(new SeriesDetail(series.Id, series.LibraryId, series.Title, series.Year, series.IsAnime, seasons, episodes)); });
        tv.MapGet("/continue", async (MoviesDbContext db, Guid? libraryId, CancellationToken ct) => { var rows = await (from episode in db.TvEpisodes.AsNoTracking() join season in db.TvSeasons.AsNoTracking() on episode.SeasonId equals season.Id join series in db.TvSeries.AsNoTracking() on season.SeriesId equals series.Id where !episode.Watched && episode.WatchProgressSeconds > 0 && (libraryId == null || series.LibraryId == libraryId) orderby episode.UpdatedAtUtc descending select new ContinueEpisode(series.Id, series.Title, season.Number, episode.Id, episode.Number, episode.Title, episode.WatchProgressSeconds)).Take(20).ToListAsync(ct); return Results.Ok(rows); });
        tv.MapPost("/episodes/{episodeId:guid}/progress", async (MoviesDbContext db, Guid episodeId, ProgressRequest request, CancellationToken ct) => { var episode = await db.TvEpisodes.SingleOrDefaultAsync(x => x.Id == episodeId, ct); if (episode is null) return Results.NotFound(); ApplyProgress(episode.DurationSeconds, request.PositionSeconds, out var watched, out var position); episode.Watched = watched; episode.WatchedAtUtc = watched ? DateTimeOffset.UtcNow : null; episode.WatchProgressSeconds = position; episode.UpdatedAtUtc = DateTimeOffset.UtcNow; await db.SaveChangesAsync(ct); return Results.Ok(EpisodeDtoOf(episode)); });
        tv.MapPost("/episodes/{episodeId:guid}/watched", async (MoviesDbContext db, Guid episodeId, WatchedRequest request, CancellationToken ct) => { var episode = await db.TvEpisodes.SingleOrDefaultAsync(x => x.Id == episodeId, ct); if (episode is null) return Results.NotFound(); episode.Watched = request.Watched; episode.WatchedAtUtc = request.Watched ? DateTimeOffset.UtcNow : null; episode.WatchProgressSeconds = null; episode.UpdatedAtUtc = DateTimeOffset.UtcNow; await db.SaveChangesAsync(ct); return Results.Ok(EpisodeDtoOf(episode)); });
        tv.MapGet("/episodes/{episodeId:guid}/stream", async (MoviesDbContext db, IVideoFileLocator locator, Guid episodeId, CancellationToken ct) => { var episode = await db.TvEpisodes.AsNoTracking().SingleOrDefaultAsync(x => x.Id == episodeId, ct); if (episode is null) return Results.NotFound(); var file = await locator.LocateAsync(episode.FileId, ct); return file is null ? Results.NotFound() : Results.File(file.AbsolutePath, file.ContentType, enableRangeProcessing: true); });
        tv.MapGet("/anime/{mediaItemId:guid}/suggestion", async (IAnimeMetadataProvider provider, Guid mediaItemId, CancellationToken ct) => { var suggestion = await provider.SuggestAsync(mediaItemId, ct); return suggestion is null ? Results.NoContent() : Results.Ok(suggestion); });
        return app;
    }

    private static MovieSummary MovieDto(Movie x) => new(x.Id, x.MediaItemId, x.Title, x.Year, x.DurationSeconds, x.Container, x.PosterBlob != null, x.Watched, x.WatchProgressSeconds, x.WatchedAtUtc);
    private static EpisodeDto EpisodeDtoOf(TvEpisode x) => new(x.Id, x.SeasonId, x.MediaItemId, x.Number, x.Title, x.DurationSeconds, x.Watched, x.WatchProgressSeconds, x.WatchedAtUtc);
    private static void ApplyProgress(double? duration, double requested, out bool watched, out double? position) { var value = Math.Max(0, requested); watched = duration is > 0 && value >= duration.Value * 0.95; position = watched ? null : value; }
    public sealed record ProgressRequest(double PositionSeconds); public sealed record WatchedRequest(bool Watched);
    public sealed record MovieSummary(Guid Id, Guid MediaItemId, string Title, int? Year, double? DurationSeconds, string? Container, bool HasPoster, bool Watched, double? WatchProgressSeconds, DateTimeOffset? WatchedAtUtc);
    public sealed record SeriesSummary(Guid Id, Guid LibraryId, string Title, int? Year, bool IsAnime, int SeasonCount);
    public sealed record SeriesDetail(Guid Id, Guid LibraryId, string Title, int? Year, bool IsAnime, System.Collections.Generic.List<TvSeasonDto> Seasons, System.Collections.Generic.List<EpisodeDto> Episodes);
    public sealed record TvSeasonDto(Guid Id, int Number, string? Title);
    public sealed record EpisodeDto(Guid Id, Guid SeasonId, Guid MediaItemId, int Number, string Title, double? DurationSeconds, bool Watched, double? WatchProgressSeconds, DateTimeOffset? WatchedAtUtc);
    public sealed record ContinueEpisode(Guid SeriesId, string SeriesTitle, int SeasonNumber, Guid EpisodeId, int EpisodeNumber, string EpisodeTitle, double? WatchProgressSeconds);
}
