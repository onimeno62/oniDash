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
    /// <summary>Full-text search across indexed media items, optionally constrained to one library and paged by offset.</summary>
    Task<IReadOnlyList<SearchResult>> SearchAsync(
        string query,
        Guid? libraryId = null,
        int limit = 50,
        int offset = 0,
        CancellationToken cancellationToken = default);

    /// <summary>Rebuilds the search index from persisted media items.</summary>
    Task<int> ReindexAsync(CancellationToken cancellationToken = default);
}
