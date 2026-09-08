using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using oniDash.Music.Persistence;

namespace oniDash.Music.Cataloging;

/// <summary>One candidate file row for the reindex pass (read from the shared database).</summary>
public sealed record ReindexCandidate(
    Guid LibraryId, Guid MediaItemId, Guid FileId, string RootPath, string RelativePath);

/// <summary>
/// Recovery pass: catalogues every audio file row that has no track yet (or whose file
/// changed after its track row was last written). Reads the core index tables through
/// raw SQL on the shared database — the plugin owns its own schema and never writes to
/// the core's.
/// </summary>
public sealed class MusicReindexService(MusicDbContext dbContext, MusicCatalogService catalog, ILogger<MusicReindexService> logger)
{
    private const string CandidateSql = """
        SELECT i.LibraryId AS LibraryId, f.MediaItemId AS MediaItemId, f.Id AS FileId,
               s.RootPath AS RootPath, f.RelativePath AS RelativePath
        FROM Files f
        JOIN Sources s ON s.Id = f.LibrarySourceId
        JOIN MediaItems i ON i.Id = f.MediaItemId
        WHERE ({LibraryFilter} f.Extension IN {Extensions})
          AND f.MissingSinceUtc IS NULL
          AND NOT EXISTS (
                SELECT 1 FROM Tracks t
                WHERE t.MediaItemId = f.MediaItemId AND t.UpdatedAtUtc >= f.LastWriteTimeUtc)
        """;

    public async Task<int> ReindexAsync(Guid? libraryId = null, CancellationToken cancellationToken = default)
    {
        var extensionList = string.Join(", ", MusicCatalogService.AudioExtensions
            .Select(extension => $"'{extension.ToLowerInvariant()}'"));
        // EF stores Guids as dashed UPPERCASE text in SQLite; match that exactly.
        var libraryFilter = libraryId is null
            ? string.Empty
            : $"i.LibraryId = '{libraryId.Value.ToString().ToUpperInvariant()}' AND ";

        var sql = CandidateSql
            .Replace("{LibraryFilter}", libraryFilter)
            .Replace("{Extensions}", $"({extensionList})");

        var candidates = await dbContext.Database
            .SqlQueryRaw<ReindexCandidate>(sql)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var indexed = 0;
        foreach (var candidate in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var absolutePath = Path.Combine(
                candidate.RootPath, candidate.RelativePath.Replace('/', Path.DirectorySeparatorChar));
            try
            {
                if (await catalog
                    .IndexFileAsync(candidate.LibraryId, candidate.MediaItemId, candidate.FileId, absolutePath, cancellationToken)
                    .ConfigureAwait(false))
                {
                    indexed++;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Reindex failed for {Path}; continuing", absolutePath);
            }
        }

        return indexed;
    }
}
