using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace oniDash.Application.Catalogue;

public sealed record BookAuthorRecord(Guid Id, string Name, int BookCount);
public sealed record BookSeriesRecord(Guid Id, string Name, int BookCount);
public interface IBookMetadataCatalogue
{
    Task<IReadOnlyList<BookAuthorRecord>> ListAuthorsAsync(Guid? libraryId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BookSeriesRecord>> ListSeriesAsync(Guid? libraryId = null, CancellationToken cancellationToken = default);
}
public interface IBookContentReader
{
    Task<Stream?> OpenAsync(Guid bookId, string? format = null, CancellationToken cancellationToken = default);
}
public interface IUnifiedCatalogueService
{
    Task<IReadOnlyList<UnifiedMediaActivity>> RecentlyAddedAsync(int limit = 50, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UnifiedMediaActivity>> RecentlyPlayedAsync(int limit = 50, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UnifiedMediaActivity>> FavoritesAsync(int limit = 50, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UnifiedMediaActivity>> RecommendationsAsync(int limit = 20, CancellationToken cancellationToken = default);
}
public interface IDesktopHost
{
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}
public interface IInstallerManifest
{
    string ProductId { get; }
    string Version { get; }
    IReadOnlyList<string> Files { get; }
}
public interface ITrayIntegration
{
    Task SetVisibleAsync(bool visible, CancellationToken cancellationToken = default);
    Task ShowNotificationAsync(string title, string message, CancellationToken cancellationToken = default);
}
public interface IFileAssociationRegistrar
{
    Task RegisterAsync(IReadOnlyCollection<string> extensions, CancellationToken cancellationToken = default);
    Task UnregisterAsync(IReadOnlyCollection<string> extensions, CancellationToken cancellationToken = default);
}
public interface IBackupService
{
    Task ExportAsync(Stream destination, CancellationToken cancellationToken = default);
    Task ImportAsync(Stream source, CancellationToken cancellationToken = default);
}
