using System.Data.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using oniDash.Application.Common;
using oniDash.Books.Reader;
using oniDash.Core.Domain;
using oniDash.Infrastructure.Persistence;

namespace oniDash.Api.Endpoints;

public static class BooksEndpoints
{
    private static readonly HashSet<string> BookExtensions = new(StringComparer.OrdinalIgnoreCase) { ".epub", ".epub3", ".pdf", ".mobi", ".azw", ".azw3", ".fb2", ".cbz", ".cbr", ".cb7" };

    /// <summary>
    /// Resolves an indexed file row to an absolute path (source root + scanner-relative
    /// path, re-validated to stay inside the root). MediaFile rows carry only relative
    /// paths; the source root lookup is what keeps this path-safe.
    /// </summary>
    private static async Task<string?> ResolveAbsolutePathAsync(OniDashDbContext db, MediaFile file, CancellationToken ct)
    {
        var source = await db.Sources.AsNoTracking().SingleOrDefaultAsync(s => s.Id == file.LibrarySourceId, ct).ConfigureAwait(false);
        return source is null
            ? null
            : PathSafety.TryResolveChildPath(source.RootPath, file.RelativePath, out var absolutePath) ? absolutePath : null;
    }

    public static IEndpointRouteBuilder MapBooksEndpoints(this IEndpointRouteBuilder app)
    {
        var books = app.MapGroup("/api/books").WithTags("Books");
        books.MapGet("/", async (OniDashDbContext db, Guid libraryId, bool? read, string? author, string? series, int? limit, CancellationToken ct) => 
        { 
            await EnsureStateTable(db, ct); 
            var list = await LoadBooks(db, libraryId, read, limit ?? 500, ct);
            if (!string.IsNullOrWhiteSpace(author))
                list = list.Where(b => string.Equals(b.Author, author, StringComparison.OrdinalIgnoreCase)).ToList();
            if (!string.IsNullOrWhiteSpace(series))
                list = list.Where(b => string.Equals(b.Series, series, StringComparison.OrdinalIgnoreCase)).ToList();
            return Results.Ok(list); 
        });

        books.MapGet("/continue", async (OniDashDbContext db, Guid libraryId, int? limit, CancellationToken ct) => 
        { 
            await EnsureStateTable(db, ct); 
            var items = await LoadBooks(db, libraryId, null, 500, ct); 
            return Results.Ok(items.Where(x => !x.Read && x.ProgressPages is > 0).OrderByDescending(x => x.LastReadAtUtc).Take(Math.Clamp(limit ?? 12, 1, 100))); 
        });

        books.MapGet("/authors", async (OniDashDbContext db, Guid libraryId, CancellationToken ct) =>
        {
            await EnsureStateTable(db, ct);
            var items = await LoadBooks(db, libraryId, null, 2000, ct);
            var authors = items
                .Where(b => !string.IsNullOrWhiteSpace(b.Author))
                .GroupBy(b => b.Author!)
                .Select(g => new BookAuthorDto(g.Key, g.Count()))
                .OrderBy(a => a.Name)
                .ToList();
            return Results.Ok(authors);
        });

        books.MapGet("/series", async (OniDashDbContext db, Guid libraryId, CancellationToken ct) =>
        {
            await EnsureStateTable(db, ct);
            var items = await LoadBooks(db, libraryId, null, 2000, ct);
            var seriesList = items
                .Where(b => !string.IsNullOrWhiteSpace(b.Series))
                .GroupBy(b => b.Series!)
                .Select(g => new BookSeriesDto(g.Key, g.Count()))
                .OrderBy(s => s.Title)
                .ToList();
            return Results.Ok(seriesList);
        });

        books.MapGet("/health", async (OniDashDbContext db, Guid? libraryId, CancellationToken ct) =>
        {
            await EnsureStateTable(db, ct);
            var query = db.MediaItems.AsNoTracking()
                .Include(x => x.Files)
                .Include(x => x.Artwork)
                .Where(x => x.Files.Any(f => BookExtensions.Contains(f.Extension)));

            if (libraryId is not null)
            {
                query = query.Where(x => x.LibraryId == libraryId.Value);
            }

            var items = await query.ToListAsync(ct);
            var states = await ReadStates(db, items.Select(x => x.Id).ToArray(), ct);

            var totalBooks = items.Count;
            var missingCovers = items.Count(x => !x.Artwork.Any());
            var sourceRoots = await db.Sources.AsNoTracking().ToDictionaryAsync(s => s.Id, s => s.RootPath, ct);
            bool IsMissing(MediaFile f)
            {
                if (f.MissingSinceUtc != null) return true;
                if (!sourceRoots.TryGetValue(f.LibrarySourceId, out var root) || !PathSafety.TryResolveChildPath(root, f.RelativePath, out var absolutePath)) return true;
                return !File.Exists(absolutePath);
            }
            var missingFiles = items.Sum(x => x.Files.Count(IsMissing));
            var missingMetadata = items.Count(x =>
            {
                var s = states.GetValueOrDefault(x.Id);
                return string.IsNullOrWhiteSpace(s?.Author) && string.IsNullOrWhiteSpace(s?.Series) && (s?.PageCount == null || s.PageCount == 0);
            });

            var report = new BookHealthReportDto(
                totalBooks,
                missingCovers,
                missingFiles,
                missingMetadata,
                DateTimeOffset.UtcNow
            );

            return Results.Ok(report);
        });

        books.MapPost("/{id:guid}/read", async (OniDashDbContext db, Guid id, BookReadRequest request, CancellationToken ct) => await Mutate(db, id, read: request.Read, ct: ct));
        books.MapPost("/{id:guid}/favorite", async (OniDashDbContext db, Guid id, BookFavoriteRequest request, CancellationToken ct) => await Mutate(db, id, favorite: request.Favorite, ct: ct));
        books.MapPost("/{id:guid}/progress", async (OniDashDbContext db, Guid id, BookProgressRequest request, CancellationToken ct) => await Mutate(db, id, progressPages: Math.Max(0, request.ProgressPages), lastReadAtUtc: DateTimeOffset.UtcNow, ct: ct));
        books.MapPost("/{id:guid}/rating", async (OniDashDbContext db, Guid id, BookRatingRequest request, CancellationToken ct) => await Mutate(db, id, rating: Math.Clamp(request.Rating, 0, 5), ct: ct));
        books.MapPost("/reindex", async (OniDashDbContext db, Guid? libraryId, CancellationToken ct) => { var count = libraryId is { } lid ? await db.MediaItems.AsNoTracking().Where(x => x.LibraryId == lid).SelectMany(x => x.Files).CountAsync(x => BookExtensions.Contains(x.Extension), ct) : await db.Files.AsNoTracking().CountAsync(x => BookExtensions.Contains(x.Extension), ct); return Results.Ok(new { indexedBooks = count }); });
        
        books.MapGet("/{id:guid}/cover", async (OniDashDbContext db, Guid id, CancellationToken ct) => 
        { 
            var artwork = await db.Artwork.AsNoTracking().FirstOrDefaultAsync(x => x.MediaItemId == id && (x.Kind == ArtworkKind.Poster || x.Kind == ArtworkKind.Thumbnail), ct); 
            if (artwork is null) return Results.NotFound(); 
            if (Uri.TryCreate(artwork.SourcePath, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https") return Results.Redirect(artwork.SourcePath); 
            return FileResultIfExists(artwork.SourcePath); 
        });

        // Reader Endpoints
        books.MapGet("/{id:guid}/manifest", async (OniDashDbContext db, IBookReaderService reader, Guid id, CancellationToken ct) =>
        {
            var file = await db.MediaItems.AsNoTracking().Where(x => x.Id == id).SelectMany(x => x.Files).OrderBy(x => x.RelativePath.Length).FirstOrDefaultAsync(ct);
            var absolutePath = file is null ? null : await ResolveAbsolutePathAsync(db, file, ct);
            if (absolutePath is null || !File.Exists(absolutePath)) return Results.NotFound();
            var manifest = await reader.GetManifestAsync(absolutePath, ct);
            return Results.Ok(manifest);
        });

        books.MapGet("/{id:guid}/pages/{pageNumber:int}", async (OniDashDbContext db, IBookReaderService reader, Guid id, int pageNumber, CancellationToken ct) =>
        {
            var file = await db.MediaItems.AsNoTracking().Where(x => x.Id == id).SelectMany(x => x.Files).OrderBy(x => x.RelativePath.Length).FirstOrDefaultAsync(ct);
            var absolutePath = file is null ? null : await ResolveAbsolutePathAsync(db, file, ct);
            if (absolutePath is null || !File.Exists(absolutePath)) return Results.NotFound();
            var page = await reader.GetPageAsync(absolutePath, pageNumber, ct);
            return page is null ? Results.NotFound() : Results.File(page.Data, page.ContentType);
        });

        books.MapGet("/{id:guid}/stream", async (OniDashDbContext db, Guid id, CancellationToken ct) =>
        {
            var file = await db.MediaItems.AsNoTracking().Where(x => x.Id == id).SelectMany(x => x.Files).OrderBy(x => x.RelativePath.Length).FirstOrDefaultAsync(ct);
            var absolutePath = file is null ? null : await ResolveAbsolutePathAsync(db, file, ct);
            if (absolutePath is null || !File.Exists(absolutePath)) return Results.NotFound();
            return Results.File(absolutePath, GetMimeType(absolutePath), enableRangeProcessing: true);
        });

        // Bookmarks
        books.MapGet("/{id:guid}/bookmarks", async (OniDashDbContext db, Guid id, CancellationToken ct) =>
        {
            await EnsureBookmarksTable(db, ct);
            var connection = db.Database.GetDbConnection();
            await EnsureOpen(connection, ct);
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT id, page_number, title, note, created_at_utc FROM book_bookmarks WHERE book_id = @bid ORDER BY page_number ASC";
            cmd.Parameters.Add(new SqliteParameter("@bid", id.ToString()));
            await using var rdr = await cmd.ExecuteReaderAsync(ct);
            var list = new List<BookmarkDto>();
            while (await rdr.ReadAsync(ct))
            {
                list.Add(new BookmarkDto(Guid.Parse(rdr.GetString(0)), rdr.GetInt32(1), rdr.IsDBNull(2) ? null : rdr.GetString(2), rdr.IsDBNull(3) ? null : rdr.GetString(3), DateTimeOffset.Parse(rdr.GetString(4))));
            }
            return Results.Ok(list);
        });

        books.MapPost("/{id:guid}/bookmarks", async (OniDashDbContext db, Guid id, CreateBookmarkRequest req, CancellationToken ct) =>
        {
            await EnsureBookmarksTable(db, ct);
            var bookmarkId = Guid.NewGuid();
            var now = DateTimeOffset.UtcNow;
            var connection = db.Database.GetDbConnection();
            await EnsureOpen(connection, ct);
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "INSERT INTO book_bookmarks (id, book_id, page_number, title, note, created_at_utc) VALUES (@id, @bid, @page, @title, @note, @created)";
            cmd.Parameters.Add(new SqliteParameter("@id", bookmarkId.ToString()));
            cmd.Parameters.Add(new SqliteParameter("@bid", id.ToString()));
            cmd.Parameters.Add(new SqliteParameter("@page", req.PageNumber));
            cmd.Parameters.Add(new SqliteParameter("@title", (object?)req.Title ?? DBNull.Value));
            cmd.Parameters.Add(new SqliteParameter("@note", (object?)req.Note ?? DBNull.Value));
            cmd.Parameters.Add(new SqliteParameter("@created", now.ToString("O")));
            await cmd.ExecuteNonQueryAsync(ct);
            return Results.Ok(new BookmarkDto(bookmarkId, req.PageNumber, req.Title, req.Note, now));
        });

        books.MapDelete("/{id:guid}/bookmarks/{bookmarkId:guid}", async (OniDashDbContext db, Guid id, Guid bookmarkId, CancellationToken ct) =>
        {
            await EnsureBookmarksTable(db, ct);
            var connection = db.Database.GetDbConnection();
            await EnsureOpen(connection, ct);
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "DELETE FROM book_bookmarks WHERE id = @id AND book_id = @bid";
            cmd.Parameters.Add(new SqliteParameter("@id", bookmarkId.ToString()));
            cmd.Parameters.Add(new SqliteParameter("@bid", id.ToString()));
            await cmd.ExecuteNonQueryAsync(ct);
            return Results.NoContent();
        });

        return app;
    }

    private static async Task<IResult> Mutate(OniDashDbContext db, Guid id, bool? read = null, bool? favorite = null, int? progressPages = null, int? rating = null, DateTimeOffset? lastReadAtUtc = null, CancellationToken ct = default)
    {
        await EnsureStateTable(db, ct); 
        var current = (await ReadStates(db, new[] { id }, ct)).GetValueOrDefault(id) ?? new BookState(false, null, null, false, null, null, null, null);
        await UpsertState(db, id, read ?? current.Read, progressPages ?? current.ProgressPages, rating ?? current.Rating, favorite ?? current.Favorite, lastReadAtUtc ?? current.LastReadAtUtc, current.PageCount, current.Author, current.Series, ct);
        var item = await db.MediaItems.AsNoTracking().Include(x => x.Files).Include(x => x.Artwork).FirstOrDefaultAsync(x => x.Id == id, ct);
        return item is null ? Results.NotFound() : Results.Ok(ToDto(item, (await ReadStates(db, new[] { id }, ct)).GetValueOrDefault(id)));
    }

    private static async Task<List<BookDto>> LoadBooks(OniDashDbContext db, Guid libraryId, bool? read, int limit, CancellationToken ct)
    {
        var items = await db.MediaItems.AsNoTracking().Include(x => x.Files).Include(x => x.Artwork).Where(x => x.LibraryId == libraryId && x.Files.Any(f => BookExtensions.Contains(f.Extension))).OrderBy(x => x.DisplayName).Take(Math.Clamp(limit, 1, 2000)).ToListAsync(ct);
        var states = await ReadStates(db, items.Select(x => x.Id).ToArray(), ct); 
        return items.Select(x => ToDto(x, states.GetValueOrDefault(x.Id))).Where(x => read is null || x.Read == read.Value).ToList();
    }

    private static BookDto ToDto(MediaItem item, BookState? state)
    {
        var file = item.Files.OrderBy(x => x.RelativePath.Length).FirstOrDefault(); 
        var pageCount = state?.PageCount; 
        var progress = state?.ProgressPages;
        double? percent = pageCount is > 0 && progress is >= 0 ? Math.Clamp((double)progress.Value / pageCount.Value * 100, 0, 100) : null;
        var author = state?.Author;
        var series = state?.Series;
        return new BookDto(item.Id.ToString(), item.Id.ToString(), item.DisplayName, author, null, pageCount, file?.Extension.TrimStart('.').ToUpperInvariant(), item.Artwork.Count > 0, state?.Read ?? false, progress, percent, state?.LastReadAtUtc, state?.Rating, state?.Favorite ?? false, series);
    }

    private static async Task<Dictionary<Guid, BookState>> ReadStates(OniDashDbContext db, Guid[] ids, CancellationToken ct)
    {
        if (ids.Length == 0) return new(); 
        var connection = db.Database.GetDbConnection(); 
        await EnsureOpen(connection, ct); 
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, read, progress_pages, rating, favorite, last_read_at_utc, page_count, author, series FROM book_catalog_state WHERE id IN (" + string.Join(',', ids.Select((_, i) => "@p" + i)) + ")";
        for (var i = 0; i < ids.Length; i++) command.Parameters.Add(new SqliteParameter("@p" + i, ids[i].ToString())); 
        await using var reader = await command.ExecuteReaderAsync(ct); 
        var result = new Dictionary<Guid, BookState>();
        while (await reader.ReadAsync(ct)) 
            result[Guid.Parse(reader.GetString(0))] = new BookState(reader.GetInt64(1) != 0, reader.IsDBNull(2) ? null : reader.GetInt32(2), reader.IsDBNull(3) ? null : reader.GetInt32(3), reader.GetInt64(4) != 0, reader.IsDBNull(5) ? null : DateTimeOffset.Parse(reader.GetString(5)), reader.IsDBNull(6) ? null : reader.GetInt32(6), reader.IsDBNull(7) ? null : reader.GetString(7), reader.IsDBNull(8) ? null : reader.GetString(8));
        return result;
    }

    private static async Task UpsertState(OniDashDbContext db, Guid id, bool read, int? progressPages, int? rating, bool favorite, DateTimeOffset? lastReadAtUtc, int? pageCount, string? author, string? series, CancellationToken ct)
    {
        var connection = db.Database.GetDbConnection(); 
        await EnsureOpen(connection, ct); 
        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO book_catalog_state(id, read, progress_pages, rating, favorite, last_read_at_utc, page_count, author, series) VALUES(@id,@read,@progress,@rating,@favorite,@last,@pages,@author,@series) ON CONFLICT(id) DO UPDATE SET read=@read, progress_pages=@progress, rating=@rating, favorite=@favorite, last_read_at_utc=@last, page_count=@pages, author=COALESCE(@author, author), series=COALESCE(@series, series)";
        command.Parameters.Add(new SqliteParameter("@id", id.ToString())); 
        command.Parameters.Add(new SqliteParameter("@read", read ? 1 : 0)); 
        command.Parameters.Add(new SqliteParameter("@progress", (object?)progressPages ?? DBNull.Value)); 
        command.Parameters.Add(new SqliteParameter("@rating", (object?)rating ?? DBNull.Value)); 
        command.Parameters.Add(new SqliteParameter("@favorite", favorite ? 1 : 0)); 
        command.Parameters.Add(new SqliteParameter("@last", (object?)lastReadAtUtc?.ToString("O") ?? DBNull.Value)); 
        command.Parameters.Add(new SqliteParameter("@pages", (object?)pageCount ?? DBNull.Value)); 
        command.Parameters.Add(new SqliteParameter("@author", (object?)author ?? DBNull.Value));
        command.Parameters.Add(new SqliteParameter("@series", (object?)series ?? DBNull.Value));
        await command.ExecuteNonQueryAsync(ct);
    }

    private static async Task EnsureStateTable(OniDashDbContext db, CancellationToken ct)
    {
        await db.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS book_catalog_state (id TEXT PRIMARY KEY, read INTEGER NOT NULL DEFAULT 0, progress_pages INTEGER NULL, rating INTEGER NULL, favorite INTEGER NOT NULL DEFAULT 0, last_read_at_utc TEXT NULL, page_count INTEGER NULL, author TEXT NULL, series TEXT NULL)", ct);

        // Columns added after the table first shipped. Existing columns are probed instead
        // of letting SQLite reject the ALTER: a swallowed SqliteException is still logged by
        // EF as a failed command, so every books request used to emit two error entries.
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var connection = db.Database.GetDbConnection();
        await EnsureOpen(connection, ct);
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT name FROM pragma_table_info('book_catalog_state')";
            await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
            while (await reader.ReadAsync(ct).ConfigureAwait(false))
            {
                columns.Add(reader.GetString(0));
            }
        }

        if (!columns.Contains("author")) await db.Database.ExecuteSqlRawAsync("ALTER TABLE book_catalog_state ADD COLUMN author TEXT NULL", ct);
        if (!columns.Contains("series")) await db.Database.ExecuteSqlRawAsync("ALTER TABLE book_catalog_state ADD COLUMN series TEXT NULL", ct);
    }

    private static Task EnsureBookmarksTable(OniDashDbContext db, CancellationToken ct) => 
        db.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS book_bookmarks (id TEXT PRIMARY KEY, book_id TEXT NOT NULL, page_number INTEGER NOT NULL, title TEXT NULL, note TEXT NULL, created_at_utc TEXT NOT NULL)", ct);

    private static async Task EnsureOpen(DbConnection connection, CancellationToken ct) { if (connection.State != System.Data.ConnectionState.Open) await connection.OpenAsync(ct); }
    private static IResult FileResultIfExists(string path) => File.Exists(path) ? Results.File(path, GetImageContentType(path)) : Results.NotFound();
    private static string GetImageContentType(string path) => Path.GetExtension(path).ToLowerInvariant() switch { ".jpg" or ".jpeg" => "image/jpeg", ".png" => "image/png", ".webp" => "image/webp", ".gif" => "image/gif", _ => "application/octet-stream" };
    private static string GetMimeType(string path) => Path.GetExtension(path).ToLowerInvariant() switch { ".epub" or ".epub3" => "application/epub+zip", ".pdf" => "application/pdf", ".cbz" => "application/vnd.comicbook+zip", _ => "application/octet-stream" };

    public sealed record BookReadRequest(bool Read); 
    public sealed record BookFavoriteRequest(bool Favorite); 
    public sealed record BookProgressRequest(int ProgressPages); 
    public sealed record BookRatingRequest(int Rating);
    public sealed record CreateBookmarkRequest(int PageNumber, string? Title, string? Note);
    public sealed record BookmarkDto(Guid Id, int PageNumber, string? Title, string? Note, DateTimeOffset CreatedAtUtc);
    public sealed record BookAuthorDto(string Name, int BookCount);
    public sealed record BookSeriesDto(string Title, int BookCount);
    public sealed record BookHealthReportDto(int TotalBooks, int MissingCovers, int MissingFiles, int MissingMetadata, DateTimeOffset GeneratedAtUtc);
    private sealed record BookState(bool Read, int? ProgressPages, int? Rating, bool Favorite, DateTimeOffset? LastReadAtUtc, int? PageCount, string? Author, string? Series);
    public sealed record BookDto(string Id, string MediaItemId, string Title, string? Author, int? Year, int? PageCount, string? Format, bool HasCover, bool Read, int? ProgressPages, double? ProgressPercent, DateTimeOffset? LastReadAtUtc, int? Rating, bool Favorite, string? Series);
}
