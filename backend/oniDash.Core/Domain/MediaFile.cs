using System;

namespace oniDash.Core.Domain;

/// <summary>
/// A physical file on disk that belongs to a media item. Pure index data: the file itself
/// is never modified by oniDash.
/// </summary>
public class MediaFile : Entity
{
    public Guid MediaItemId { get; set; }

    public Guid LibrarySourceId { get; set; }

    /// <summary>Path relative to <see cref="LibrarySource.RootPath"/> with '/' separators.</summary>
    public string RelativePath { get; set; } = string.Empty;

    /// <summary>
    /// Stable, case-normalized identity of the file within its source (source id + relative
    /// path). Dedup groundwork: re-scans match on this key instead of the row id.
    /// </summary>
    public string IdentityKey { get; set; } = string.Empty;

    /// <summary>
    /// Set when a scan of the owning source no longer finds the file. Index data is kept
    /// (preserving user tags/collections) instead of being deleted.
    /// </summary>
    public DateTimeOffset? MissingSinceUtc { get; set; }

    public string Extension { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public DateTimeOffset LastWriteTimeUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public MediaItem MediaItem { get; set; } = null!;

    public LibrarySource LibrarySource { get; set; } = null!;
}
