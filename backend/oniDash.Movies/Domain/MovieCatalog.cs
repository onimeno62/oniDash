using System;

namespace oniDash.Movies.Domain;

/// <summary>One movie anchored to a Core media item.</summary>
public sealed class Movie
{
    public Guid Id { get; set; }
    public Guid LibraryId { get; set; }
    public Guid MediaItemId { get; set; }
    public Guid FileId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string NormalizedTitle { get; set; } = string.Empty;
    public int? Year { get; set; }
    public double? DurationSeconds { get; set; }
    public string? Container { get; set; }
    public byte[]? PosterBlob { get; set; }
    public string? PosterContentType { get; set; }
    public double? WatchProgressSeconds { get; set; }
    public bool Watched { get; set; }
    public DateTimeOffset? WatchedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

/// <summary>A persisted TV or Anime series owned by the Movies catalogue plugin.</summary>
public sealed class TvSeries
{
    public Guid Id { get; set; }
    public Guid LibraryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string NormalizedTitle { get; set; } = string.Empty;
    public int? Year { get; set; }
    public bool IsAnime { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

/// <summary>A numbered season within a series.</summary>
public sealed class TvSeason
{
    public Guid Id { get; set; }
    public Guid SeriesId { get; set; }
    public int Number { get; set; }
    public string? Title { get; set; }
}

/// <summary>An episode anchored to the canonical Core media item and file.</summary>
public sealed class TvEpisode
{
    public Guid Id { get; set; }
    public Guid SeasonId { get; set; }
    public Guid MediaItemId { get; set; }
    public Guid FileId { get; set; }
    public int Number { get; set; }
    public string Title { get; set; } = string.Empty;
    public double? DurationSeconds { get; set; }
    public double? WatchProgressSeconds { get; set; }
    public bool Watched { get; set; }
    public DateTimeOffset? WatchedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
