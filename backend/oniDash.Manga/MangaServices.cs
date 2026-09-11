using System.IO.Compression;
using Microsoft.EntityFrameworkCore;
using oniDash.Application.Catalogue;
using oniDash.Manga.Domain;
using oniDash.Manga.Persistence;

namespace oniDash.Manga;

public interface IMangaSourceAdapter : IMangaChapterSource
{
    string Id { get; }
    Task<IReadOnlyList<string>> PopularAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> LatestAsync(CancellationToken cancellationToken = default);
}

public sealed class LocalMangaSource(MangaDbContext db) : IMangaSourceAdapter
{
    public string Id => "local";
    public async Task<IReadOnlyList<string>> SearchAsync(string query, CancellationToken ct = default) => await db.Titles.AsNoTracking().Where(x => x.Title.Contains(query)).OrderBy(x => x.Title).Select(x => x.Title).Take(100).ToListAsync(ct);
    public async Task<IReadOnlyList<string>> PopularAsync(CancellationToken ct = default) => await db.Titles.AsNoTracking().OrderByDescending(x => x.Favorite).ThenBy(x => x.Title).Select(x => x.Title).Take(50).ToListAsync(ct);
    public async Task<IReadOnlyList<string>> LatestAsync(CancellationToken ct = default) => await db.Titles.AsNoTracking().OrderByDescending(x => x.UpdatedAtUtc).Select(x => x.Title).Take(50).ToListAsync(ct);
    public async Task<Stream?> OpenChapterAsync(string chapterId, CancellationToken ct = default) { if (!Guid.TryParse(chapterId, out var id)) return null; var path = await db.Chapters.AsNoTracking().Where(x => x.Id == id).Select(x => x.LocalPath).SingleOrDefaultAsync(ct); return path is not null && File.Exists(path) ? File.OpenRead(path) : null; }
}

public sealed class MangaPluginManager(MangaDbContext db) : IMediaPluginManager
{
    public async Task<IReadOnlyList<PluginDescriptor>> ListAsync(CancellationToken ct = default) => await db.Plugins.AsNoTracking().OrderBy(x => x.Name).Select(x => new PluginDescriptor(x.Id, x.Name, x.Version, Enum.Parse<PluginLifecycleState>(x.State), new HashSet<string> { "search", "popular", "latest", "chapters" })).ToListAsync(ct);
    public async Task<PluginDescriptor?> InstallAsync(string pluginId, CancellationToken ct = default) { var plugin = await db.Plugins.SingleOrDefaultAsync(x => x.Id == pluginId, ct) ?? new MangaPlugin { Id = pluginId, Name = pluginId, Version = "1.0" }; plugin.Installed = true; plugin.Enabled = true; plugin.State = PluginLifecycleState.Enabled.ToString(); if (db.Entry(plugin).State == EntityState.Detached) db.Plugins.Add(plugin); await db.SaveChangesAsync(ct); return new PluginDescriptor(plugin.Id, plugin.Name, plugin.Version, PluginLifecycleState.Enabled, new HashSet<string> { "search", "popular", "latest", "chapters" }); }
    public async Task<bool> SetEnabledAsync(string pluginId, bool enabled, CancellationToken ct = default) { var plugin = await db.Plugins.SingleOrDefaultAsync(x => x.Id == pluginId, ct); if (plugin is null || !plugin.Installed) return false; plugin.Enabled = enabled; plugin.State = (enabled ? PluginLifecycleState.Enabled : PluginLifecycleState.Disabled).ToString(); await db.SaveChangesAsync(ct); return true; }
}

public sealed class MangaChapterCatalogue(MangaDbContext db) : IMangaChapterCatalogue
{
    public async Task<IReadOnlyList<MangaChapterSummary>> ListAsync(Guid mangaId, CancellationToken ct = default) => await db.Chapters.AsNoTracking().Where(x => x.MangaId == mangaId).OrderBy(x => x.Number).Select(x => new MangaChapterSummary(x.Id.ToString(), x.MangaId, x.Title, (int)x.Number, x.LocalPath != null, x.PageCount > 0 ? (double)x.CurrentPage / x.PageCount : null)).ToListAsync(ct);
    public async Task SetProgressAsync(string chapterId, double progress, CancellationToken ct = default) { if (!Guid.TryParse(chapterId, out var id)) throw new ArgumentException("Invalid chapter id", nameof(chapterId)); var chapter = await db.Chapters.SingleAsync(x => x.Id == id, ct); var normalized = Math.Clamp(progress, 0, 1); chapter.CurrentPage = chapter.PageCount is > 0 ? (int)Math.Round(chapter.PageCount.Value * normalized) : chapter.CurrentPage; chapter.Read = normalized >= .95; chapter.LastReadAtUtc = DateTimeOffset.UtcNow; chapter.UpdatedAtUtc = DateTimeOffset.UtcNow; await db.SaveChangesAsync(ct); }
}

