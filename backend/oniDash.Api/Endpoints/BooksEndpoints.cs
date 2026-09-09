using System.Data.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using oniDash.Core.Domain;
using oniDash.Infrastructure.Persistence;

namespace oniDash.Api.Endpoints;

public static class BooksEndpoints
{
    private static readonly HashSet<string> BookExtensions = new(StringComparer.OrdinalIgnoreCase) { ".epub", ".epub3", ".pdf", ".mobi", ".azw", ".azw3", ".fb2", ".cbz", ".cbr", ".cb7" };

    public static IEndpointRouteBuilder MapBooksEndpoints(this IEndpointRouteBuilder app)
    {
        var books = app.MapGroup("/api/books").WithTags("Books");
        books.MapGet("/", async (OniDashDbContext db, Guid libraryId, bool? read, int? limit, CancellationToken ct) => { await EnsureStateTable(db, ct); return Results.Ok(await LoadBooks(db, libraryId, read, limit ?? 500, ct)); });
        books.MapGet("/continue", async (OniDashDbContext db, Guid libraryId, int? limit, CancellationToken ct) => { await EnsureStateTable(db, ct); var items = await LoadBooks(db, libraryId, null, 500, ct); return Results.Ok(items.Where(x => !x.Read && x.ProgressPages is > 0).OrderByDescending(x => x.LastReadAtUtc).Take(Math.Clamp(limit ?? 12, 1, 100))); });
        books.MapPost("/{id:guid}/read", async (OniDashDbContext db, Guid id, BookReadRequest request, CancellationToken ct) => await Mutate(db, id, read: request.Read, ct: ct));
        books.MapPost("/{id:guid}/favorite", async (OniDashDbContext db, Guid id, BookFavoriteRequest request, CancellationToken ct) => await Mutate(db, id, favorite: request.Favorite, ct: ct));
        books.MapPost("/{id:guid}/progress", async (OniDashDbContext db, Guid id, BookProgressRequest request, CancellationToken ct) => await Mutate(db, id, progressPages: Math.Max(0, request.ProgressPages), lastReadAtUtc: DateTimeOffset.UtcNow, ct: ct));
        books.MapPost("/{id:guid}/rating", async (OniDashDbContext db, Guid id, BookRatingRequest request, CancellationToken ct) => await Mutate(db, id, rating: Math.Clamp(request.Rating, 0, 5), ct: ct));
        books.MapPost("/reindex", async (OniDashDbContext db, Guid? libraryId, CancellationToken ct) => { var count = libraryId is { } id ? await db.MediaItems.AsNoTracking().Where(x => x.LibraryId == id).SelectMany(x => x.Files).CountAsync(x => BookExtensions.Contains(x.Extension), ct) : await db.Files.AsNoTracking().CountAsync(x => BookExtensions.Contains(x.Extension), ct); return Results.Ok(new { indexedBooks = count }); });
        books.MapGet("/{id:guid}/cover", async (OniDashDbContext db, Guid id, CancellationToken ct) => { var artwork = await db.Artwork.AsNoTracking().FirstOrDefaultAsync(x => x.MediaItemId == id && (x.Kind == ArtworkKind.Poster || x.Kind == ArtworkKind.Thumbnail), ct); if (artwork is null) return Results.NotFound(); if (Uri.TryCreate(artwork.SourcePath, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https") return Results.Redirect(artwork.SourcePath); return FileResultIfExists(artwork.SourcePath); });
        return app;
    }

    private static async Task<IResult> Mutate(OniDashDbContext db, Guid id, bool? read = null, bool? favorite = null, int? progressPages = null, int? rating = null, DateTimeOffset? lastReadAtUtc = null, CancellationToken ct = default)
    {
        await EnsureStateTable(db, ct); var current = (await ReadStates(db, new[] { id }, ct)).GetValueOrDefault(id) ?? new BookState(false, null, null, false, null, null);
        await UpsertState(db, id, read ?? current.Read, progressPages ?? current.ProgressPages, rating ?? current.Rating, favorite ?? current.Favorite, lastReadAtUtc ?? current.LastReadAtUtc, current.PageCount, ct);
        var item = await db.MediaItems.AsNoTracking().Include(x => x.Files).Include(x => x.Artwork).FirstOrDefaultAsync(x => x.Id == id, ct);
        return item is null ? Results.NotFound() : Results.Ok(ToDto(item, (await ReadStates(db, new[] { id }, ct)).GetValueOrDefault(id)));
    }

    private static async Task<List<BookDto>> LoadBooks(OniDashDbContext db, Guid libraryId, bool? read, int limit, CancellationToken ct)
    {
        var items = await db.MediaItems.AsNoTracking().Include(x => x.Files).Include(x => x.Artwork).Where(x => x.LibraryId == libraryId && x.Files.Any(f => BookExtensions.Contains(f.Extension))).OrderBy(x => x.DisplayName).Take(Math.Clamp(limit, 1, 2000)).ToListAsync(ct);
        var states = await ReadStates(db, items.Select(x => x.Id).ToArray(), ct); return items.Select(x => ToDto(x, states.GetValueOrDefault(x.Id))).Where(x => read is null || x.Read == read.Value).ToList();
    }

    private static BookDto ToDto(MediaItem item, BookState? state)
    {
        var file = item.Files.OrderBy(x => x.RelativePath.Length).FirstOrDefault(); var pageCount = state?.PageCount; var progress = state?.ProgressPages;
        double? percent = pageCount is > 0 && progress is >= 0 ? Math.Clamp((double)progress.Value / pageCount.Value * 100, 0, 100) : null;
        return new BookDto(item.Id.ToString(), item.Id.ToString(), item.DisplayName, null, null, pageCount, file?.Extension.TrimStart('.').ToUpperInvariant(), item.Artwork.Count > 0, state?.Read ?? false, progress, percent, state?.LastReadAtUtc, state?.Rating, state?.Favorite ?? false, null);
    }

    private static async Task<Dictionary<Guid, BookState>> ReadStates(OniDashDbContext db, Guid[] ids, CancellationToken ct)
    {
        if (ids.Length == 0) return new(); var connection = db.Database.GetDbConnection(); await EnsureOpen(connection, ct); await using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, read, progress_pages, rating, favorite, last_read_at_utc, page_count FROM book_catalog_state WHERE id IN (" + string.Join(',', ids.Select((_, i) => "@p" + i)) + ")";
        for (var i = 0; i < ids.Length; i++) command.Parameters.Add(new SqliteParameter("@p" + i, ids[i].ToString())); await using var reader = await command.ExecuteReaderAsync(ct); var result = new Dictionary<Guid, BookState>();
        while (await reader.ReadAsync(ct)) result[Guid.Parse(reader.GetString(0))] = new BookState(reader.GetInt64(1) != 0, reader.IsDBNull(2) ? null : reader.GetInt32(2), reader.IsDBNull(3) ? null : reader.GetInt32(3), reader.GetInt64(4) != 0, reader.IsDBNull(5) ? null : DateTimeOffset.Parse(reader.GetString(5)), reader.IsDBNull(6) ? null : reader.GetInt32(6));
        return result;
    }

    private static async Task UpsertState(OniDashDbContext db, Guid id, bool read, int? progressPages, int? rating, bool favorite, DateTimeOffset? lastReadAtUtc, int? pageCount, CancellationToken ct)
    {
        var connection = db.Database.GetDbConnection(); await EnsureOpen(connection, ct); await using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO book_catalog_state(id, read, progress_pages, rating, favorite, last_read_at_utc, page_count) VALUES(@id,@read,@progress,@rating,@favorite,@last,@pages) ON CONFLICT(id) DO UPDATE SET read=@read, progress_pages=@progress, rating=@rating, favorite=@favorite, last_read_at_utc=@last, page_count=@pages";
        command.Parameters.Add(new SqliteParameter("@id", id.ToString())); command.Parameters.Add(new SqliteParameter("@read", read ? 1 : 0)); command.Parameters.Add(new SqliteParameter("@progress", (object?)progressPages ?? DBNull.Value)); command.Parameters.Add(new SqliteParameter("@rating", (object?)rating ?? DBNull.Value)); command.Parameters.Add(new SqliteParameter("@favorite", favorite ? 1 : 0)); command.Parameters.Add(new SqliteParameter("@last", (object?)lastReadAtUtc?.ToString("O") ?? DBNull.Value)); command.Parameters.Add(new SqliteParameter("@pages", (object?)pageCount ?? DBNull.Value)); await command.ExecuteNonQueryAsync(ct);
    }

    private static Task EnsureStateTable(OniDashDbContext db, CancellationToken ct) => db.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS book_catalog_state (id TEXT PRIMARY KEY, read INTEGER NOT NULL DEFAULT 0, progress_pages INTEGER NULL, rating INTEGER NULL, favorite INTEGER NOT NULL DEFAULT 0, last_read_at_utc TEXT NULL, page_count INTEGER NULL)", ct);
    private static async Task EnsureOpen(DbConnection connection, CancellationToken ct) { if (connection.State != System.Data.ConnectionState.Open) await connection.OpenAsync(ct); }
    private static IResult FileResultIfExists(string path) => File.Exists(path) ? Results.File(path, GetImageContentType(path)) : Results.NotFound();
    private static string GetImageContentType(string path) => Path.GetExtension(path).ToLowerInvariant() switch { ".jpg" or ".jpeg" => "image/jpeg", ".png" => "image/png", ".webp" => "image/webp", ".gif" => "image/gif", _ => "application/octet-stream" };
    public sealed record BookReadRequest(bool Read); public sealed record BookFavoriteRequest(bool Favorite); public sealed record BookProgressRequest(int ProgressPages); public sealed record BookRatingRequest(int Rating);
    private sealed record BookState(bool Read, int? ProgressPages, int? Rating, bool Favorite, DateTimeOffset? LastReadAtUtc, int? PageCount);
    public sealed record BookDto(string Id, string MediaItemId, string Title, string? Author, int? Year, int? PageCount, string? Format, bool HasCover, bool Read, int? ProgressPages, double? ProgressPercent, DateTimeOffset? LastReadAtUtc, int? Rating, bool Favorite, string? Series);
}
