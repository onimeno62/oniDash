using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using oniDash.Application.Scanning;

namespace oniDash.Music.Cataloging;

/// <summary>
/// Scan hook: enriches newly indexed or changed audio files with embedded metadata.
/// Unchanged files are skipped (tags cannot have changed), non-audio extensions are
/// ignored, and per-file failures never fail the scan (rule 11: reads only).
/// </summary>
public sealed class MusicIndexedMediaHandler(
    MusicCatalogService catalog,
    ILogger<MusicIndexedMediaHandler> logger) : IIndexedMediaHandler
{
    public async Task HandleAsync(IndexedMediaContext context, CancellationToken cancellationToken = default)
    {
        if (context.ChangeKind == IndexedMediaChangeKind.Unchanged)
        {
            return;
        }

        if (!MusicCatalogService.AudioExtensionsMatch(context.File.Extension))
        {
            return;
        }

        try
        {
            await catalog
                .IndexFileAsync(
                    context.Source.LibraryId,
                    context.MediaItemId,
                    context.FileRowId,
                    context.AbsolutePath,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Music enrichment failed for {Path}; scan continues", context.AbsolutePath);
        }
    }
}
