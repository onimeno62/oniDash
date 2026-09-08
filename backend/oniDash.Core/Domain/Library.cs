using System;
using System.Collections.Generic;

namespace oniDash.Core.Domain;

/// <summary>
/// A top-level library container. All sources, media items, tags, and collections belong
/// to exactly one library. Deleting a library removes only index data — never user files.
/// </summary>
public class Library : Entity
{
    public string Name { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<LibrarySource> Sources { get; set; } = new List<LibrarySource>();
}
