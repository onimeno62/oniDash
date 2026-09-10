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
            if (!TryParseTagIds(tagIds, out var parsedTagIds)) return Results.BadRequest(new { error = "tagIds must be a comma-separated list of GUIDs." });
            var normalizedStatus = status?.Trim().ToLowerInvariant();
            if (normalizedStatus is not null and not ("available" or "missing")) return Results.BadRequest(new { error = "status must be 'available' or 'missing'." });
            var effectiveLimit = limit is null or < 1 ? 50 : Math.Min(limit.Value, 200);
            var effectiveOffset = offset is null or < 0 ? 0 : Math.Min(offset.Value, 10_000);
            var results = await search.SearchAsync(q ?? string.Empty, libraryId, effectiveLimit, ct, effectiveOffset, mediaType, parsedTagIds, normalizedStatus);
            return Results.Ok(results);
        });
        search.MapPost("/reindex", async (ISearchService search, CancellationToken ct) => Results.Ok(new { indexedItems = await search.ReindexAsync(ct) }));
        return app;
    }

    private static bool TryParseTagIds(string? value, out IReadOnlyCollection<Guid>? ids)
    {
        ids = null;
        if (string.IsNullOrWhiteSpace(value)) return true;
        var parsed = new List<Guid>();
        foreach (var part in value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (!Guid.TryParse(part, out var id)) return false;
            if (!parsed.Contains(id)) parsed.Add(id);
        }
        ids = parsed;
        return true;
    }
}
