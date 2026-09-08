using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace oniDash.Application.Search;

/// <summary>One ranked hit from the global search index (mirrors a media item).</summary>
public sealed record SearchResult(
    Guid ItemId,
    string DisplayName,
    Guid LibraryId,
    string LibraryName);

public interface ISearchService
{
    /// <summary>
    /// Full-text search across all indexed media items. All whitespace-separated terms
    /// must match (prefix matching per term), optionally constrained to one library,
    /// ranked most-relevant-first. A blank query yields an empty result.
    /// </summary>
    Task<IReadOnlyList<SearchResult>> SearchAsync(
        string query,
        Guid? libraryId = null,
        int limit = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Rebuilds the search index from the persisted media items (recovery/consistency
    /// command; the index is otherwise kept in sync by database triggers). Returns the
    /// number of indexed items.
    /// </summary>
    Task<int> ReindexAsync(CancellationToken cancellationToken = default);
}
