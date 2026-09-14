using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using oniDash.Music.Lyrics;

namespace oniDash.Music.Endpoints;

public static class MusicLyricsProviderEndpoints
{
    public static IEndpointRouteBuilder MapMusicLyricsProviderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/music/providers/lyrics").WithTags("Music lyrics");
        group.MapGet("/search", async (IMusicLyricsProvider provider, string? track, string? artist, string? album, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(track)) return Results.BadRequest(new { error = "track is required" });
            var candidates = await provider.SearchAsync(track, artist, album, ct);
            return Results.Ok(candidates);
        });
        return app;
    }
}
