using System;

namespace oniDash.Music.Domain;

public sealed class MusicPlaylist
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSmart { get; set; }
    public string? SmartQuery { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class MusicPlaylistItem
{
    public Guid Id { get; set; }
    public Guid PlaylistId { get; set; }
    public Guid TrackId { get; set; }
    public int Position { get; set; }
    public DateTimeOffset AddedAtUtc { get; set; }
    public MusicPlaylist? Playlist { get; set; }
    public MusicTrack? Track { get; set; }
}
