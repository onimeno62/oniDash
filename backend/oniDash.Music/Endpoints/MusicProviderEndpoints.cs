using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using oniDash.Music.Metadata;

namespace oniDash.Music.Endpoints;

/// <summary>External metadata lookup is explicitly read-only and returns proposals only.</summary>
public static class MusicProviderEndpoints
{
    public static IEndpointRouteBuilder MapMusicProviderEndpoints(this IEndpointRouteBuilder app)
    {
        var music = app.MapGroup("/api/music/providers").WithTags("Music metadata providers");
        music.MapGet("/{providerId}/search", async (IMusicMetadataProvider provider, string providerId, string? type, string? q, CancellationToken ct) =>
        {
            if (!string.Equals(provider.Id, providerId, StringComparison.OrdinalIgnoreCase)) return Results.NotFound();
            if (string.IsNullOrWhiteSpace(q)) return Results.BadRequest(new { error = "Search query is required." });
            var result = type?.ToLowerInvariant() switch
            {
                "artist" => await provider.SearchArtistAsync(q, ct),
                "album" => await provider.SearchAlbumAsync(q, ct),
                "track" => await provider.SearchTrackAsync(q, ct),
                _ => null
            };
            return result is null ? Results.NotFound() : Results.Ok(new { proposal = result, authoritative = false });
        });
        return app;
    }
}
