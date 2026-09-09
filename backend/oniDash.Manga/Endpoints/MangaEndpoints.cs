using System.Text.Json;
using Microsoft.AspNetCore.Http.HttpResults;

namespace oniDash.Manga.Endpoints;

public static class MangaEndpoints
{
    public static IEndpointRouteBuilder MapMangaEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/manga");

        group.MapGet("/library", async (SuwayomiClient client, CancellationToken ct) => Results.Ok(await client.LibraryAsync(ct)));
        group.MapGet("/continue-reading", async (SuwayomiClient client, CancellationToken ct) =>
        {
            var data = await client.LibraryAsync(ct);
            return Results.Ok(data.GetProperty("mangas").GetProperty("nodes").EnumerateArray()
                .Where(x => x.TryGetProperty("lastReadChapter", out var c) && c.ValueKind != JsonValueKind.Null)
                .OrderByDescending(x => x.GetProperty("lastReadChapter").GetProperty("lastPageRead").GetInt32()).Take(12).ToArray());
        });
        group.MapGet("/updates", async (SuwayomiClient client, CancellationToken ct) =>
        {
            var data = await client.UpdatesAsync(ct);
            return Results.Ok(data.GetProperty("libraryUpdateStatus").GetProperty("mangaUpdates"));
        });
        group.MapGet("/categories", async (SuwayomiClient client, CancellationToken ct) => Results.Ok((await client.CategoriesAsync(ct)).GetProperty("categories").GetProperty("nodes")));
        group.MapGet("/sources", async (SuwayomiClient client, CancellationToken ct) => Results.Ok((await client.SourcesAsync(ct)).GetProperty("sources").GetProperty("nodes")));
        group.MapGet("/extensions", async (SuwayomiClient client, CancellationToken ct) => Results.Ok(await client.ExtensionsAsync(ct)));

        group.MapGet("/{id:int}", async (int id, SuwayomiClient client, CancellationToken ct) => Results.Ok((await client.MangaAsync(id, ct)).GetProperty("manga")));
        group.MapGet("/{id:int}/chapters", async (int id, SuwayomiClient client, CancellationToken ct) => Results.Ok((await client.ChaptersAsync(id, ct)).GetProperty("manga").GetProperty("chapters").GetProperty("nodes")));
        group.MapGet("/{id:int}/chapters/{chapterId:int}/pages", async (int chapterId, SuwayomiClient client, CancellationToken ct) => Results.Ok((await client.FetchPagesAsync(chapterId, ct)).GetProperty("fetchChapterPages").GetProperty("pages")));

        group.MapPut("/{id:int}/favorite", async (int id, FavoriteRequest request, SuwayomiClient client, CancellationToken ct) => Results.Ok(await client.SetFavoriteAsync(id, request.Favorite, ct)));
        group.MapPut("/{id:int}/progress", async (int id, ProgressRequest request, SuwayomiClient client, CancellationToken ct) =>
        {
            if (!int.TryParse(request.Chapter, out var chapterId))
            {
                var chapters = (await client.ChaptersAsync(id, ct)).GetProperty("manga").GetProperty("chapters").GetProperty("nodes");
                var match = chapters.EnumerateArray().FirstOrDefault(x => string.Equals(x.GetProperty("name").GetString(), request.Chapter, StringComparison.OrdinalIgnoreCase));
                if (match.ValueKind == JsonValueKind.Undefined) return Results.NotFound();
                chapterId = match.GetProperty("id").GetInt32();
            }
            return Results.Ok(await client.SetChapterStateAsync(chapterId, true, request.LastPageRead, ct));
        });
        group.MapPut("/{id:int}/status", async (int id, StatusRequest request, SuwayomiClient client, CancellationToken ct) =>
        {
            // Suwayomi owns source publication status; oniDash maps "completed" to all chapters read.
            if (!string.Equals(request.Status, "completed", StringComparison.OrdinalIgnoreCase)) return Results.Ok();
            var chapters = (await client.ChaptersAsync(id, ct)).GetProperty("manga").GetProperty("chapters").GetProperty("nodes");
            var ids = chapters.EnumerateArray().Select(x => x.GetProperty("id").GetInt32()).ToArray();
            foreach (var chapterId in ids) await client.SetChapterStateAsync(chapterId, true, null, ct);
            return Results.Ok();
        });
        group.MapPost("/{id:int}/download", async (int id, DownloadRequest request, SuwayomiClient client, CancellationToken ct) =>
        {
            var chapters = (await client.ChaptersAsync(id, ct)).GetProperty("manga").GetProperty("chapters").GetProperty("nodes");
            var ids = request.Chapters.Select(x => int.TryParse(x, out var parsed) ? parsed : chapters.EnumerateArray().FirstOrDefault(c => string.Equals(c.GetProperty("name").GetString(), x, StringComparison.OrdinalIgnoreCase)).GetProperty("id").GetInt32()).ToArray();
            return Results.Ok(await client.DownloadAsync(ids, ct));
        });
        group.MapPost("/update", async (SuwayomiClient client, CancellationToken ct) => Results.Ok(await client.UpdateLibraryAsync(ct)));
        group.MapPost("/sources/install", async (InstallSourceRequest request, SuwayomiClient client, CancellationToken ct) => Results.Ok(await client.InstallExtensionAsync(request.SourceId, ct)));

        return app;
    }

    public sealed record FavoriteRequest(bool Favorite);
    public sealed record ProgressRequest(string Chapter, int? LastPageRead = null);
    public sealed record StatusRequest(string Status);
    public sealed record DownloadRequest(string[] Chapters);
    public sealed record InstallSourceRequest(string SourceId);
}
