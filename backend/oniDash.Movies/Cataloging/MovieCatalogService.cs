using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using oniDash.Movies.Domain;
using oniDash.Movies.Persistence;
using oniDash.Movies.Probing;

namespace oniDash.Movies.Cataloging;

/// <summary>
/// Builds and maintains the movie catalogue from indexed video files. Movies anchor
/// 1:1 to the scan's media items, so re-scans and re-indexing converge on the same
/// rows (user tags/collections on the items are never disturbed). Per-file failures
/// are logged and skipped — a bad file never blocks the rest.
/// </summary>
public sealed class MovieCatalogService(
    MoviesDbContext dbContext,
    IVideoProbeReader probeReader,
    IVideoArtworkReader artworkReader,
    ILogger<MovieCatalogService> logger)
{
    /// <summary>Extensions the catalogue understands; everything else is ignored.</summary>
    public static readonly string[] VideoExtensions =
    [
        ".mp4", ".m4v", ".mkv", ".avi", ".mov", ".wmv", ".webm", ".mpg", ".mpeg", ".ts", ".flv",
    ];

    public static bool VideoExtensionsMatch(string extension)
    {
        var normalized = extension.StartsWith('.') ? extension : $".{extension}";
        return VideoExtensions.Contains(normalized, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<bool> IndexFileAsync(
        Guid libraryId,
        Guid mediaItemId,
        Guid fileId,
        string absolutePath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!VideoExtensionsMatch(Path.GetExtension(absolutePath)))
            {
                return false;
            }

            var probe = await probeReader.ProbeAsync(absolutePath, cancellationToken).ConfigureAwait(false);
            var artwork = await artworkReader.ReadAsync(absolutePath, cancellationToken).ConfigureAwait(false);
            var nameInfo = MovieNameParser.Parse(absolutePath);

            var movie = await dbContext.Movies
                .SingleOrDefaultAsync(m => m.MediaItemId == mediaItemId, cancellationToken)
                .ConfigureAwait(false);
            if (movie is null)
            {
                movie = new Movie { Id = Guid.NewGuid(), MediaItemId = mediaItemId };
                dbContext.Movies.Add(movie);
            }

            movie.LibraryId = libraryId;
            movie.FileId = fileId;
            movie.Title = nameInfo.Title;
            movie.NormalizedTitle = Normalize(nameInfo.Title);
            movie.Year = nameInfo.Year;
            movie.DurationSeconds = probe?.DurationSeconds;
            movie.Container = Path.GetExtension(absolutePath).ToLowerInvariant();
            if (movie.PosterBlob is null && artwork is not null)
            {
                movie.PosterBlob = artwork.Bytes;
                movie.PosterContentType = artwork.ContentType;
            }

            movie.UpdatedAtUtc = DateTimeOffset.UtcNow;

            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to index video file {Path}; skipping", absolutePath);
            return false;
        }
    }

    /// <summary>Saves the resume position for a movie; position is never negative.</summary>
    public async Task<bool> SaveProgressAsync(Guid movieId, double positionSeconds, CancellationToken cancellationToken = default)
    {
        var movie = await dbContext.Movies.FindAsync([movieId], cancellationToken).ConfigureAwait(false);
        if (movie is null)
        {
            return false;
        }

        movie.WatchProgressSeconds = Math.Max(0, positionSeconds);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    /// <summary>
    /// Marks a movie watched (recording when) or unwatched. Either way the resume
    /// position resets: a finished movie offers no resume, and re-watching starts
    /// fresh.
    /// </summary>
    public async Task<bool> SetWatchedAsync(Guid movieId, bool watched, CancellationToken cancellationToken = default)
    {
        var movie = await dbContext.Movies.FindAsync([movieId], cancellationToken).ConfigureAwait(false);
        if (movie is null)
        {
            return false;
        }

        movie.Watched = watched;
        movie.WatchedAtUtc = watched ? DateTimeOffset.UtcNow : null;
        movie.WatchProgressSeconds = null;

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    /// <summary>Case/whitespace-insensitive ordering key.</summary>
    internal static string Normalize(string? value) =>
        (value ?? string.Empty).Trim().ToLowerInvariant();
}
