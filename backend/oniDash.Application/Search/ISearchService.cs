using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace oniDash.Application.Search;

/// <summary>One ranked hit from the global search index (mirrors a media item).</summary>
public sealed record SearchResult(Guid ItemId, string DisplayName, Guid LibraryId, string LibraryName);

public interface ISearchService
{
    /// <summary>Full-text search across all indexed media items with optional library and offset filters.</summary>
    Task<IReadOnlyList<SearchResult>> SearchAsync(string query, Guid? libraryId = null, int limit = 50, CancellationToken cancellationToken = default, int offset = 0);
    Task<int> ReindexAsync(CancellationToken cancellationToken = default);
}
