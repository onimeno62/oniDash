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
        api.MapGet("/continue-reading", async (MangaDbContext db, int? limit, CancellationToken ct) => Results.Ok(await (from title in db.Titles.AsNoTracking() join chapter in db.Chapters.AsNoTracking() on title.Id equals chapter.MangaId where chapter.CurrentPage > 0 && !chapter.Read orderby chapter.LastReadAtUtc descending select title).Distinct().Take(Math.Clamp(limit ?? 12, 1, 100)).ToListAsync(ct)));
        api.MapGet("/search", async (IMangaSourceAdapter source, string q, CancellationToken ct) => Results.Ok(await source.SearchAsync(q, ct)));
        api.MapGet("/browse/popular", async (IMangaSourceAdapter source, CancellationToken ct) => Results.Ok(await source.PopularAsync(ct)));
        api.MapGet("/updates", async (IMangaSourceAdapter source, CancellationToken ct) => Results.Ok(await source.LatestAsync(ct)));
        api.MapGet("/categories", () => Results.Ok(Array.Empty<object>()));
        api.MapGet("/sources", async (IMangaPluginManager manager, CancellationToken ct) => Results.Ok(await manager.ListAsync(ct)));
        api.MapPut("/{mangaId:guid}/favorite", async (MangaDbContext db, Guid mangaId, FavoriteRequest request, CancellationToken ct) => { var title = await db.Titles.SingleOrDefaultAsync(x => x.Id == mangaId, ct); if (title is null) return Results.NotFound(); title.Favorite = request.Favorite; title.UpdatedAtUtc = DateTimeOffset.UtcNow; await db.SaveChangesAsync(ct); return Results.NoContent(); });
        api.MapPut("/{mangaId:guid}/status", () => Results.NoContent());
        api.MapPut("/{mangaId:guid}/progress", async (MangaDbContext db, IMangaChapterCatalogue catalogue, Guid mangaId, LegacyProgressRequest request, CancellationToken ct) => { var chapter = await db.Chapters.Where(x => x.MangaId == mangaId && x.SourceChapterId == request.Chapter).OrderByDescending(x => x.Number).FirstOrDefaultAsync(ct); if (chapter is null) return Results.NotFound(); await catalogue.SetProgressAsync(chapter.Id.ToString(), 1, ct); return Results.NoContent(); });
        api.MapGet("/{mangaId:guid}/chapters", async (IMangaChapterCatalogue catalogue, Guid mangaId, CancellationToken ct) => Results.Ok(await catalogue.ListAsync(mangaId, ct)));
        api.MapPut("/chapters/{chapterId:guid}/progress", async (IMangaChapterCatalogue catalogue, Guid chapterId, ProgressRequest request, CancellationToken ct) => { await catalogue.SetProgressAsync(chapterId.ToString(), request.Progress, ct); return Results.NoContent(); });
        api.MapGet("/chapters/{chapterId:guid}/bookmarks", async (IMangaBookmarkStore store, Guid chapterId, CancellationToken ct) => Results.Ok(await store.ListAsync(chapterId.ToString(), ct)));
        api.MapPost("/chapters/{chapterId:guid}/bookmarks", async (IMangaBookmarkStore store, Guid chapterId, BookmarkRequest request, CancellationToken ct) => Results.Ok(await store.AddAsync(chapterId.ToString(), request.Page, request.Note, ct)));
        api.MapDelete("/bookmarks/{bookmarkId:guid}", async (IMangaBookmarkStore store, Guid bookmarkId, CancellationToken ct) => { await store.RemoveAsync(bookmarkId.ToString(), ct); return Results.NoContent(); });
        api.MapGet("/extensions", async (IMangaPluginManager manager, CancellationToken ct) => Results.Ok(await manager.ListAsync(ct)));
        api.MapPost("/extensions/{pluginId}/install", async (IMangaPluginManager manager, string pluginId, CancellationToken ct) => Results.Ok(await manager.InstallAsync(pluginId, ct)));
        api.MapPost("/extensions/{pluginId}/update", async (IMangaPluginManager manager, string pluginId, CancellationToken ct) => { var result = await manager.UpdateAsync(pluginId, ct); return result is null ? Results.NotFound() : Results.Ok(result); });
        api.MapPut("/extensions/{pluginId}/enabled", async (IMangaPluginManager manager, string pluginId, EnabledRequest request, CancellationToken ct) => await manager.SetEnabledAsync(pluginId, request.Enabled, ct) ? Results.NoContent() : Results.NotFound());
        api.MapPost("/sources/install", async (IMangaPluginManager manager, InstallRequest request, CancellationToken ct) => Results.Ok(await manager.InstallAsync(request.SourceId, ct)));
        api.MapPost("/{mangaId:guid}/download", async (MangaDbContext db, IMangaDownloadQueue queue, Guid mangaId, LegacyDownloadRequest request, CancellationToken ct) => { var ids = await db.Chapters.Where(x => x.MangaId == mangaId && request.Chapters.Contains(x.SourceChapterId)).Select(x => x.Id.ToString()).ToListAsync(ct); await queue.EnqueueAsync(ids, ct); return Results.Accepted(); });
        api.MapPost("/downloads", async (IMangaDownloadQueue queue, DownloadRequest request, CancellationToken ct) => { await queue.EnqueueAsync(request.ChapterIds, ct); return Results.Accepted(); });
        api.MapGet("/downloads/pending", async (IMangaDownloadQueue queue, CancellationToken ct) => Results.Ok(await queue.ListPendingAsync(ct)));
        api.MapPost("/downloads/process", async (IMangaDownloadProcessor processor, CancellationToken ct) => Results.Ok(new { completed = await processor.ProcessPendingAsync(ct) }));
        api.MapGet("/chapters/{chapterId:guid}/pages", async (IMediaReader reader, Guid chapterId, CancellationToken ct) => { var count = await reader.GetPageCountAsync(chapterId.ToString(), ct); return count is null ? Results.NotFound() : Results.Ok(new { pageCount = count }); });
        api.MapGet("/chapters/{chapterId:guid}/pages/{page:int}", async (IMediaReader reader, Guid chapterId, int page, CancellationToken ct) => { var stream = await reader.OpenPageAsync(chapterId.ToString(), page, ct); return stream is null ? Results.NotFound() : Results.Stream(stream, "application/octet-stream"); });
        api.MapPost("/update", () => Results.Accepted());
        return app;
    }

    public sealed record ProgressRequest(double Progress); public sealed record LegacyProgressRequest(string Chapter); public sealed record BookmarkRequest(int Page, string? Note); public sealed record EnabledRequest(bool Enabled); public sealed record FavoriteRequest(bool Favorite); public sealed record InstallRequest(string SourceId); public sealed record DownloadRequest(IReadOnlyCollection<string> ChapterIds); public sealed record LegacyDownloadRequest(IReadOnlyCollection<string> Chapters);
}
