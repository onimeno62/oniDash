using System;
using System.Collections.Generic;

namespace oniDash.Core.Domain;

/// <summary>A user-applied label, unique per library.</summary>
public class Tag : Entity
{
    public Guid LibraryId { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public Library Library { get; set; } = null!;

    public ICollection<MediaItemTag> Items { get; set; } = new List<MediaItemTag>();
}

/// <summary>Join entity: a tag applied to a media item.</summary>
public class MediaItemTag
{
    public Guid MediaItemId { get; set; }

    public Guid TagId { get; set; }

    public DateTimeOffset TaggedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public MediaItem MediaItem { get; set; } = null!;

    public Tag Tag { get; set; } = null!;
}
