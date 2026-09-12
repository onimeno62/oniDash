using System;

namespace oniDash.Music.Domain;

public sealed class MusicArtist { public Guid Id { get; set; } public Guid LibraryId { get; set; } public string Name { get; set; } = string.Empty; public string NormalizedName { get; set; } = string.Empty; }
public sealed class MusicAlbum { public Guid Id { get; set; } public Guid LibraryId { get; set; } public string Title { get; set; } = string.Empty; public string NormalizedTitle { get; set; } = string.Empty; public string AlbumKey { get; set; } = string.Empty; public string? ArtistName { get; set; } public Guid? ArtistId { get; set; } public int? Year { get; set; } public byte[]? CoverBlob { get; set; } public string? CoverContentType { get; set; } public MusicArtist? Artist { get; set; } }
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
    public int? TrackNumber { get; set; }
    public int? DiscNumber { get; set; }
    public int? Year { get; set; }
    public double? DurationSeconds { get; set; }
    public string? Genre { get; set; }
    public int Rating { get; set; }
    public double? ReplayGainTrackGainDb { get; set; }
    public double? ReplayGainTrackPeak { get; set; }
    public double? ReplayGainAlbumGainDb { get; set; }
    public double? ReplayGainAlbumPeak { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public MusicAlbum? Album { get; set; }
    public MusicArtist? Artist { get; set; }
}
