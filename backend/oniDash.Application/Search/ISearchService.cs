using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using oniDash.Core.Domain;

namespace oniDash.Application.Search;

/// <summary>One ranked hit from the global search index (mirrors a media item).</summary>
public sealed record SearchResult(Guid ItemId, string DisplayName, Guid LibraryId, string LibraryName);

public interface ISearchService
{
    /// <summary>Full-text search across indexed media items with optional library, media-type, and offset filters.</summary>
    Task<IReadOnlyList<SearchResult>> SearchAsync(string query, Guid? libraryId = null, int limit = 50, CancellationToken cancellationToken = default, int offset = 0, MediaType? mediaType = null);
    Task<int> ReindexAsync(CancellationToken cancellationToken = default);
}
