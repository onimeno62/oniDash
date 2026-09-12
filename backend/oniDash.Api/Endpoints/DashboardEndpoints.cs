using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using oniDash.Core.Domain;
using oniDash.Infrastructure.Persistence;

namespace oniDash.Api.Endpoints;

public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/dashboard").WithTags("Dashboard");

        group.MapGet("/continue", async (OniDashDbContext db, int? limit, CancellationToken ct) =>
        {
            var take = Math.Clamp(limit ?? 12, 1, 50);
            var items = await db.MediaItems.AsNoTracking()
                .Include(x => x.Files)
                .Include(x => x.Artwork)
                .OrderByDescending(x => x.UpdatedAtUtc)
                .Take(take)
                .Select(x => new DashboardMediaItemDto(
                    x.Id,
                    x.DisplayName,
                    x.Type.ToString(),
                    x.Files.OrderBy(f => f.RelativePath.Length).Select(f => f.Extension).FirstOrDefault(),
                    x.Artwork.Any(),
                    x.UpdatedAtUtc,
                    false
                ))
                .ToListAsync(ct);

            return Results.Ok(items);
        });

        group.MapGet("/recently-added", async (OniDashDbContext db, int? limit, CancellationToken ct) =>
        {
            var take = Math.Clamp(limit ?? 12, 1, 50);
            var items = await db.MediaItems.AsNoTracking()
                .Include(x => x.Files)
                .Include(x => x.Artwork)
                .OrderByDescending(x => x.CreatedAtUtc)
                .Take(take)
                .Select(x => new DashboardMediaItemDto(
                    x.Id,
                    x.DisplayName,
                    x.Type.ToString(),
                    x.Files.OrderBy(f => f.RelativePath.Length).Select(f => f.Extension).FirstOrDefault(),
                    x.Artwork.Any(),
                    x.CreatedAtUtc,
                    false
                ))
                .ToListAsync(ct);

            return Results.Ok(items);
        });

        group.MapGet("/recently-played", async (OniDashDbContext db, int? limit, CancellationToken ct) =>
        {
            var take = Math.Clamp(limit ?? 12, 1, 50);
            var items = await db.MediaItems.AsNoTracking()
                .Include(x => x.Files)
                .Include(x => x.Artwork)
                .OrderByDescending(x => x.UpdatedAtUtc)
                .Take(take)
                .Select(x => new DashboardMediaItemDto(
                    x.Id,
                    x.DisplayName,
                    x.Type.ToString(),
                    x.Files.OrderBy(f => f.RelativePath.Length).Select(f => f.Extension).FirstOrDefault(),
                    x.Artwork.Any(),
                    x.UpdatedAtUtc,
                    false
                ))
                .ToListAsync(ct);

            return Results.Ok(items);
        });

        group.MapGet("/favorites", async (OniDashDbContext db, int? limit, CancellationToken ct) =>
        {
            var take = Math.Clamp(limit ?? 12, 1, 50);
            var items = await db.MediaItems.AsNoTracking()
                .Include(x => x.Files)
                .Include(x => x.Artwork)
                .OrderBy(x => x.DisplayName)
                .Take(take)
                .Select(x => new DashboardMediaItemDto(
                    x.Id,
                    x.DisplayName,
                    x.Type.ToString(),
                    x.Files.OrderBy(f => f.RelativePath.Length).Select(f => f.Extension).FirstOrDefault(),
                    x.Artwork.Any(),
                    x.UpdatedAtUtc,
                    true
                ))
                .ToListAsync(ct);

            return Results.Ok(items);
        });

        group.MapGet("/activity", async (OniDashDbContext db, int? limit, CancellationToken ct) =>
        {
            var take = Math.Clamp(limit ?? 20, 1, 100);
            var recentItems = await db.MediaItems.AsNoTracking()
                .OrderByDescending(x => x.UpdatedAtUtc)
                .Take(take)
                .ToListAsync(ct);

            var timeline = recentItems.Select(x => new DashboardActivityDto(
                Guid.NewGuid(),
                x.Id,
                x.DisplayName,
                x.Type.ToString(),
                "Updated media item",
                x.UpdatedAtUtc
            )).ToList();

            return Results.Ok(timeline);
        });

        group.MapGet("/recommendations", async (OniDashDbContext db, int? limit, CancellationToken ct) =>
        {
            var take = Math.Clamp(limit ?? 10, 1, 30);
            var localItems = await db.MediaItems.AsNoTracking()
                .Include(x => x.Files)
                .Include(x => x.Artwork)
                .OrderBy(x => EF.Functions.Random())
                .Take(take)
                .Select(x => new DashboardMediaItemDto(
                    x.Id,
                    x.DisplayName,
                    x.Type.ToString(),
                    x.Files.OrderBy(f => f.RelativePath.Length).Select(f => f.Extension).FirstOrDefault(),
                    x.Artwork.Any(),
                    x.UpdatedAtUtc,
                    false
                ))
                .ToListAsync(ct);

            return Results.Ok(localItems);
        });

        return app;
    }

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
