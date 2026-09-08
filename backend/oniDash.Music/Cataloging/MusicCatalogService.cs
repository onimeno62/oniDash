using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using oniDash.Music.Domain;
using oniDash.Music.Persistence;
using oniDash.Music.Tagging;

namespace oniDash.Music.Cataloging;

/// <summary>
/// Builds and maintains the artist/album/track catalogue from embedded audio tags.
/// Tracks anchor 1:1 to the scan's media items, so re-scans and re-indexing converge
/// on the same rows (user tags/collections on the items are never disturbed). Per-file
/// failures are logged and skipped — a bad file never blocks the rest.
/// </summary>
public sealed class MusicCatalogService(
    MusicDbContext dbContext,
    IAudioTagReader tagReader,
    ILogger<MusicCatalogService> logger)
{
    /// <summary>Extensions the catalogue understands; everything else is ignored.</summary>
    public static readonly string[] AudioExtensions =
        [".mp3", ".m4a", ".mp4", ".aac", ".flac", ".ogg", ".opus", ".wav", ".wma"];

    public static bool AudioExtensionsMatch(string extension)
    {
        var normalized = extension.StartsWith('.') ? extension : $".{extension}";
        return AudioExtensions.Contains(normalized, StringComparer.OrdinalIgnoreCase);
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
            var tags = tagReader.Read(absolutePath);
            if (tags is null)
            {
                logger.LogDebug("No readable audio tags in {Path}; skipping", absolutePath);
                return false;
            }

            var track = await dbContext.Tracks
                .SingleOrDefaultAsync(t => t.MediaItemId == mediaItemId, cancellationToken)
                .ConfigureAwait(false);
            if (track is null)
            {
                track = new MusicTrack { Id = Guid.NewGuid(), MediaItemId = mediaItemId };
                dbContext.Tracks.Add(track);
            }

            var album = await ResolveAlbumAsync(libraryId, tags, cancellationToken).ConfigureAwait(false);
            var artist = await ResolveArtistAsync(libraryId, tags.TrackArtist, cancellationToken)
                .ConfigureAwait(false);

            track.LibraryId = libraryId;
            track.FileId = fileId;
            track.Title = tags.Title ?? TrackTitleFromFile(absolutePath);
            track.AlbumId = album?.Id;
            track.ArtistId = artist?.Id;
            track.ArtistName = tags.TrackArtist;
            track.TrackNumber = tags.TrackNumber;
            track.DiscNumber = tags.DiscNumber;
            track.Year = tags.Year;
            track.DurationSeconds = tags.DurationSeconds;
            track.Genre = tags.Genre;
            track.UpdatedAtUtc = DateTimeOffset.UtcNow;

            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to index audio file {Path}; skipping", absolutePath);
            return false;
        }
    }

    private async Task<MusicAlbum?> ResolveAlbumAsync(
        Guid libraryId, AudioTags tags, CancellationToken cancellationToken)
    {
        if (tags.Album is null)
        {
            return null;
        }

        var artistName = tags.AlbumArtist ?? tags.TrackArtist;
        var albumKey = $"{Normalize(tags.Album)}|{Normalize(artistName)}";
        var album = await dbContext.Albums
            .SingleOrDefaultAsync(a => a.AlbumKey == albumKey && a.LibraryId == libraryId, cancellationToken)
            .ConfigureAwait(false)
            // Entities added during this pass are not yet visible to database queries.
            ?? dbContext.Albums.Local.FirstOrDefault(a => a.AlbumKey == albumKey && a.LibraryId == libraryId);

        if (album is null)
        {
            var artist = await ResolveArtistAsync(libraryId, artistName, cancellationToken).ConfigureAwait(false);
            album = new MusicAlbum
            {
                Id = Guid.NewGuid(),
                LibraryId = libraryId,
                Title = tags.Album,
                NormalizedTitle = Normalize(tags.Album),
                AlbumKey = albumKey,
                ArtistName = artistName,
                ArtistId = artist?.Id,
                Year = tags.Year,
            };
            dbContext.Albums.Add(album);
        }
        else if (album.Year is null && tags.Year is not null)
        {
            album.Year = tags.Year;
        }

        if (album.CoverBlob is null && tags.CoverBytes is { Length: > 0 })
        {
            album.CoverBlob = tags.CoverBytes;
            album.CoverContentType = tags.CoverContentType ?? "image/jpeg";
        }

        return album;
    }

    private async Task<MusicArtist?> ResolveArtistAsync(
        Guid libraryId, string? name, CancellationToken cancellationToken)
    {
        if (name is null)
        {
            return null;
        }

        var normalizedName = Normalize(name);
        var artist = await dbContext.Artists
            .SingleOrDefaultAsync(a => a.NormalizedName == normalizedName && a.LibraryId == libraryId, cancellationToken)
            .ConfigureAwait(false)
            // Entities added during this pass are not yet visible to database queries.
            ?? dbContext.Artists.Local.FirstOrDefault(a => a.NormalizedName == normalizedName && a.LibraryId == libraryId);

        if (artist is null)
        {
            artist = new MusicArtist
            {
                Id = Guid.NewGuid(),
                LibraryId = libraryId,
                Name = name,
                NormalizedName = normalizedName,
            };
            dbContext.Artists.Add(artist);
        }

        return artist;
    }

    /// <summary>Case/whitespace-insensitive grouping key part.</summary>
    internal static string Normalize(string? value) =>
        (value ?? string.Empty).Trim().ToLowerInvariant();

    private static string TrackTitleFromFile(string absolutePath)
    {
        var name = Path.GetFileName(absolutePath);
        var dot = name.LastIndexOf('.');
        return dot > 0 ? name[..dot] : name;
    }
}
