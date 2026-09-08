using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using oniDash.Music.Domain;
using oniDash.Music.Persistence;

namespace oniDash.Music.Endpoints;

public static class MusicEngagementEndpoints
{
    public static IEndpointRouteBuilder MapMusicEngagementEndpoints(this IEndpointRouteBuilder app)
    {
        var music = app.MapGroup("/api/music").WithTags("Music");

        music.MapGet("/favorites", async (MusicDbContext db, CancellationToken ct) =>
            Results.Ok(await db.Favorites.AsNoTracking().OrderByDescending(f => f.CreatedAtUtc).ToListAsync(ct)));

        music.MapPost("/favorites", async (MusicDbContext db, FavoriteRequest request, CancellationToken ct) =>
        {
            var entityType = request.EntityType.Trim().ToLowerInvariant();
            if (entityType is not ("track" or "album" or "artist") || request.EntityId == Guid.Empty)
                return Results.BadRequest(new { error = "Entity type must be track, album, or artist and entityId must be valid." });
            var exists = await db.Favorites.AnyAsync(f => f.EntityType == entityType && f.EntityId == request.EntityId, ct);
            if (!exists) db.Favorites.Add(new MusicFavorite { Id = Guid.NewGuid(), EntityType = entityType, EntityId = request.EntityId, CreatedAtUtc = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync(ct);
            return Results.Ok();
        });

        music.MapDelete("/favorites/{entityType}/{entityId:guid}", async (MusicDbContext db, string entityType, Guid entityId, CancellationToken ct) =>
        {
            var favorite = await db.Favorites.SingleOrDefaultAsync(f => f.EntityType == entityType.ToLower() && f.EntityId == entityId, ct);
            if (favorite is null) return Results.NotFound();
            db.Favorites.Remove(favorite); await db.SaveChangesAsync(ct); return Results.NoContent();
        });

        music.MapGet("/history", async (MusicDbContext db, int? limit, int? offset, CancellationToken ct) =>
        {
            var take = Math.Clamp(limit ?? 50, 1, 200); var skip = Math.Max(0, offset ?? 0);
            return Results.Ok(await db.PlayHistory.AsNoTracking().OrderByDescending(h => h.StartedAtUtc).Skip(skip).Take(take).ToListAsync(ct));
        });

        music.MapPost("/tracks/{trackId:guid}/play", async (MusicDbContext db, Guid trackId, PlayRequest request, CancellationToken ct) =>
        {
            if (!await db.Tracks.AnyAsync(t => t.Id == trackId, ct)) return Results.NotFound();
            var played = Math.Max(0, request.PlayedSeconds); var ratio = Math.Clamp(request.CompletionRatio, 0, 1);
            db.PlayHistory.Add(new MusicPlayHistory { Id = Guid.NewGuid(), TrackId = trackId, StartedAtUtc = DateTimeOffset.UtcNow, CompletedAtUtc = ratio >= 0.95 ? DateTimeOffset.UtcNow : null, PlayedSeconds = played, CompletionRatio = ratio, Source = string.IsNullOrWhiteSpace(request.Source) ? "local" : request.Source.Trim() });
            await db.SaveChangesAsync(ct); return Results.Accepted();
        });

        return app;
    }

    public sealed record FavoriteRequest(string EntityType, Guid EntityId);
    public sealed record PlayRequest(double PlayedSeconds, double CompletionRatio, string? Source);
}
