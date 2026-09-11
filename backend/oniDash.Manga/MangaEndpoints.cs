using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using oniDash.Application.Catalogue;
using oniDash.Manga.Persistence;

namespace oniDash.Manga;

public static class MangaEndpoints
{
    public static IEndpointRouteBuilder MapMangaPluginEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api/manga").WithTags("Manga");
        api.MapGet("/library", async (MangaDbContext db, Guid? libraryId, int? limit, CancellationToken ct) => { var query = db.Titles.AsNoTracking(); if (libraryId is not null) query = query.Where(x => x.LibraryId == libraryId); return Results.Ok(await query.OrderBy(x => x.NormalizedTitle).Take(Math.Clamp(limit ?? 100, 1, 500)).ToListAsync(ct)); });
        api.MapGet("/search", async (IMangaSourceAdapter source, string q, CancellationToken ct) => Results.Ok(await source.SearchAsync(q, ct)));
        api.MapGet("/browse/popular", async (IMangaSourceAdapter source, CancellationToken ct) => Results.Ok(await source.PopularAsync(ct)));
        api.MapGet("/updates", async (IMangaSourceAdapter source, CancellationToken ct) => Results.Ok(await source.LatestAsync(ct)));
        api.MapGet("/{mangaId:guid}/chapters", async (IMangaChapterCatalogue catalogue, Guid mangaId, CancellationToken ct) => Results.Ok(await catalogue.ListAsync(mangaId, ct)));
        api.MapPut("/chapters/{chapterId:guid}/progress", async (IMangaChapterCatalogue catalogue, Guid chapterId, ProgressRequest request, CancellationToken ct) => { await catalogue.SetProgressAsync(chapterId.ToString(), request.Progress, ct); return Results.NoContent(); });
        api.MapGet("/chapters/{chapterId:guid}/bookmarks", async (IMangaBookmarkStore store, Guid chapterId, CancellationToken ct) => Results.Ok(await store.ListAsync(chapterId.ToString(), ct)));
        api.MapPost("/chapters/{chapterId:guid}/bookmarks", async (IMangaBookmarkStore store, Guid chapterId, BookmarkRequest request, CancellationToken ct) => Results.Ok(await store.AddAsync(chapterId.ToString(), request.Page, request.Note, ct)));
        api.MapDelete("/bookmarks/{bookmarkId:guid}", async (IMangaBookmarkStore store, Guid bookmarkId, CancellationToken ct) => { await store.RemoveAsync(bookmarkId.ToString(), ct); return Results.NoContent(); });
        api.MapGet("/extensions", async (IMediaPluginManager manager, CancellationToken ct) => Results.Ok(await manager.ListAsync(ct)));
        api.MapPost("/extensions/{pluginId}/install", async (IMediaPluginManager manager, string pluginId, CancellationToken ct) => { var result = await manager.InstallAsync(pluginId, ct); return result is null ? Results.NotFound() : Results.Ok(result); });
        api.MapPut("/extensions/{pluginId}/enabled", async (IMediaPluginManager manager, string pluginId, EnabledRequest request, CancellationToken ct) => await manager.SetEnabledAsync(pluginId, request.Enabled, ct) ? Results.NoContent() : Results.NotFound());
        api.MapPost("/downloads", async (IMangaDownloadQueue queue, DownloadRequest request, CancellationToken ct) => { await queue.EnqueueAsync(request.ChapterIds, ct); return Results.Accepted(); });
        api.MapGet("/downloads/pending", async (IMangaDownloadQueue queue, CancellationToken ct) => Results.Ok(await queue.ListPendingAsync(ct)));
        api.MapGet("/chapters/{chapterId:guid}/pages", async (IMediaReader reader, Guid chapterId, CancellationToken ct) => { var count = await reader.GetPageCountAsync(chapterId.ToString(), ct); return count is null ? Results.NotFound() : Results.Ok(new { pageCount = count }); });
        api.MapGet("/chapters/{chapterId:guid}/pages/{page:int}", async (IMediaReader reader, Guid chapterId, int page, CancellationToken ct) => { var stream = await reader.OpenPageAsync(chapterId.ToString(), page, ct); return stream is null ? Results.NotFound() : Results.Stream(stream, "application/octet-stream"); });
        return app;
    }

    public sealed record ProgressRequest(double Progress);
    public sealed record BookmarkRequest(int Page, string? Note);
    public sealed record EnabledRequest(bool Enabled);
    public sealed record DownloadRequest(IReadOnlyCollection<string> ChapterIds);
}
