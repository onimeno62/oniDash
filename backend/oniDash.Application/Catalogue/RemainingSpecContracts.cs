using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace oniDash.Application.Catalogue;

public sealed record SeasonSummary(Guid Id, Guid SeriesId, int Number, int EpisodeCount);
public sealed record EpisodeSummary(Guid Id, Guid SeriesId, Guid SeasonId, int Number, string Title, double? DurationSeconds, double? ProgressSeconds, bool Watched);
public interface IEpisodeCatalogue
{
    Task<IReadOnlyList<SeasonSummary>> ListSeasonsAsync(Guid seriesId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EpisodeSummary>> ListEpisodesAsync(Guid seasonId, CancellationToken cancellationToken = default);
}
public interface IAnimeMetadataProvider
{
    string Id { get; }
    Task<MetadataSuggestion?> SuggestAsync(Guid mediaItemId, CancellationToken cancellationToken = default);
}
public sealed record MetadataSuggestion(string ProviderId, string? Title, IReadOnlyDictionary<string, string> Fields, double Confidence);

public enum PluginLifecycleState { Available, Installed, Enabled, Disabled, Failed }
public sealed record PluginDescriptor(string Id, string Name, string Version, PluginLifecycleState State, IReadOnlySet<string> Capabilities);
public interface IMediaPluginManager
{
    Task<IReadOnlyList<PluginDescriptor>> ListAsync(CancellationToken cancellationToken = default);
    Task<PluginDescriptor?> InstallAsync(string pluginId, CancellationToken cancellationToken = default);
    Task<bool> SetEnabledAsync(string pluginId, bool enabled, CancellationToken cancellationToken = default);
}
public interface IMangaChapterSource
{
    Task<IReadOnlyList<string>> SearchAsync(string query, CancellationToken cancellationToken = default);
    Task<Stream?> OpenChapterAsync(string chapterId, CancellationToken cancellationToken = default);
}
public sealed record MangaChapterSummary(string Id, Guid MangaId, string Title, int Number, bool Downloaded, double? Progress);
public interface IMangaChapterCatalogue
{
    Task<IReadOnlyList<MangaChapterSummary>> ListAsync(Guid mangaId, CancellationToken cancellationToken = default);
    Task SetProgressAsync(string chapterId, double progress, CancellationToken cancellationToken = default);
}
public interface IMangaDownloadQueue
{
    Task EnqueueAsync(IReadOnlyCollection<string> chapterIds, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> ListPendingAsync(CancellationToken cancellationToken = default);
}
public interface IUnifiedMediaService
{
    Task<IReadOnlyList<UnifiedMediaActivity>> ActivityAsync(int limit = 50, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UnifiedMediaActivity>> ContinueAsync(int limit = 20, CancellationToken cancellationToken = default);
}
