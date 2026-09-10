using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace oniDash.Application.Catalogue;

public interface ISeriesCatalogue
{
    Task<IReadOnlyList<SeriesSummary>> ListAsync(Guid? libraryId = null, CancellationToken cancellationToken = default);
}
public sealed record SeriesSummary(Guid Id, Guid LibraryId, string Title, int? Year, int SeasonCount, int EpisodeCount);

public interface IMediaPlugin
{
    string Id { get; }
    string Name { get; }
    IReadOnlySet<string> Capabilities { get; }
}
public interface IMangaSource
{
    string Id { get; }
    string Name { get; }
    string? Language { get; }
    IReadOnlySet<string> Capabilities { get; }
}

public sealed record BookAuthorSummary(Guid Id, string Name, int BookCount);
public sealed record BookSeriesSummary(Guid Id, string Name, int BookCount);
public interface IBookReader
{
    Task<Stream?> OpenAsync(Guid bookId, CancellationToken cancellationToken = default);
}
public sealed record UnifiedMediaActivity(Guid MediaItemId, string MediaType, string Title, DateTimeOffset OccurredAtUtc, double? Progress);
