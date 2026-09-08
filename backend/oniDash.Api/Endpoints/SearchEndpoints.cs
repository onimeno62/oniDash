using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using oniDash.Application.Search;

namespace oniDash.Api.Endpoints;

/// <summary>
/// Global search endpoints: full-text query over indexed media items (FTS5, bm25
/// ranking, optional library filter) plus a manual reindex command for consistency
/// recovery.
/// </summary>
public static class SearchEndpoints
{
    public static IEndpointRouteBuilder MapSearchEndpoints(this IEndpointRouteBuilder app)
    {
        var search = app.MapGroup("/api/search").WithTags("Search");

        search.MapGet("/", async (
            ISearchService search,
            string? q,
            Guid? libraryId,
            int? limit,
            CancellationToken ct) =>
        {
            var effectiveLimit = limit is null or < 1 ? 50 : Math.Min(limit.Value, 200);
            var results = await search.SearchAsync(q ?? string.Empty, libraryId, effectiveLimit, ct);
            return Results.Ok(results);
        });

        search.MapPost("/reindex", async (ISearchService search, CancellationToken ct) =>
        {
            var indexedItems = await search.ReindexAsync(ct);
            return Results.Ok(new { indexedItems });
        });

        return app;
    }
}
