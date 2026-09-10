using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using oniDash.Core.Domain;

namespace oniDash.Application.Search;

/// <summary>Canonical cross-media search request. Implementations may use SQLite FTS or another local index.</summary>
public sealed record SearchRequest(
    string Query,
    int Limit = 50,
    int Offset = 0,
    MediaType? MediaType = null,
    Guid? LibraryId = null,
    IReadOnlyCollection<Guid>? TagIds = null);

// Named distinctly from the legacy ISearchService.SearchResult so both contract
// generations coexist while the canonical search migration is in progress.
public sealed record CanonicalSearchResult(
    Guid MediaItemId,
    Guid? MediaFileId,
    MediaType MediaType,
    string Title,
    string? Snippet,
    double Score,
    string? ArtworkPath);

public sealed record SearchResponse(
    IReadOnlyList<CanonicalSearchResult> Items,
    int TotalCount,
    int Limit,
    int Offset,
    bool IsRebuilding = false);

public interface ICanonicalSearchService
{
    Task<SearchResponse> SearchAsync(SearchRequest request, CancellationToken cancellationToken = default);
    Task RebuildAsync(CancellationToken cancellationToken = default);
}
