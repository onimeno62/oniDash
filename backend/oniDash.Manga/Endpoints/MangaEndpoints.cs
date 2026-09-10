using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace oniDash.Manga.Endpoints;

public static class MangaEndpoints
{
    public static IEndpointRouteBuilder MapMangaEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/manga");
        group.MapGet("/health", async (SuwayomiClient client, CancellationToken ct) =>
        {
            try { return Results.Ok(await client.QueryAsync("query{ aboutServer { name version } }", null, ct)); }
            catch (Exception ex) { return Results.Json(new { connected = false, error = ex.Message }, statusCode: StatusCodes.Status503ServiceUnavailable); }
        });
        group.MapGet("/library", async (SuwayomiClient client, CancellationToken ct) => Results.Ok(MapMangas((await client.LibraryAsync(ct)).GetProperty("mangas").GetProperty("nodes"))));
        group.MapGet("/continue-reading", async (SuwayomiClient client, CancellationToken ct) =>
        {
            var items = MapMangas((await client.LibraryAsync(ct)).GetProperty("mangas").GetProperty("nodes")).EnumerateArray();
            return Results.Ok(items.Where(x => x.GetProperty("readCount").GetInt32() > 0).Take(12));
        });
        group.MapGet("/updates", async (SuwayomiClient client, CancellationToken ct) =>
        {
            var updates = (await client.UpdatesAsync(ct)).GetProperty("libraryUpdateStatus").GetProperty("mangaUpdates");
            var mapped = updates.EnumerateArray().Select(u =>
            {
                var manga = u.GetProperty("manga");
                var chapter = manga.GetProperty("latestUploadedChapter");
                var source = manga.TryGetProperty("source", out var s) && s.ValueKind != JsonValueKind.Null ? s.GetProperty("name").GetString() : null;
                return new { id = $"{manga.GetProperty("id").GetInt32()}-{chapter.GetProperty("id").GetInt32()}", mangaId = manga.GetProperty("id").GetInt32().ToString(), mangaTitle = manga.GetProperty("title").GetString(), coverUrl = manga.GetProperty("thumbnailUrl").GetString(), chapter = chapter.GetProperty("name").GetString(), publishedAtUtc = chapter.GetProperty("uploadDate").GetString(), sourceName = source, downloaded = false, read = chapter.GetProperty("isRead").GetBoolean() };
            });
            return Results.Ok(mapped);
        });
        group.MapGet("/categories", async (SuwayomiClient client, CancellationToken ct) => Results.Ok((await client.CategoriesAsync(ct)).GetProperty("categories").GetProperty("nodes").EnumerateArray().Select(x => new { id = x.GetProperty("id").GetInt32().ToString(), name = x.GetProperty("name").GetString(), count = x.GetProperty("mangas").GetProperty("totalCount").GetInt32() })));
        group.MapGet("/sources", async (SuwayomiClient client, CancellationToken ct) => Results.Ok((await client.SourcesAsync(ct)).GetProperty("sources").GetProperty("nodes").EnumerateArray().Select(x => new { id = x.GetProperty("id").GetString(), name = x.GetProperty("name").GetString(), language = x.GetProperty("lang").GetString(), installed = x.GetProperty("extension").GetProperty("isInstalled").GetBoolean(), enabled = x.GetProperty("extension").GetProperty("isInstalled").GetBoolean(), mangaCount = 0, iconUrl = x.GetProperty("iconUrl").GetString() })));
        group.MapGet("/extensions", async (SuwayomiClient client, CancellationToken ct) => Results.Ok(await client.ExtensionsAsync(ct)));
        group.MapGet("/browse/popular", async (SuwayomiClient client, CancellationToken ct) => Results.Ok(await Browse(client, "POPULAR", null, ct)));
        group.MapGet("/search", async (string q, SuwayomiClient client, CancellationToken ct) => Results.Ok(await Browse(client, "SEARCH", q, ct)));
        group.MapGet("/{id:int}", async (int id, SuwayomiClient client, CancellationToken ct) => Results.Ok((await client.MangaAsync(id, ct)).GetProperty("manga")));
        group.MapGet("/{id:int}/chapters", async (int id, SuwayomiClient client, CancellationToken ct) => Results.Ok((await client.ChaptersAsync(id, ct)).GetProperty("manga").GetProperty("chapters").GetProperty("nodes")));
        group.MapGet("/{id:int}/chapters/{chapterId:int}/pages", async (int chapterId, SuwayomiClient client, CancellationToken ct) => Results.Ok((await client.FetchPagesAsync(chapterId, ct)).GetProperty("fetchChapterPages").GetProperty("pages")));
        group.MapPut("/{id:int}/favorite", async (int id, FavoriteRequest request, SuwayomiClient client, CancellationToken ct) => Results.Ok(await client.SetFavoriteAsync(id, request.Favorite, ct)));
        group.MapPut("/{id:int}/progress", async (int id, ProgressRequest request, SuwayomiClient client, CancellationToken ct) =>
        {
            var chapters = (await client.ChaptersAsync(id, ct)).GetProperty("manga").GetProperty("chapters").GetProperty("nodes");
            var chapter = int.TryParse(request.Chapter, out var numericId) ? chapters.EnumerateArray().FirstOrDefault(x => x.GetProperty("id").GetInt32() == numericId) : chapters.EnumerateArray().FirstOrDefault(x => string.Equals(x.GetProperty("name").GetString(), request.Chapter, StringComparison.OrdinalIgnoreCase));
            if (chapter.ValueKind == JsonValueKind.Undefined) return Results.NotFound();
            return Results.Ok(await client.SetChapterStateAsync(chapter.GetProperty("id").GetInt32(), true, request.LastPageRead, ct));
        });
        group.MapPut("/{id:int}/status", async (int id, StatusRequest request, SuwayomiClient client, CancellationToken ct) =>
        {
            if (!string.Equals(request.Status, "completed", StringComparison.OrdinalIgnoreCase)) return Results.Ok();
            var chapters = (await client.ChaptersAsync(id, ct)).GetProperty("manga").GetProperty("chapters").GetProperty("nodes");
            foreach (var chapter in chapters.EnumerateArray()) await client.SetChapterStateAsync(chapter.GetProperty("id").GetInt32(), true, null, ct);
            return Results.Ok();
        });
        group.MapPost("/{id:int}/download", async (int id, DownloadRequest request, SuwayomiClient client, CancellationToken ct) =>
        {
            var chapters = (await client.ChaptersAsync(id, ct)).GetProperty("manga").GetProperty("chapters").GetProperty("nodes");
            var ids = request.Chapters.Select(value => int.TryParse(value, out var numericId) ? numericId : chapters.EnumerateArray().Where(c => string.Equals(c.GetProperty("name").GetString(), value, StringComparison.OrdinalIgnoreCase)).Select(c => c.GetProperty("id").GetInt32()).FirstOrDefault()).Where(x => x > 0).ToArray();
            return Results.Ok(await client.DownloadAsync(ids, ct));
        });
        group.MapPost("/update", async (SuwayomiClient client, CancellationToken ct) => Results.Ok(await client.UpdateLibraryAsync(ct)));
        group.MapPost("/sources/install", async (InstallSourceRequest request, SuwayomiClient client, CancellationToken ct) =>
        {
            var source = (await client.SourcesAsync(ct)).GetProperty("sources").GetProperty("nodes").EnumerateArray().FirstOrDefault(x => x.GetProperty("id").GetString() == request.SourceId);
            if (source.ValueKind == JsonValueKind.Undefined) return Results.NotFound();
            var packageName = source.GetProperty("extension").GetProperty("pkgName").GetString();
            return Results.Ok(await client.InstallExtensionAsync(packageName!, ct));
        });
        return app;
    }

    private static async Task<object[]> Browse(SuwayomiClient client, string type, string? query, CancellationToken ct)
    {
        var sources = (await client.SourcesAsync(ct)).GetProperty("sources").GetProperty("nodes").EnumerateArray().Where(x => x.GetProperty("extension").GetProperty("isInstalled").GetBoolean()).Take(8).ToArray();
        var results = new List<object>();
        foreach (var source in sources)
        {
            try
            {
                var mangas = (await client.BrowseSourceAsync(source.GetProperty("id").GetString()!, type, query, ct)).GetProperty("fetchSourceManga").GetProperty("mangas");
                results.AddRange(MapMangas(mangas).EnumerateArray().Select(x => (object)x));
            }
            catch { }
        }
        return results.Take(24).ToArray();
    }

    private static JsonElement MapMangas(JsonElement nodes)
    {
        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(nodes.EnumerateArray().Select(MapManga)));
        return doc.RootElement.Clone();
    }

    private static object MapManga(JsonElement x)
    {
        var chapters = x.TryGetProperty("chapters", out var c) && c.ValueKind == JsonValueKind.Object ? c.GetProperty("totalCount").GetInt32() : 0;
        var unread = x.TryGetProperty("unreadCount", out var u) ? u.GetInt32() : 0;
        var read = Math.Max(0, chapters - unread);
        var progress = chapters == 0 ? 0 : Math.Clamp(read * 100.0 / chapters, 0, 100);
        var source = x.TryGetProperty("source", out var s) && s.ValueKind != JsonValueKind.Null ? s : default;
        var latest = x.TryGetProperty("latestUploadedChapter", out var l) && l.ValueKind != JsonValueKind.Null ? l : default;
        var status = x.TryGetProperty("status", out var st) ? st.GetString() : "UNKNOWN";
        return new { id = x.GetProperty("id").GetInt32().ToString(), title = x.GetProperty("title").GetString(), author = x.GetProperty("author").GetString(), artist = x.GetProperty("artist").GetString(), description = x.GetProperty("description").GetString(), coverUrl = x.GetProperty("thumbnailUrl").GetString(), sourceId = x.GetProperty("sourceId").GetString(), sourceName = source.ValueKind == JsonValueKind.Object ? source.GetProperty("name").GetString() : null, genres = x.GetProperty("genre").EnumerateArray().Select(g => g.GetString()).ToArray(), status = x.GetProperty("inLibrary").GetBoolean() ? (unread == 0 && chapters > 0 ? "completed" : "reading") : "plan_to_read", chapterCount = chapters, unreadCount = unread, readCount = read, latestChapter = latest.ValueKind == JsonValueKind.Object ? latest.GetProperty("name").GetString() : null, favorite = x.GetProperty("inLibrary").GetBoolean(), inLibrary = x.GetProperty("inLibrary").GetBoolean(), downloadCount = x.GetProperty("downloadCount").GetInt32(), progressPercent = progress, year = (int?)null, publicationStatus = status };
    }

    public sealed record FavoriteRequest(bool Favorite);
    public sealed record ProgressRequest(string Chapter, int? LastPageRead = null);
    public sealed record StatusRequest(string Status);
    public sealed record DownloadRequest(string[] Chapters);
    public sealed record InstallSourceRequest(string SourceId);
}
