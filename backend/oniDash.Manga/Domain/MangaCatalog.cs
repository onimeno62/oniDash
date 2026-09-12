using System;

namespace oniDash.Manga.Domain;

public sealed class MangaTitle
{
    public Guid Id { get; set; }
    public Guid LibraryId { get; set; }
    public Guid? MediaItemId { get; set; }
    public string SourceId { get; set; } = "local";
    public string ExternalId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string NormalizedTitle { get; set; } = string.Empty;
    public string? Author { get; set; }
    public string? Description { get; set; }
    public bool Favorite { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class MangaChapter
{
    public Guid Id { get; set; }
    public Guid MangaId { get; set; }
    public string SourceChapterId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public double Number { get; set; }
    public string? LocalPath { get; set; }
    public int? PageCount { get; set; }
    public int CurrentPage { get; set; }
    public bool Read { get; set; }
    public DateTimeOffset? LastReadAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class MangaBookmarkEntity
{
    public Guid Id { get; set; }
    public Guid ChapterId { get; set; }
    public int Page { get; set; }
    public string? Note { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class MangaPlugin
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0";
    public bool Installed { get; set; }
    public bool Enabled { get; set; }
    public string State { get; set; } = "Available";
}

public sealed class MangaDownload
{
    public Guid Id { get; set; }
    public Guid ChapterId { get; set; }
    public string State { get; set; } = "Pending";
    public string? DestinationPath { get; set; }
    public string? Error { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class MangaNotification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MangaId { get; set; }
    public string MangaTitle { get; set; } = string.Empty;
    public string ChapterTitle { get; set; } = string.Empty;
    public double ChapterNumber { get; set; }
    public bool Read { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class MangaTrackingSync
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MangaId { get; set; }
    public string TrackerName { get; set; } = string.Empty; // e.g. AniList, MyAnimeList, Kitsu
    public string ExternalTrackingId { get; set; } = string.Empty;
    public int LastSyncedChapter { get; set; }
    public string Status { get; set; } = "reading";
    public int Score { get; set; }
    public DateTimeOffset LastSyncedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
