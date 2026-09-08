using System;
using System.Collections.Generic;

namespace oniDash.Core.Domain;

/// <summary>A named, user-curated grouping of media items, unique per library.</summary>
public class Collection : Entity
{
    public Guid LibraryId { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public Library Library { get; set; } = null!;

    public ICollection<CollectionItem> Items { get; set; } = new List<CollectionItem>();
}

/// <summary>Join entity: a media item placed in a collection.</summary>
public class CollectionItem
{
    public Guid CollectionId { get; set; }

    public Guid MediaItemId { get; set; }

    public DateTimeOffset AddedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public Collection Collection { get; set; } = null!;

    public MediaItem MediaItem { get; set; } = null!;
}
