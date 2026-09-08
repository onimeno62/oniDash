using System;

namespace oniDash.Music.Domain;

/// <summary>A music artist within one library. Grouping key: normalized name.</summary>
public sealed class MusicArtist
{
    public Guid Id { get; set; }

    public Guid LibraryId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Trimmed, lowercase name used to merge identical artists within a library.</summary>
    public string NormalizedName { get; set; } = string.Empty;
}

/// <summary>
/// An album within one library: grouped by (normalized album title, normalized album
/// artist). Cover art is the first embedded front-cover picture found among its tracks.
/// </summary>
public sealed class MusicAlbum
{
    public Guid Id { get; set; }

    public Guid LibraryId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string NormalizedTitle { get; set; } = string.Empty;

    /// <summary>Library-scoped grouping key: normalized title + '|' + normalized album artist.</summary>
    public string AlbumKey { get; set; } = string.Empty;

    /// <summary>Display name of the album artist (denormalized; survives artist-row changes).</summary>
    public string? ArtistName { get; set; }

    /// <summary>Row of the primary album artist, if the tags named one.</summary>
    public Guid? ArtistId { get; set; }

    public int? Year { get; set; }

    public byte[]? CoverBlob { get; set; }

    public string? CoverContentType { get; set; }

    public MusicArtist? Artist { get; set; }
}

/// <summary>
/// One playable audio track, anchored 1:1 to a Core media item (the scan's placeholder
/// item), so global search, tags, and collections keep working across re-scans.
/// </summary>
public sealed class MusicTrack
{
    public Guid Id { get; set; }

    public Guid LibraryId { get; set; }

    /// <summary>The Core MediaItem this track enriches (unique).</summary>
    public Guid MediaItemId { get; set; }

    /// <summary>The Core MediaFile row currently backing this track (for streaming).</summary>
    public Guid FileId { get; set; }

    public string Title { get; set; } = string.Empty;

    public Guid? AlbumId { get; set; }

    public Guid? ArtistId { get; set; }

    /// <summary>Display name of the track artist (denormalized).</summary>
    public string? ArtistName { get; set; }

    public int? TrackNumber { get; set; }

    public int? DiscNumber { get; set; }

    public int? Year { get; set; }

    public double? DurationSeconds { get; set; }

    public string? Genre { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }

    public MusicAlbum? Album { get; set; }

    public MusicArtist? Artist { get; set; }
}
