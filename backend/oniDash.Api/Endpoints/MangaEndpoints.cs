using System.Data.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using oniDash.Core.Domain;
using oniDash.Infrastructure.Persistence;

namespace oniDash.Api.Endpoints;

public static class MangaEndpoints
{
    private static readonly HashSet<string> MangaExtensions = new(StringComparer.OrdinalIgnoreCase) { ".cbz", ".cbr", ".cb7", ".zip", ".pdf", ".epub" };

    public static IEndpointRouteBuilder MapMangaEndpoints(this IEndpointRouteBuilder app)
    {
        var manga = app.MapGroup("/api/manga").WithTags("Manga");
        manga.MapGet("/library", async (OniDashDbContext db, int? limit, CancellationToken ct) => Results.Ok(await Load(db, limit ?? 500, ct)));
        manga.MapGet("/continue-reading", async (OniDashDbContext db, int? limit, CancellationToken ct) => { var items = await Load(db, 500, ct); return Results.Ok(items.Where(x => x.ProgressPercent > 0 && x.ProgressPercent < 100).OrderByDescending(x => x.LastReadAtUtc).Take(Math.Clamp(limit ?? 12, 1, 100))); });
        manga.MapGet("/updates", async (OniDashDbContext db, int? limit, CancellationToken ct) => { var items = await Load(db, 500, ct); return Results.Ok(items.OrderByDescending(x => x.UpdatedAtUtc).Take(Math.Clamp(limit ?? 50, 1, 200)).Select(x => new MangaUpdateDto(x.Id, x.Id, x.Title, x.CoverUrl, x.LatestChapter ?? "New", x.UpdatedAtUtc, x.SourceName, false, x.ProgressPercent >= 100))); });
        manga.MapGet("/categories", async (OniDashDbContext db, CancellationToken ct) => { var tags = await db.Tags.AsNoTracking().Where(t => t.Items.Any(i => i.MediaItem.Files.Any(f => MangaExtensions.Contains(f.Extension)))).Select(t => new { t.Id, t.Name, Count = t.Items.Count(i => i.MediaItem.Files.Any(f => MangaExtensions.Contains(f.Extension))) }).OrderByDescending(x => x.Count).ToListAsync(ct); return Results.Ok(tags.Select(x => new MangaCategoryDto(x.Id.ToString(), x.Name, x.Count, null))); });
        manga.MapGet("/sources", async (OniDashDbContext db, CancellationToken ct) => Results.Ok(await db.Sources.AsNoTracking().Select(x => new MangaSourceDto(x.Id.ToString(), x.Name, null, true, true, x.Files.Count(f => MangaExtensions.Contains(f.Extension)), null)).ToListAsync(ct)));
        manga.MapGet("/browse/popular", async (OniDashDbContext db, int? limit, CancellationToken ct) => Results.Ok((await Load(db, 500, ct)).OrderByDescending(x => x.ChapterCount).Take(Math.Clamp(limit ?? 24, 1, 100))));
        manga.MapGet("/search", async (OniDashDbContext db, string? q, int? limit, CancellationToken ct) => Results.Ok((await Load(db, 2000, ct)).Where(x => string.IsNullOrWhiteSpace(q) || x.Title.Contains(q, StringComparison.OrdinalIgnoreCase) || (x.Author?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false)).Take(Math.Clamp(limit ?? 50, 1, 500))));
        manga.MapPut("/{id:guid}/favorite", async (OniDashDbContext db, Guid id, FavoriteRequest request, CancellationToken ct) => await Mutate(db, id, favorite: request.Favorite, ct: ct));
        manga.MapPut("/{id:guid}/status", async (OniDashDbContext db, Guid id, StatusRequest request, CancellationToken ct) => await Mutate(db, id, status: request.Status, ct: ct));
        manga.MapPut("/{id:guid}/progress", async (OniDashDbContext db, Guid id, ProgressRequest request, CancellationToken ct) => await Mutate(db, id, progress: request.Chapter, lastReadAtUtc: DateTimeOffset.UtcNow, ct: ct));
        manga.MapPost("/{id:guid}/download", async (OniDashDbContext db, Guid id, DownloadRequest request, CancellationToken ct) => await Mutate(db, id, downloadCountDelta: request.Chapters.Count, ct: ct));
        manga.MapPost("/update", () => Results.Ok(new { accepted = true }));
        manga.MapPost("/sources/install", (InstallSourceRequest request) => Results.Ok(new { installed = true, sourceId = request.SourceId }));
        return app;
    }

    private static async Task<List<MangaDto>> Load(OniDashDbContext db, int limit, CancellationToken ct)
    {
        await EnsureStateTable(db, ct);
        var items = await db.MediaItems.AsNoTracking().Include(x => x.Files).Include(x => x.Artwork).Include(x => x.Tags).ThenInclude(x => x.Tag).Where(x => x.Files.Any(f => MangaExtensions.Contains(f.Extension))).OrderBy(x => x.DisplayName).Take(Math.Clamp(limit, 1, 2000)).ToListAsync(ct);
        var states = await ReadStates(db, items.Select(x => x.Id).ToArray(), ct); return items.Select(x => ToDto(x, states.GetValueOrDefault(x.Id))).ToList();
    }

    private static MangaDto ToDto(MediaItem item, MangaState? state)
    {
        var chapters = item.Files.Count; var progress = state?.ProgressPercent ?? 0; var read = chapters == 0 ? 0 : (int)Math.Round(chapters * progress / 100); var cover = item.Artwork.FirstOrDefault()?.SourcePath;
        return new MangaDto(item.Id.ToString(), item.DisplayName, null, null, null, cover, null, item.Tags.Select(x => x.Tag.Name).ToArray(), state?.Status ?? "plan_to_read", chapters, Math.Max(0, chapters - read), read, state?.LastReadAtUtc, null, state?.Favorite ?? false, true, state?.DownloadCount ?? 0, progress, null, DateTimeOffset.UtcNow);
    }

