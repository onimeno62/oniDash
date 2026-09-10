using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using oniDash.Application.Search;
using oniDash.Core.Domain;

namespace oniDash.Api.Endpoints;

/// <summary>Global search endpoints with library, media-type, and offset filters.</summary>
public static class SearchEndpoints
{
    public static IEndpointRouteBuilder MapSearchEndpoints(this IEndpointRouteBuilder app)
    {
        var search = app.MapGroup("/api/search").WithTags("Search");
        search.MapGet("/", async (ISearchService search, string? q, Guid? libraryId, MediaType? mediaType, int? limit, int? offset, CancellationToken ct) =>
        {
            var effectiveLimit = limit is null or < 1 ? 50 : Math.Min(limit.Value, 200);
            var effectiveOffset = offset is null or < 0 ? 0 : Math.Min(offset.Value, 10_000);
            var results = await search.SearchAsync(q ?? string.Empty, libraryId, effectiveLimit, ct, effectiveOffset, mediaType);
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
