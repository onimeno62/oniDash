using System;

namespace oniDash.Music.Domain;

public sealed class MusicArtist
{
    public Guid Id { get; set; }
    public Guid LibraryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
}

public sealed class MusicAlbum
{
    public Guid Id { get; set; }
    public Guid LibraryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string NormalizedTitle { get; set; } = string.Empty;
    public string AlbumKey { get; set; } = string.Empty;
    public string? ArtistName { get; set; }
    public Guid? ArtistId { get; set; }
    public int? Year { get; set; }
    public byte[]? CoverBlob { get; set; }
    public string? CoverContentType { get; set; }
    public MusicArtist? Artist { get; set; }
}

public sealed class MusicTrack
{
    public Guid Id { get; set; }
    public Guid LibraryId { get; set; }
    public Guid MediaItemId { get; set; }
    public Guid FileId { get; set; }
    public string Title { get; set; } = string.Empty;
    public Guid? AlbumId { get; set; }
    public Guid? ArtistId { get; set; }
    public string? ArtistName { get; set; }
    public string? AlbumArtistName { get; set; }
    public Guid? AlbumArtistId { get; set; }
    public int? TrackNumber { get; set; }
    public int? DiscNumber { get; set; }
    public int? Year { get; set; }
    public double? DurationSeconds { get; set; }
    public string? Genre { get; set; }
    public int Rating { get; set; }
    public bool IsMissing { get; set; }
    public DateTimeOffset AddedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public double? ReplayGainTrackGainDb { get; set; }
    public double? ReplayGainTrackPeak { get; set; }
    public double? ReplayGainAlbumGainDb { get; set; }
    public double? ReplayGainAlbumPeak { get; set; }
    public MusicAlbum? Album { get; set; }
    public MusicArtist? Artist { get; set; }
    public MusicArtist? AlbumArtist { get; set; }
}

public sealed class MusicPlaybackState
{
    public Guid Id { get; set; }
    public Guid TrackId { get; set; }
    public double PositionSeconds { get; set; }
    public bool Completed { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public MusicTrack? Track { get; set; }
}

public enum MusicLyricsKind
{
    Plain = 0,
    Synced = 1,
    WordSynced = 2
}

public sealed class MusicLyrics
{
    public Guid Id { get; set; }
    public Guid TrackId { get; set; }
    public string Text { get; set; } = string.Empty;
    public MusicLyricsKind Kind { get; set; }
    public string Source { get; set; } = "local";
    public bool IsLocalEdit { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public MusicTrack? Track { get; set; }
}

public enum MusicArtworkSource
{
    Embedded = 0,
    Local = 1,
    External = 2
}

public sealed class MusicArtwork
{
    public Guid Id { get; set; }
    public Guid? TrackId { get; set; }
    public Guid? AlbumId { get; set; }
    public MusicArtworkSource Source { get; set; }
    public string? Provider { get; set; }
    public string ContentType { get; set; } = "image/jpeg";
    public byte[] Data { get; set; } = Array.Empty<byte>();
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
