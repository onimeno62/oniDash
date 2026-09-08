using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using oniDash.Music.Domain;
using oniDash.Music.Persistence;

namespace oniDash.Music.Endpoints;

public static class MusicPlaylistEndpoints
{
    public static IEndpointRouteBuilder MapMusicPlaylistEndpoints(this IEndpointRouteBuilder app)
    {
        var music = app.MapGroup("/api/music").WithTags("Music playlists");
        music.MapGet("/playlists", async (MusicDbContext db, CancellationToken ct) => Results.Ok(await db.Playlists.AsNoTracking().OrderBy(p => p.Name).Select(p => new PlaylistSummary(p.Id, p.Name, p.Description, p.IsSmart, p.UpdatedAtUtc)).ToListAsync(ct)));
        music.MapPost("/playlists", async (MusicDbContext db, PlaylistRequest request, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 256) return Results.BadRequest(new { error = "Playlist name is required and must be 256 characters or fewer." });
            if (request.IsSmart && string.IsNullOrWhiteSpace(request.SmartQuery)) return Results.BadRequest(new { error = "Smart playlists require a query." });
            var now = DateTimeOffset.UtcNow; var playlist = new MusicPlaylist { Id = Guid.NewGuid(), Name = request.Name.Trim(), Description = request.Description?.Trim(), IsSmart = request.IsSmart, SmartQuery = request.IsSmart ? request.SmartQuery : null, CreatedAtUtc = now, UpdatedAtUtc = now };
            db.Playlists.Add(playlist); await db.SaveChangesAsync(ct); return Results.Created($"/api/music/playlists/{playlist.Id}", new PlaylistSummary(playlist.Id, playlist.Name, playlist.Description, playlist.IsSmart, playlist.UpdatedAtUtc));
        });
        music.MapGet("/playlists/{playlistId:guid}", async (MusicDbContext db, Guid playlistId, int? limit, int? offset, CancellationToken ct) =>
        {
            var playlist = await db.Playlists.AsNoTracking().SingleOrDefaultAsync(p => p.Id == playlistId, ct); if (playlist is null) return Results.NotFound();
            var take = Math.Clamp(limit ?? 200, 1, 500); var skip = Math.Max(0, offset ?? 0);
            IQueryable<MusicTrack> tracks = playlist.IsSmart ? ApplySmartQuery(db.Tracks.AsNoTracking(), playlist.SmartQuery) : db.PlaylistItems.AsNoTracking().Where(i => i.PlaylistId == playlistId).OrderBy(i => i.Position).Skip(skip).Take(take).Select(i => i.Track!);
            var result = await tracks.Select(t => new TrackSummary(t.Id, t.Title, t.ArtistName, t.Album!.Title, t.DurationSeconds, t.Genre)).ToListAsync(ct);
            return Results.Ok(new PlaylistDetail(playlist.Id, playlist.Name, playlist.Description, playlist.IsSmart, playlist.SmartQuery, result));
        });
        music.MapPatch("/playlists/{playlistId:guid}", async (MusicDbContext db, Guid playlistId, PlaylistRequest request, CancellationToken ct) =>
        {
            var playlist = await db.Playlists.SingleOrDefaultAsync(p => p.Id == playlistId, ct); if (playlist is null) return Results.NotFound();
            if (!string.IsNullOrWhiteSpace(request.Name)) playlist.Name = request.Name.Trim(); playlist.Description = request.Description?.Trim(); playlist.IsSmart = request.IsSmart; playlist.SmartQuery = request.IsSmart ? request.SmartQuery : null; playlist.UpdatedAtUtc = DateTimeOffset.UtcNow; await db.SaveChangesAsync(ct); return Results.Ok(new PlaylistSummary(playlist.Id, playlist.Name, playlist.Description, playlist.IsSmart, playlist.UpdatedAtUtc));
        });
        music.MapDelete("/playlists/{playlistId:guid}", async (MusicDbContext db, Guid playlistId, CancellationToken ct) => { var playlist = await db.Playlists.FindAsync([playlistId], ct); if (playlist is null) return Results.NotFound(); db.Playlists.Remove(playlist); await db.SaveChangesAsync(ct); return Results.NoContent(); });
        music.MapPost("/playlists/{playlistId:guid}/items", async (MusicDbContext db, Guid playlistId, PlaylistItemRequest request, CancellationToken ct) =>
        {
            var playlist = await db.Playlists.FindAsync([playlistId], ct); if (playlist is null) return Results.NotFound(); if (playlist.IsSmart) return Results.Conflict(new { error = "Smart playlists cannot contain manual items." }); if (!await db.Tracks.AnyAsync(t => t.Id == request.TrackId, ct)) return Results.NotFound(); if (await db.PlaylistItems.AnyAsync(i => i.PlaylistId == playlistId && i.TrackId == request.TrackId, ct)) return Results.Conflict(new { error = "Track is already in this playlist." }); var position = request.Position ?? await db.PlaylistItems.Where(i => i.PlaylistId == playlistId).Select(i => (int?)i.Position).MaxAsync(ct) + 1 ?? 0; db.PlaylistItems.Add(new MusicPlaylistItem { Id = Guid.NewGuid(), PlaylistId = playlistId, TrackId = request.TrackId, Position = Math.Max(0, position), AddedAtUtc = DateTimeOffset.UtcNow }); playlist.UpdatedAtUtc = DateTimeOffset.UtcNow; await db.SaveChangesAsync(ct); return Results.Ok();
        });
        music.MapDelete("/playlists/{playlistId:guid}/items/{itemId:guid}", async (MusicDbContext db, Guid playlistId, Guid itemId, CancellationToken ct) => { var item = await db.PlaylistItems.SingleOrDefaultAsync(i => i.PlaylistId == playlistId && i.Id == itemId, ct); if (item is null) return Results.NotFound(); db.PlaylistItems.Remove(item); await db.SaveChangesAsync(ct); return Results.NoContent(); });
        music.MapPatch("/playlists/{playlistId:guid}/items/reorder", async (MusicDbContext db, Guid playlistId, ReorderRequest request, CancellationToken ct) => { var items = await db.PlaylistItems.Where(i => i.PlaylistId == playlistId).ToListAsync(ct); var order = request.ItemIds; if (order.Count != items.Count || order.Distinct().Count() != order.Count || items.Any(i => !order.Contains(i.Id))) return Results.BadRequest(new { error = "Order must include every playlist item exactly once." }); for (var index = 0; index < order.Count; index++) items.Single(i => i.Id == order[index]).Position = index; await db.SaveChangesAsync(ct); return Results.NoContent(); });
        return app;
    }

    private static IQueryable<MusicTrack> ApplySmartQuery(IQueryable<MusicTrack> query, string? raw)
    {
        SmartRule? rule = null; try { rule = string.IsNullOrWhiteSpace(raw) ? null : JsonSerializer.Deserialize<SmartRule>(raw); } catch (JsonException) { }
        if (rule is null) return query.OrderBy(t => t.Title).Take(200);
        if (!string.IsNullOrWhiteSpace(rule.Genre)) query = query.Where(t => t.Genre == rule.Genre);
        if (!string.IsNullOrWhiteSpace(rule.ArtistContains)) query = query.Where(t => t.ArtistName != null && t.ArtistName.Contains(rule.ArtistContains));
        if (rule.MinYear is not null) query = query.Where(t => t.Year >= rule.MinYear);
        if (rule.MaxYear is not null) query = query.Where(t => t.Year <= rule.MaxYear);
        return (rule.Sort?.ToLowerInvariant()) switch { "duration" => query.OrderBy(t => t.DurationSeconds).Take(Math.Clamp(rule.Limit ?? 200, 1, 500)), "year" => query.OrderByDescending(t => t.Year).ThenBy(t => t.Title).Take(Math.Clamp(rule.Limit ?? 200, 1, 500)), _ => query.OrderBy(t => t.Title).Take(Math.Clamp(rule.Limit ?? 200, 1, 500)) };
    }

    public sealed record PlaylistRequest(string Name, string? Description, bool IsSmart, string? SmartQuery);
    public sealed record PlaylistItemRequest(Guid TrackId, int? Position);
    public sealed record ReorderRequest(System.Collections.Generic.IReadOnlyList<Guid> ItemIds);
    public sealed record SmartRule(string? Genre, string? ArtistContains, int? MinYear, int? MaxYear, string? Sort, int? Limit);
    public sealed record PlaylistSummary(Guid Id, string Name, string? Description, bool IsSmart, DateTimeOffset UpdatedAtUtc);
    public sealed record PlaylistDetail(Guid Id, string Name, string? Description, bool IsSmart, string? SmartQuery, System.Collections.Generic.IReadOnlyList<TrackSummary> Tracks);
    public sealed record TrackSummary(Guid Id, string Title, string? ArtistName, string AlbumTitle, double? DurationSeconds, string? Genre);
}
