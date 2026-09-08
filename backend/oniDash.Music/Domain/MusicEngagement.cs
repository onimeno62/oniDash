using System;

namespace oniDash.Music.Domain;

public sealed class MusicFavorite
{
    public Guid Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class MusicPlayHistory
{
    public Guid Id { get; set; }
    public Guid TrackId { get; set; }
    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public double PlayedSeconds { get; set; }
    public double CompletionRatio { get; set; }
    public string Source { get; set; } = "local";
}
