using System;
using System.Collections.Generic;
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
            if (!ValidateRequest(request, out var error)) return Results.BadRequest(new { error });
            var now = DateTimeOffset.UtcNow;
            var playlist = new MusicPlaylist { Id = Guid.NewGuid(), Name = request.Name.Trim(), Description = request.Description?.Trim(), IsSmart = request.IsSmart, SmartQuery = request.IsSmart ? request.SmartQuery : null, CreatedAtUtc = now, UpdatedAtUtc = now };
            db.Playlists.Add(playlist); await db.SaveChangesAsync(ct);
            return Results.Created($"/api/music/playlists/{playlist.Id}", new PlaylistSummary(playlist.Id, playlist.Name, playlist.Description, playlist.IsSmart, playlist.UpdatedAtUtc));
        });
        music.MapGet("/playlists/{playlistId:guid}", async (MusicDbContext db, Guid playlistId, int? limit, int? offset, CancellationToken ct) =>
        {
            var playlist = await db.Playlists.AsNoTracking().SingleOrDefaultAsync(p => p.Id == playlistId, ct); if (playlist is null) return Results.NotFound();
            var take = Math.Clamp(limit ?? 200, 1, 500); var skip = Math.Max(0, offset ?? 0);
            IQueryable<MusicTrack> tracks = playlist.IsSmart ? ApplySmartQuery(db.Tracks.AsNoTracking(), playlist.SmartQuery, take, skip) : db.PlaylistItems.AsNoTracking().Where(i => i.PlaylistId == playlistId).OrderBy(i => i.Position).Skip(skip).Take(take).Select(i => i.Track!);
            var result = await tracks.Select(t => new TrackSummary(t.Id, t.Title, t.ArtistName, t.Album!.Title, t.DurationSeconds, t.Genre)).ToListAsync(ct);
            return Results.Ok(new PlaylistDetail(playlist.Id, playlist.Name, playlist.Description, playlist.IsSmart, playlist.SmartQuery, result));
        });
        music.MapPatch("/playlists/{playlistId:guid}", async (MusicDbContext db, Guid playlistId, PlaylistRequest request, CancellationToken ct) =>
        {
            var playlist = await db.Playlists.SingleOrDefaultAsync(p => p.Id == playlistId, ct); if (playlist is null) return Results.NotFound();
            if (!ValidateRequest(request, out var error)) return Results.BadRequest(new { error });
            if (!string.IsNullOrWhiteSpace(request.Name)) playlist.Name = request.Name.Trim(); playlist.Description = request.Description?.Trim(); playlist.IsSmart = request.IsSmart; playlist.SmartQuery = request.IsSmart ? request.SmartQuery : null; playlist.UpdatedAtUtc = DateTimeOffset.UtcNow; await db.SaveChangesAsync(ct);
            return Results.Ok(new PlaylistSummary(playlist.Id, playlist.Name, playlist.Description, playlist.IsSmart, playlist.UpdatedAtUtc));
        });
        music.MapDelete("/playlists/{playlistId:guid}", async (MusicDbContext db, Guid playlistId, CancellationToken ct) => { var playlist = await db.Playlists.FindAsync([playlistId], ct); if (playlist is null) return Results.NotFound(); db.Playlists.Remove(playlist); await db.SaveChangesAsync(ct); return Results.NoContent(); });
        music.MapPost("/playlists/{playlistId:guid}/items", async (MusicDbContext db, Guid playlistId, PlaylistItemRequest request, CancellationToken ct) =>
        {
            var playlist = await db.Playlists.FindAsync([playlistId], ct); if (playlist is null) return Results.NotFound(); if (playlist.IsSmart) return Results.Conflict(new { error = "Smart playlists cannot contain manual items." }); if (!await db.Tracks.AnyAsync(t => t.Id == request.TrackId, ct)) return Results.NotFound(); if (await db.PlaylistItems.AnyAsync(i => i.PlaylistId == playlistId && i.TrackId == request.TrackId, ct)) return Results.Conflict(new { error = "Track is already in this playlist." }); var max = await db.PlaylistItems.Where(i => i.PlaylistId == playlistId).Select(i => (int?)i.Position).MaxAsync(ct); var position = request.Position ?? (max is null ? 0 : max.Value + 1); db.PlaylistItems.Add(new MusicPlaylistItem { Id = Guid.NewGuid(), PlaylistId = playlistId, TrackId = request.TrackId, Position = Math.Max(0, position), AddedAtUtc = DateTimeOffset.UtcNow }); playlist.UpdatedAtUtc = DateTimeOffset.UtcNow; await db.SaveChangesAsync(ct); return Results.Ok();
        });
        music.MapDelete("/playlists/{playlistId:guid}/items/{itemId:guid}", async (MusicDbContext db, Guid playlistId, Guid itemId, CancellationToken ct) => { var item = await db.PlaylistItems.SingleOrDefaultAsync(i => i.PlaylistId == playlistId && i.Id == itemId, ct); if (item is null) return Results.NotFound(); db.PlaylistItems.Remove(item); await db.SaveChangesAsync(ct); return Results.NoContent(); });
        music.MapPatch("/playlists/{playlistId:guid}/items/reorder", async (MusicDbContext db, Guid playlistId, ReorderRequest request, CancellationToken ct) => { var items = await db.PlaylistItems.Where(i => i.PlaylistId == playlistId).ToListAsync(ct); var order = request.ItemIds; if (order.Count != items.Count || order.Distinct().Count() != order.Count || items.Any(i => !order.Contains(i.Id))) return Results.BadRequest(new { error = "Order must include every playlist item exactly once." }); for (var index = 0; index < order.Count; index++) items.Single(i => i.Id == order[index]).Position = index; await db.SaveChangesAsync(ct); return Results.NoContent(); });
        return app;
    }

    private static bool ValidateRequest(PlaylistRequest request, out string error)
    {
        error = string.Empty;
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 256) { error = "Playlist name is required and must be 256 characters or fewer."; return false; }
        if (!request.IsSmart) return true;
        if (string.IsNullOrWhiteSpace(request.SmartQuery) || request.SmartQuery.Length > 4000) { error = "Smart playlists require a query of 4000 characters or fewer."; return false; }
        try { using var document = JsonDocument.Parse(request.SmartQuery); return ValidateRule(document.RootElement, 0, out error); } catch (JsonException) { error = "Smart query must be valid JSON."; return false; }
    }

    private static bool ValidateRule(JsonElement element, int depth, out string error)
    {
        error = string.Empty; if (depth > 8 || element.ValueKind != JsonValueKind.Object) { error = "Smart query nesting is too deep or invalid."; return false; }
        var hasCondition = false;
        foreach (var property in element.EnumerateObject())
        {
            if (property.Name is "all" or "any") { hasCondition = true; if (property.Value.ValueKind != JsonValueKind.Array || property.Value.GetArrayLength() == 0) { error = "all/any must contain rules."; return false; } foreach (var child in property.Value.EnumerateArray()) if (!ValidateRule(child, depth + 1, out error)) return false; }
            else if (property.Name == "not") { hasCondition = true; if (!ValidateRule(property.Value, depth + 1, out error)) return false; }
            else if (property.Name is "genre" or "artistContains" or "minYear" or "maxYear" or "minDuration" or "maxDuration") hasCondition = true;
            else if (property.Name is "sort" or "limit") { }
            else { error = $"Unsupported smart rule field '{property.Name}'."; return false; }
        }
        if (!hasCondition) { error = "Smart query needs at least one condition."; return false; }
        return true;
    }

    private static IQueryable<MusicTrack> ApplySmartQuery(IQueryable<MusicTrack> query, string? raw, int take, int skip)
    {
        if (string.IsNullOrWhiteSpace(raw)) return query.OrderBy(t => t.Title).Skip(skip).Take(take);
        using var document = JsonDocument.Parse(raw); query = ApplyRule(query, document.RootElement);
        var sort = document.RootElement.TryGetProperty("sort", out var sortElement) ? sortElement.GetString()?.ToLowerInvariant() : "title";
        query = sort switch { "duration" => query.OrderBy(t => t.DurationSeconds).ThenBy(t => t.Title), "year" => query.OrderByDescending(t => t.Year).ThenBy(t => t.Title), _ => query.OrderBy(t => t.Title) };
        return query.Skip(skip).Take(take);
    }

    private static IQueryable<MusicTrack> ApplyRule(IQueryable<MusicTrack> query, JsonElement rule)
    {
        if (rule.TryGetProperty("genre", out var genre) && genre.ValueKind == JsonValueKind.String) query = query.Where(t => t.Genre == genre.GetString());
        if (rule.TryGetProperty("artistContains", out var artist) && artist.ValueKind == JsonValueKind.String) query = query.Where(t => t.ArtistName != null && t.ArtistName.Contains(artist.GetString()!));
        if (rule.TryGetProperty("minYear", out var minYear) && minYear.TryGetInt32(out var min)) query = query.Where(t => t.Year >= min);
        if (rule.TryGetProperty("maxYear", out var maxYear) && maxYear.TryGetInt32(out var max)) query = query.Where(t => t.Year <= max);
        if (rule.TryGetProperty("minDuration", out var minDuration) && minDuration.TryGetDouble(out var minSeconds)) query = query.Where(t => t.DurationSeconds >= minSeconds);
        if (rule.TryGetProperty("maxDuration", out var maxDuration) && maxDuration.TryGetDouble(out var maxSeconds)) query = query.Where(t => t.DurationSeconds <= maxSeconds);
        if (rule.TryGetProperty("all", out var all) && all.ValueKind == JsonValueKind.Array) foreach (var child in all.EnumerateArray()) query = ApplyRule(query, child);
        if (rule.TryGetProperty("any", out var any) && any.ValueKind == JsonValueKind.Array) { var branches = any.EnumerateArray().Select(child => ApplyRule(query, child)); query = branches.Skip(1).Aggregate(branches.First(), (left, right) => left.Concat(right)); }
        if (rule.TryGetProperty("not", out var not)) { var excluded = ApplyRule(query, not).Select(t => t.Id); query = query.Where(t => !excluded.Contains(t.Id)); }
        return query;
    }

    public sealed record PlaylistRequest(string Name, string? Description, bool IsSmart, string? SmartQuery);
    public sealed record PlaylistItemRequest(Guid TrackId, int? Position);
    public sealed record ReorderRequest(IReadOnlyList<Guid> ItemIds);
    public sealed record PlaylistSummary(Guid Id, string Name, string? Description, bool IsSmart, DateTimeOffset UpdatedAtUtc);
    public sealed record PlaylistDetail(Guid Id, string Name, string? Description, bool IsSmart, string? SmartQuery, IReadOnlyList<TrackSummary> Tracks);
    public sealed record TrackSummary(Guid Id, string Title, string? ArtistName, string AlbumTitle, double? DurationSeconds, string? Genre);
}
