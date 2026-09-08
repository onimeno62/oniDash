using System;
using System.Collections.Generic;

namespace oniDash.Core.Domain;

/// <summary>
/// A logical media entry in the library. Catalogue plugins (music, movies, ...) attach
/// their own typed models to a media item later; this type deliberately carries no
/// catalogue-specific fields (AGENTS.md rule 1, ARCHITECTURE.md).
/// </summary>
public class MediaItem : Entity
{
    public Guid LibraryId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public Library Library { get; set; } = null!;

    public ICollection<MediaFile> Files { get; set; } = new List<MediaFile>();

    public ICollection<Artwork> Artwork { get; set; } = new List<Artwork>();

    public ICollection<MediaItemTag> Tags { get; set; } = new List<MediaItemTag>();

    public ICollection<CollectionItem> Collections { get; set; } = new List<CollectionItem>();
}
