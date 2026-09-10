using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using oniDash.Application.Search;

namespace oniDash.Api.Endpoints;

/// <summary>Global FTS search with optional library filter and bounded offset pagination.</summary>
public static class SearchEndpoints
{
    public static IEndpointRouteBuilder MapSearchEndpoints(this IEndpointRouteBuilder app)
    {
        var search = app.MapGroup("/api/search").WithTags("Search");
        search.MapGet("/", async (ISearchService search, string? q, Guid? libraryId, int? limit, int? offset, CancellationToken ct) =>
        {
            var effectiveLimit = limit is null or < 1 ? 50 : Math.Min(limit.Value, 200);
            var effectiveOffset = offset is null or < 0 ? 0 : Math.Min(offset.Value, 10_000);
            var results = await search.SearchAsync(q ?? string.Empty, libraryId, effectiveLimit, effectiveOffset, ct);
            return Results.Ok(results);
        });
        search.MapPost("/reindex", async (ISearchService search, CancellationToken ct) => Results.Ok(new { indexedItems = await search.ReindexAsync(ct) }));
        return app;
    }
}
