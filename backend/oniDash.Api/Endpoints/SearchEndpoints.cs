using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using oniDash.Application.Search;
using oniDash.Core.Domain;

namespace oniDash.Api.Endpoints;

/// <summary>Global search endpoints with library, media type, tag, availability status, and offset filters.</summary>
public static class SearchEndpoints
{
    public static IEndpointRouteBuilder MapSearchEndpoints(this IEndpointRouteBuilder app)
    {
        var search = app.MapGroup("/api/search").WithTags("Search");
        search.MapGet("/", async (ISearchService search, string? q, Guid? libraryId, MediaType? mediaType, string? tagIds, string? status, int? limit, int? offset, CancellationToken ct) =>
        {
            var effectiveLimit = limit is null or < 1 ? 50 : Math.Min(limit.Value, 200);
            var effectiveOffset = offset is null or < 0 ? 0 : Math.Min(offset.Value, 10_000);
            var parsedTagIds = ParseTagIds(tagIds);
            var results = await search.SearchAsync(q ?? string.Empty, libraryId, effectiveLimit, ct, effectiveOffset, mediaType, parsedTagIds, status);
            return Results.Ok(results);
        });
        search.MapPost("/reindex", async (ISearchService search, CancellationToken ct) => Results.Ok(new { indexedItems = await search.ReindexAsync(ct) }));
        return app;
    }

    private static IReadOnlyCollection<Guid>? ParseTagIds(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var ids = value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Select(Guid.Parse).Distinct().ToArray();
        return ids.Length == 0 ? null : ids;
    }
}
