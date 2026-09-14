using System;
using System.Collections.Generic;

namespace oniDash.Music.Contracts;

public sealed record MusicTrackDto(
    Guid Id,
    Guid LibraryId,
    Guid MediaItemId,
    Guid FileId,
    string Title,
    string? ArtistName,
    string? AlbumArtistName,
    string? AlbumTitle,
    Guid? AlbumId,
    int? TrackNumber,
    int? DiscNumber,
    int? Year,
    double? DurationSeconds,
    string? Genre,
    int Rating,
    bool IsFavorite,
    bool IsMissing,
    DateTimeOffset AddedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record MusicAlbumDto(
    Guid Id,
    Guid LibraryId,
    string Title,
    string? ArtistName,
    int? Year,
    bool HasArtwork,
    int TrackCount);

public sealed record MusicArtistDto(
    Guid Id,
    Guid LibraryId,
    string Name,
    int TrackCount,
    int AlbumCount);

public sealed record MusicPlaybackStateDto(
    Guid TrackId,
    double PositionSeconds,
    bool Completed,
    DateTimeOffset UpdatedAtUtc);

public sealed record MusicLyricsDto(
    Guid Id,
    Guid TrackId,
    string Text,
    string Kind,
    string Source,
    bool IsLocalEdit,
    DateTimeOffset UpdatedAtUtc);

public sealed record MusicArtworkDto(
    Guid Id,
    Guid? TrackId,
    Guid? AlbumId,
    string Source,
    string? Provider,
    string ContentType,
    DateTimeOffset UpdatedAtUtc);

public sealed record MusicPage<T>(
    IReadOnlyList<T> Items,
    int Offset,
    int Limit,
    long Total);
