using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using oniDash.Application.Scanning;

namespace oniDash.Movies.Cataloging;

/// <summary>
/// Scan hook: enriches newly indexed or changed video files with filename + probe
/// metadata. Unchanged files are skipped, non-video extensions are ignored, and
/// per-file failures never fail the scan (rule 11: reads only).
/// </summary>
public sealed class MovieIndexedMediaHandler(
    MovieCatalogService catalog,
    ILogger<MovieIndexedMediaHandler> logger) : IIndexedMediaHandler
{
    public async Task HandleAsync(IndexedMediaContext context, CancellationToken cancellationToken = default)
    {
        if (context.ChangeKind == IndexedMediaChangeKind.Unchanged)
        {
            return;
        }

        if (!MovieCatalogService.VideoExtensionsMatch(context.File.Extension))
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
            logger.LogWarning(ex, "Movie enrichment failed for {Path}; scan continues", context.AbsolutePath);
        }
    }
}
