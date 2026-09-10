using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using oniDash.Application.Search;
using oniDash.Core.Domain;
using oniDash.Infrastructure.Persistence;

namespace oniDash.Infrastructure.Search;

/// <summary>SQLite FTS5-backed global search with safe prefix matching, library/media-type filtering, and bounded pagination.</summary>
public sealed class FtsSearchService(OniDashDbContext dbContext) : ISearchService
{
    private const int MaxLimit = 200;
    private const int MaxOffset = 10_000;

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(string query, Guid? libraryId = null, int limit = 50, CancellationToken cancellationToken = default, int offset = 0, MediaType? mediaType = null)
    {
        var match = BuildMatchExpression(query);
        if (match.Length == 0) return [];
        var effectiveLimit = Math.Clamp(limit, 1, MaxLimit);
        var effectiveOffset = Math.Clamp(offset, 0, MaxOffset);
        var extensions = mediaType is null ? Array.Empty<string>() : ExtensionsFor(mediaType.Value).ToArray();
        var extensionPlaceholders = string.Join(",", extensions.Select((_, index) => $"@extension{index}"));
        var mediaFilter = extensions.Length == 0
            ? mediaType is null ? string.Empty : "AND 1 = 0"
            : $"AND EXISTS (SELECT 1 FROM MediaFiles mf JOIN LibrarySources ls ON ls.Id = mf.LibrarySourceId WHERE mf.MediaItemId = i.Id AND ls.LibraryId = i.LibraryId AND LOWER(mf.Extension) IN ({extensionPlaceholders}))";
        var parameters = new List<SqliteParameter> { new("@match", match) };
        if (libraryId is not null) parameters.Add(new SqliteParameter("@libraryId", libraryId.Value));
        for (var index = 0; index < extensions.Length; index++) parameters.Add(new SqliteParameter($"@extension{index}", extensions[index]));
        parameters.Add(new SqliteParameter("@limit", effectiveLimit));
        parameters.Add(new SqliteParameter("@offset", effectiveOffset));
        var sql = $"""
            SELECT i.Id AS ItemId, i.DisplayName AS DisplayName, i.LibraryId AS LibraryId, l.Name AS LibraryName
            FROM MediaItemsFts f JOIN MediaItems i ON i.Id = f.ItemId JOIN Libraries l ON l.Id = i.LibraryId
            WHERE MediaItemsFts MATCH @match
            {(libraryId is null ? string.Empty : "AND i.LibraryId = @libraryId ")}
            {mediaFilter}
            ORDER BY bm25(MediaItemsFts), i.DisplayName LIMIT @limit OFFSET @offset
            """;
        var rows = await dbContext.Database.SqlQueryRaw<SearchRow>(sql, parameters.Cast<object>().ToArray()).ToListAsync(cancellationToken).ConfigureAwait(false);
        return rows.Select(row => new SearchResult(row.ItemId, row.DisplayName, row.LibraryId, row.LibraryName)).ToList();
    }

    private static IReadOnlyList<string> ExtensionsFor(MediaType type) => type switch
    {
        MediaType.Audio => [".mp3", ".flac", ".m4a", ".aac", ".ogg", ".wav"],
        MediaType.Video => [".mp4", ".mkv", ".avi", ".mov", ".webm"],
        MediaType.Book => [".epub", ".mobi", ".azw", ".azw3", ".fb2"],
        MediaType.Document => [".pdf"],
        MediaType.Comic => [".cbz", ".cbr", ".cb7"],
        _ => []
    };

    public async Task<int> ReindexAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.Database.ExecuteSqlRawAsync("DELETE FROM MediaItemsFts", cancellationToken).ConfigureAwait(false);
        await dbContext.Database.ExecuteSqlRawAsync("INSERT INTO MediaItemsFts(ItemId, LibraryId, DisplayName) SELECT Id, LibraryId, DisplayName FROM MediaItems", cancellationToken).ConfigureAwait(false);
        await using var command = dbContext.Database.GetDbConnection().CreateCommand(); command.CommandText = "SELECT COUNT(*) FROM MediaItemsFts";
        if (command.Connection?.State != System.Data.ConnectionState.Open) await command.Connection!.OpenAsync(cancellationToken).ConfigureAwait(false);
        var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false); return result is null ? 0 : Convert.ToInt32(result);
    }

    internal static string BuildMatchExpression(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return string.Empty;
        var terms = query.Split((char[]?)null, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Select(term => $"\"{term.Replace("\"", "\"\"")}\"*");
        return string.Join(" AND ", terms);
    }

    private sealed class SearchRow { public Guid ItemId { get; set; } public string DisplayName { get; set; } = string.Empty; public Guid LibraryId { get; set; } public string LibraryName { get; set; } = string.Empty; }
}
