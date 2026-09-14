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

/// <summary>Builds the music catalogue from indexed audio files and embedded tags.</summary>
public sealed class MusicCatalogService(
    MusicDbContext dbContext,
    IAudioTagReader tagReader,
    ILogger<MusicCatalogService> logger)
{
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

            var now = DateTimeOffset.UtcNow;
            var track = await dbContext.Tracks
                .SingleOrDefaultAsync(t => t.MediaItemId == mediaItemId, cancellationToken)
                .ConfigureAwait(false);

            var isNew = track is null;
            if (track is null)
            {
                track = new MusicTrack
                {
                    Id = Guid.NewGuid(),
                    MediaItemId = mediaItemId,
                    AddedAtUtc = now,
                };
                dbContext.Tracks.Add(track);
            }

            var albumArtistName = tags.AlbumArtist ?? tags.TrackArtist;
            var album = await ResolveAlbumAsync(libraryId, tags, cancellationToken).ConfigureAwait(false);
            var artist = await ResolveArtistAsync(libraryId, tags.TrackArtist, cancellationToken).ConfigureAwait(false);
            var albumArtist = await ResolveArtistAsync(libraryId, albumArtistName, cancellationToken).ConfigureAwait(false);

            track.LibraryId = libraryId;
            track.FileId = fileId;
            track.Title = tags.Title ?? TrackTitleFromFile(absolutePath);
            track.AlbumId = album?.Id;
            track.ArtistId = artist?.Id;
            track.ArtistName = tags.TrackArtist;
            track.AlbumArtistName = albumArtistName;
            track.AlbumArtistId = albumArtist?.Id;
            track.TrackNumber = tags.TrackNumber;
            track.DiscNumber = tags.DiscNumber;
            track.Year = tags.Year;
            track.DurationSeconds = tags.DurationSeconds;
            track.Genre = tags.Genre;
            track.IsMissing = false;
            track.UpdatedAtUtc = now;

            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            logger.LogDebug("{Action} music track {TrackId} from {Path}", isNew ? "Indexed" : "Updated", track.Id, absolutePath);
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

    private async Task<MusicAlbum?> ResolveAlbumAsync(Guid libraryId, AudioTags tags, CancellationToken cancellationToken)
    {
        if (tags.Album is null) return null;

        var artistName = tags.AlbumArtist ?? tags.TrackArtist;
        var albumKey = $"{Normalize(tags.Album)}|{Normalize(artistName)}";
        var album = await dbContext.Albums
            .SingleOrDefaultAsync(a => a.AlbumKey == albumKey && a.LibraryId == libraryId, cancellationToken)
            .ConfigureAwait(false)
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

    private async Task<MusicArtist?> ResolveArtistAsync(Guid libraryId, string? name, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        var normalizedName = Normalize(name);
        var artist = await dbContext.Artists
            .SingleOrDefaultAsync(a => a.NormalizedName == normalizedName && a.LibraryId == libraryId, cancellationToken)
            .ConfigureAwait(false)
            ?? dbContext.Artists.Local.FirstOrDefault(a => a.NormalizedName == normalizedName && a.LibraryId == libraryId);

        if (artist is null)
        {
            artist = new MusicArtist
            {
                Id = Guid.NewGuid(),
                LibraryId = libraryId,
                Name = name.Trim(),
                NormalizedName = normalizedName,
            };
            dbContext.Artists.Add(artist);
        }

        return artist;
    }

    internal static string Normalize(string? value) => (value ?? string.Empty).Trim().ToLowerInvariant();

    private static string TrackTitleFromFile(string absolutePath)
    {
        var name = Path.GetFileName(absolutePath);
        var dot = name.LastIndexOf('.');
        return dot > 0 ? name[..dot] : name;
    }
}