    private static async Task<IResult> Mutate(OniDashDbContext db, Guid id, bool? favorite = null, string? status = null, string? progress = null, DateTimeOffset? lastReadAtUtc = null, int downloadCountDelta = 0, CancellationToken ct = default)
    {
        await EnsureStateTable(db, ct); var current = (await ReadStates(db, new[] { id }, ct)).GetValueOrDefault(id) ?? new MangaState(false, "plan_to_read", 0, null, 0); var nextProgress = progress is null ? current.ProgressPercent : Math.Min(100, current.ProgressPercent + 10);
        await UpsertState(db, id, favorite ?? current.Favorite, status ?? current.Status, nextProgress, lastReadAtUtc ?? current.LastReadAtUtc, current.DownloadCount + downloadCountDelta, ct);
        var item = await db.MediaItems.AsNoTracking().Include(x => x.Files).Include(x => x.Artwork).Include(x => x.Tags).ThenInclude(x => x.Tag).FirstOrDefaultAsync(x => x.Id == id, ct);
        return item is null ? Results.NotFound() : Results.Ok(ToDto(item, (await ReadStates(db, new[] { id }, ct)).GetValueOrDefault(id)));
    }

    private static async Task<Dictionary<Guid, MangaState>> ReadStates(OniDashDbContext db, Guid[] ids, CancellationToken ct)
    {
        if (ids.Length == 0) return new(); var connection = db.Database.GetDbConnection(); await EnsureOpen(connection, ct); await using var command = connection.CreateCommand(); command.CommandText = "SELECT id, favorite, status, progress_percent, last_read_at_utc, download_count FROM manga_catalog_state WHERE id IN (" + string.Join(',', ids.Select((_, i) => "@p" + i)) + ")"; for (var i = 0; i < ids.Length; i++) command.Parameters.Add(new SqliteParameter("@p" + i, ids[i].ToString())); await using var reader = await command.ExecuteReaderAsync(ct); var result = new Dictionary<Guid, MangaState>(); while (await reader.ReadAsync(ct)) result[Guid.Parse(reader.GetString(0))] = new MangaState(reader.GetInt64(1) != 0, reader.GetString(2), reader.GetDouble(3), reader.IsDBNull(4) ? null : DateTimeOffset.Parse(reader.GetString(4)), reader.GetInt32(5)); return result;
    }

    private static async Task UpsertState(OniDashDbContext db, Guid id, bool favorite, string status, double progress, DateTimeOffset? lastReadAtUtc, int downloadCount, CancellationToken ct)
    {
        var connection = db.Database.GetDbConnection(); await EnsureOpen(connection, ct); await using var command = connection.CreateCommand(); command.CommandText = "INSERT INTO manga_catalog_state(id, favorite, status, progress_percent, last_read_at_utc, download_count) VALUES(@id,@favorite,@status,@progress,@last,@downloads) ON CONFLICT(id) DO UPDATE SET favorite=@favorite,status=@status,progress_percent=@progress,last_read_at_utc=@last,download_count=@downloads"; command.Parameters.Add(new SqliteParameter("@id", id.ToString())); command.Parameters.Add(new SqliteParameter("@favorite", favorite ? 1 : 0)); command.Parameters.Add(new SqliteParameter("@status", status)); command.Parameters.Add(new SqliteParameter("@progress", progress)); command.Parameters.Add(new SqliteParameter("@last", (object?)lastReadAtUtc?.ToString("O") ?? DBNull.Value)); command.Parameters.Add(new SqliteParameter("@downloads", downloadCount)); await command.ExecuteNonQueryAsync(ct);
    }

    private static Task EnsureStateTable(OniDashDbContext db, CancellationToken ct) => db.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS manga_catalog_state (id TEXT PRIMARY KEY, favorite INTEGER NOT NULL DEFAULT 0, status TEXT NOT NULL DEFAULT 'plan_to_read', progress_percent REAL NOT NULL DEFAULT 0, last_read_at_utc TEXT NULL, download_count INTEGER NOT NULL DEFAULT 0)", ct);
    private static async Task EnsureOpen(DbConnection connection, CancellationToken ct) { if (connection.State != System.Data.ConnectionState.Open) await connection.OpenAsync(ct); }

    public sealed record FavoriteRequest(bool Favorite); public sealed record StatusRequest(string Status); public sealed record ProgressRequest(string Chapter); public sealed record DownloadRequest(List<string> Chapters); public sealed record InstallSourceRequest(string SourceId);
    public sealed record MangaCategoryDto(string Id, string Name, int Count, string? Color); public sealed record MangaSourceDto(string Id, string Name, string? Language, bool Installed, bool Enabled, int MangaCount, string? IconUrl); public sealed record MangaUpdateDto(string Id, string MangaId, string MangaTitle, string? CoverUrl, string Chapter, DateTimeOffset UpdatedAtUtc, string? SourceName, bool Downloaded, bool Read);
    public sealed record MangaDto(string Id, string Title, string? AltTitle, string? Author, string? Artist, string? Description, string? CoverUrl, string[] Genres, string Status, int ChapterCount, int UnreadCount, int ReadCount, DateTimeOffset? LastReadAtUtc, string? LatestChapter, double? Rating, bool Favorite, bool InLibrary, int DownloadCount, double ProgressPercent, int? Year, DateTimeOffset UpdatedAtUtc);
    private sealed record MangaState(bool Favorite, string Status, double ProgressPercent, DateTimeOffset? LastReadAtUtc, int DownloadCount);
}