public sealed class MangaBookmarkStore(MangaDbContext db) : IMangaBookmarkStore
{
    public async Task<IReadOnlyList<MangaBookmark>> ListAsync(string chapterId, CancellationToken ct = default) { if (!Guid.TryParse(chapterId, out var id)) return Array.Empty<MangaBookmark>(); return await db.Bookmarks.AsNoTracking().Where(x => x.ChapterId == id).OrderBy(x => x.Page).Select(x => new MangaBookmark(x.Id.ToString(), x.ChapterId.ToString(), x.Page, x.Note, x.CreatedAtUtc)).ToListAsync(ct); }
    public async Task<MangaBookmark> AddAsync(string chapterId, int page, string? note, CancellationToken ct = default) { if (!Guid.TryParse(chapterId, out var id)) throw new ArgumentException("Invalid chapter id", nameof(chapterId)); var entity = new MangaBookmarkEntity { Id = Guid.NewGuid(), ChapterId = id, Page = Math.Max(0, page), Note = note, CreatedAtUtc = DateTimeOffset.UtcNow }; db.Bookmarks.Add(entity); await db.SaveChangesAsync(ct); return new MangaBookmark(entity.Id.ToString(), chapterId, entity.Page, entity.Note, entity.CreatedAtUtc); }
    public async Task RemoveAsync(string bookmarkId, CancellationToken ct = default) { if (!Guid.TryParse(bookmarkId, out var id)) return; var entity = await db.Bookmarks.SingleOrDefaultAsync(x => x.Id == id, ct); if (entity is null) return; db.Remove(entity); await db.SaveChangesAsync(ct); }
}

public sealed class MangaDownloadQueue(MangaDbContext db) : IMangaDownloadQueue
{
    public async Task EnqueueAsync(IReadOnlyCollection<string> chapterIds, CancellationToken ct = default) { var now = DateTimeOffset.UtcNow; foreach (var value in chapterIds) if (Guid.TryParse(value, out var id) && !await db.Downloads.AnyAsync(x => x.ChapterId == id && x.State == "Pending", ct)) db.Downloads.Add(new MangaDownload { Id = Guid.NewGuid(), ChapterId = id, CreatedAtUtc = now, UpdatedAtUtc = now }); await db.SaveChangesAsync(ct); }
    public async Task<IReadOnlyList<string>> ListPendingAsync(CancellationToken ct = default) => await db.Downloads.AsNoTracking().Where(x => x.State == "Pending").OrderBy(x => x.CreatedAtUtc).Select(x => x.ChapterId.ToString()).ToListAsync(ct);
}

public sealed class MangaReader(MangaDbContext db) : IMediaReader
{
    public async Task<int?> GetPageCountAsync(string documentId, CancellationToken ct = default) { var path = await PathFor(documentId, ct); if (path is null) return null; using var archive = ZipFile.OpenRead(path); return archive.Entries.Count(x => IsImage(x.FullName)); }
    public async Task<Stream?> OpenPageAsync(string documentId, int page, CancellationToken ct = default) { var path = await PathFor(documentId, ct); if (path is null || page < 0) return null; using var archive = ZipFile.OpenRead(path); var entry = archive.Entries.Where(x => IsImage(x.FullName)).OrderBy(x => x.FullName, StringComparer.OrdinalIgnoreCase).ElementAtOrDefault(page); if (entry is null) return null; var output = new MemoryStream(); await using var input = entry.Open(); await input.CopyToAsync(output, ct); output.Position = 0; return output; }
    private async Task<string?> PathFor(string id, CancellationToken ct) { if (!Guid.TryParse(id, out var guid)) return null; var path = await db.Chapters.AsNoTracking().Where(x => x.Id == guid).Select(x => x.LocalPath).SingleOrDefaultAsync(ct); return path is not null && File.Exists(path) && (Path.GetExtension(path).Equals(".cbz", StringComparison.OrdinalIgnoreCase) || Path.GetExtension(path).Equals(".zip", StringComparison.OrdinalIgnoreCase)) ? path : null; }
    private static bool IsImage(string path) => new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" }.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);
}
