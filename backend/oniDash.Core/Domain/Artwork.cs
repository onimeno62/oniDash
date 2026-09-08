using System;

namespace oniDash.Core.Domain;

/// <summary>
/// Categorization of an artwork asset. Values stay generic so every catalogue can reuse
/// them without type-specific conditionals (AGENTS.md rule 6).
/// </summary>
public enum ArtworkKind
{
    Thumbnail = 0,
    Poster = 1,
    Backdrop = 2,
    Banner = 3,
}

/// <summary>
/// An image asset associated with a media item (local file or cached external image).
/// </summary>
public class Artwork : Entity
{
    public Guid MediaItemId { get; set; }

    public ArtworkKind Kind { get; set; } = ArtworkKind.Thumbnail;

    /// <summary>Absolute path or remote URL of the image asset.</summary>
    public string SourcePath { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public MediaItem MediaItem { get; set; } = null!;
}
