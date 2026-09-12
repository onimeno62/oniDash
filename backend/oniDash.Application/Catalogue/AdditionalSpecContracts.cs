using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace oniDash.Application.Catalogue;

public sealed record ProviderCapability(string Id, string Name, IReadOnlySet<string> Capabilities);
public interface IProviderRegistry
{
    Task<IReadOnlyList<ProviderCapability>> ListAsync(CancellationToken cancellationToken = default);
    Task<bool> SetEnabledAsync(string providerId, bool enabled, CancellationToken cancellationToken = default);
}

public sealed record EpisodeProgress(Guid EpisodeId, double PositionSeconds, double? DurationSeconds, bool Watched);
public interface IEpisodeProgressStore
{
    Task<EpisodeProgress?> GetAsync(Guid episodeId, CancellationToken cancellationToken = default);
    Task<EpisodeProgress> SetAsync(Guid episodeId, double positionSeconds, CancellationToken cancellationToken = default);
}

public sealed record MangaBookmark(string Id, string ChapterId, int Page, string? Note, DateTimeOffset CreatedAtUtc);
public interface IMangaBookmarkStore
{
    Task<IReadOnlyList<MangaBookmark>> ListAsync(string chapterId, CancellationToken cancellationToken = default);
    Task<MangaBookmark> AddAsync(string chapterId, int page, string? note, CancellationToken cancellationToken = default);
    Task RemoveAsync(string bookmarkId, CancellationToken cancellationToken = default);
}

public interface IMangaTrackingProvider
{
    string Id { get; }
    Task<bool> PushProgressAsync(Guid mangaId, double progress, CancellationToken cancellationToken = default);
}

public sealed record LibraryUpdateSummary(DateTimeOffset CheckedAtUtc, int Added, int Updated, int Failed, IReadOnlyList<string> Errors);
public interface ILibraryUpdateService
{
    Task<LibraryUpdateSummary> CheckAsync(Guid? libraryId = null, CancellationToken cancellationToken = default);
}

public interface IMediaReader
{
    Task<Stream?> OpenPageAsync(string documentId, int page, CancellationToken cancellationToken = default);
    Task<int?> GetPageCountAsync(string documentId, CancellationToken cancellationToken = default);
}

public interface IMediaKeyHandler
{
    Task RegisterAsync(CancellationToken cancellationToken = default);
    Task UnregisterAsync(CancellationToken cancellationToken = default);
}

public interface IBackupManifestService
{
    Task<BackupManifest> CreateAsync(CancellationToken cancellationToken = default);
}
public sealed record BackupManifest(string SchemaVersion, DateTimeOffset CreatedAtUtc, IReadOnlyList<string> IncludedStores);

public interface IMigrationService
{
    Task<IReadOnlyList<string>> ValidateAsync(Stream backup, CancellationToken cancellationToken = default);
    Task RestoreAsync(Stream backup, CancellationToken cancellationToken = default);
}

public interface IInstallerUpdateService
{
    Task<InstallerUpdate?> CheckAsync(CancellationToken cancellationToken = default);
}
public sealed record InstallerUpdate(string Version, Uri PackageUri, string Sha256);

public interface INotificationService
{
    Task NotifyAsync(string title, string message, CancellationToken cancellationToken = default);
}

public interface IDiagnosticsExportService
{
    Task ExportAsync(Stream destination, CancellationToken cancellationToken = default);
}
