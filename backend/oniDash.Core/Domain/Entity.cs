using System;
using System.Collections.Generic;

namespace oniDash.Core.Domain;

/// <summary>
/// Base type for entities persisted by the core library. Identity and equality are value-based
/// on <see cref="Id"/> within the same concrete type.
/// </summary>
/// <remarks>
/// Milestone 01 contains no catalogue domain models on purpose (Architecture rule 1). Concrete
/// generic entities (Library, MediaItem, ...) arrive with Phase 2 of the roadmap.
/// </remarks>
public abstract class Entity : IEquatable<Entity>
{
    /// <summary>
    /// Stable identity of the entity within its type. Init-only: assigned at construction
    /// (or by EF Core materialization), immutable afterwards.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    public bool Equals(Entity? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return GetType() == other.GetType() && Id == other.Id;
    }

    public override bool Equals(object? obj) => Equals(obj as Entity);

    public override int GetHashCode() => (GetType().GetHashCode(), Id.GetHashCode()).GetHashCode();

    public static bool operator ==(Entity? left, Entity? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(Entity? left, Entity? right) => !(left == right);
}
