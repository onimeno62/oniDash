using System;

namespace oniDash.Movies.Domain;

/// <summary>
/// One movie, anchored 1:1 to a Core media item (the scan's placeholder item), so
/// global search, tags, and collections keep working across re-scans. Metadata comes
/// from the filename (title, year) and a probe of the file itself (duration); artwork
/// is embedded container art when the file carries one (MP4 cover atom).
/// </summary>
public sealed class Movie
{
    public Guid Id { get; set; }

    public Guid LibraryId { get; set; }

    /// <summary>The Core MediaItem this movie enriches (unique).</summary>
    public Guid MediaItemId { get; set; }

    /// <summary>The Core MediaFile row currently backing this movie (for streaming).</summary>
    public Guid FileId { get; set; }

    public string Title { get; set; } = string.Empty;

    /// <summary>Trimmed, lowercase title used for deterministic ordering only.</summary>
    public string NormalizedTitle { get; set; } = string.Empty;

    public int? Year { get; set; }

    public double? DurationSeconds { get; set; }

    /// <summary>Lowercase file extension with dot (".mp4"), for display and codecs.</summary>
    public string? Container { get; set; }

    public byte[]? PosterBlob { get; set; }

    public string? PosterContentType { get; set; }

    /// <summary>Resume position in seconds; null once watched or never started.</summary>
    public double? WatchProgressSeconds { get; set; }

    public bool Watched { get; set; }

    public DateTimeOffset? WatchedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }
}
