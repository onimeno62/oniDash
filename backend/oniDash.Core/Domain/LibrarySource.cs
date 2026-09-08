using System;
using System.Collections.Generic;

namespace oniDash.Core.Domain;

/// <summary>
/// A configured local folder that feeds the library. Sources are read-only by definition:
/// oniDash never creates, renames, moves, or deletes files inside them (AGENTS.md rule 11).
/// </summary>
public class LibrarySource : Entity
{
    public Guid LibraryId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Absolute, normalized path of an existing directory.</summary>
    public string RootPath { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>When the source was last scanned successfully (null = never scanned).</summary>
    public DateTimeOffset? LastScannedAtUtc { get; set; }

    public Library Library { get; set; } = null!;

    public ICollection<MediaFile> Files { get; set; } = new List<MediaFile>();
}
