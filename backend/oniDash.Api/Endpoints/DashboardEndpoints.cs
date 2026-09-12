using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using oniDash.Application.Media;
using oniDash.Core.Domain;
using oniDash.Infrastructure.Persistence;
using oniDash.Infrastructure.Scanning;

namespace oniDash.Api.Endpoints;

public static class DashboardEndpoints
{
    private const string FavoritesCollectionName = "Favorites";

    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/dashboard").WithTags("Dashboard");

        group.MapGet("/continue", async (OniDashDbContext db, IMediaTypeDetector types, int? limit, CancellationToken ct) =>
        {
            var take = Math.Clamp(limit ?? 12, 1, 50);
            var rows = await QueryDashboardRows(db, ct, take, x => x.UpdatedAtUtc);
            return Results.Ok(rows.Select(r => ToDto(r, types, r.Timestamp, r.IsFavorite)).ToList());
        });

        group.MapGet("/recently-added", async (OniDashDbContext db, IMediaTypeDetector types, int? limit, CancellationToken ct) =>
        {
            var take = Math.Clamp(limit ?? 12, 1, 50);
            var rows = await QueryDashboardRows(db, ct, take, x => x.CreatedAtUtc);
            return Results.Ok(rows.Select(r => ToDto(r, types, r.Timestamp, r.IsFavorite)).ToList());
        });

        group.MapGet("/recently-played", async (OniDashDbContext db, IMediaTypeDetector types, int? limit, CancellationToken ct) =>
        {
            var take = Math.Clamp(limit ?? 12, 1, 50);
            var rows = await QueryDashboardRows(db, ct, take, x => x.UpdatedAtUtc);
            return Results.Ok(rows.Select(r => ToDto(r, types, r.Timestamp, r.IsFavorite)).ToList());
        });

        group.MapGet("/favorites", async (OniDashDbContext db, IMediaTypeDetector types, int? limit, CancellationToken ct) =>
        {
            var take = Math.Clamp(limit ?? 12, 1, 50);
            var rows = await QueryDashboardRows(db, ct, take, x => x.UpdatedAtUtc, favoritesOnly: true);
            return Results.Ok(rows.Select(r => ToDto(r, types, r.Timestamp, r.IsFavorite)).ToList());
        });

        group.MapGet("/activity", async (OniDashDbContext db, IMediaTypeDetector types, int? limit, CancellationToken ct) =>
        {
            var take = Math.Clamp(limit ?? 20, 1, 100);
            // SQLite cannot ORDER BY DateTimeOffset; the activity feed is a bounded recent list.
            var recentItems = await db.MediaItems.AsNoTracking()
                .Include(x => x.Files)
                .Select(x => new { x.Id, x.DisplayName, x.UpdatedAtUtc, Extension = x.Files.OrderBy(f => f.RelativePath.Length).Select(f => f.Extension).FirstOrDefault() })
                .ToListAsync(ct);
            var newestFirst = recentItems
                .OrderByDescending(x => x.UpdatedAtUtc)
                .Take(take)
                .ToList();

            var timeline = newestFirst.Select(x => new DashboardActivityDto(
                Guid.NewGuid(),
                x.Id,
                x.DisplayName,
                types.Detect(x.Extension ?? string.Empty).ToString(),
                "Updated media item",
                x.UpdatedAtUtc
            )).ToList();

            return Results.Ok(timeline);
        });

        group.MapGet("/recommendations", async (OniDashDbContext db, IMediaTypeDetector types, int? limit, CancellationToken ct) =>
        {
            var take = Math.Clamp(limit ?? 10, 1, 30);
            // SQLite has no translatable ORDER BY random(); draw a larger recency pool and
            // shuffle in memory to keep recommendations varied.
            var pool = await QueryDashboardRows(db, ct, Math.Min(100, take * 10), x => x.UpdatedAtUtc);
            var shuffled = pool.OrderBy(_ => Guid.NewGuid()).Take(take).ToList();
            return Results.Ok(shuffled.Select(r => ToDto(r, types, r.Timestamp, r.IsFavorite)).ToList());
        });

        return app;
    }

    private static async Task<List<(Guid Id, string DisplayName, string? Extension, bool HasArtwork, DateTimeOffset Timestamp, bool IsFavorite)>> QueryDashboardRows(
        OniDashDbContext db, CancellationToken ct, int take,
        System.Linq.Expressions.Expression<Func<MediaItem, DateTimeOffset>> orderBy,
        bool favoritesOnly = false)
    {
        IQueryable<MediaItem> query = db.MediaItems.AsNoTracking()
            .Include(x => x.Files)
            .Include(x => x.Artwork)
            .Include(x => x.Collections).ThenInclude(ci => ci.Collection);

        if (favoritesOnly)
        {
            query = query.Where(x => x.Collections.Any(ci => ci.Collection.Name == FavoritesCollectionName));
        }

        // SQLite cannot ORDER BY DateTimeOffset, so the newest-first cut happens in memory
        // over a bounded pool (same pattern as the music plugin's play history).
        var items = await query.ToListAsync(ct);
        var ordered = items.OrderByDescending(orderBy.Compile()).Take(take).ToList();
        return ordered.Select(x => (
            x.Id,
            x.DisplayName,
            Extension: x.Files.OrderBy(f => f.RelativePath.Length).Select(f => f.Extension).FirstOrDefault(),
            HasArtwork: x.Artwork.Any(),
            Timestamp: orderBy.Compile()(x),
            IsFavorite: x.Collections.Any(ci => ci.Collection.Name == FavoritesCollectionName)
        )).ToList();
    }

    private static DashboardMediaItemDto ToDto(
        (Guid Id, string DisplayName, string? Extension, bool HasArtwork, DateTimeOffset Timestamp, bool IsFavorite) row,
        IMediaTypeDetector types, DateTimeOffset timestamp, bool isFavorite) =>
        new(row.Id, row.DisplayName, types.Detect(row.Extension ?? string.Empty).ToString(), row.Extension, row.HasArtwork, timestamp, isFavorite);

    public sealed record DashboardMediaItemDto(
        Guid Id,
        string Title,
        string MediaType,
        string? Extension,
        bool HasArtwork,
        DateTimeOffset Timestamp,
        bool IsFavorite
    );

    public sealed record DashboardActivityDto(
        Guid Id,
        Guid MediaItemId,
        string Title,
        string MediaType,
        string Action,
        DateTimeOffset OccurredAtUtc
    );
}
