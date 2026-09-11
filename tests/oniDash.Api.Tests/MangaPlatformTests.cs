using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using oniDash.Manga;
using oniDash.Manga.Domain;
using oniDash.Manga.Persistence;
using Xunit;

namespace oniDash.Api.Tests;

public sealed class MangaPlatformTests
{
    [Fact]
    public async Task Persists_progress_bookmarks_downloads_and_reads_cbz_pages()
    {
        await using var connection = new SqliteConnection("Filename=:memory:"); await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<MangaDbContext>().UseSqlite(connection).Options;
        await using var db = new MangaDbContext(options); await db.Database.MigrateAsync();
        var archivePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".cbz");
        using (var archive = ZipFile.Open(archivePath, ZipArchiveMode.Create)) { await using var page = archive.CreateEntry("001.jpg").Open(); await page.WriteAsync(new byte[] { 1, 2, 3 }); }
        try
        {
            var manga = new MangaTitle { Id = Guid.NewGuid(), LibraryId = Guid.NewGuid(), SourceId = "local", ExternalId = "one", Title = "One", NormalizedTitle = "one", UpdatedAtUtc = DateTimeOffset.UtcNow };
            var chapter = new MangaChapter { Id = Guid.NewGuid(), MangaId = manga.Id, SourceChapterId = "c1", Title = "Chapter 1", Number = 1, LocalPath = archivePath, PageCount = 1, UpdatedAtUtc = DateTimeOffset.UtcNow };
            db.AddRange(manga, chapter); await db.SaveChangesAsync();
            var catalogue = new MangaChapterCatalogue(db); await catalogue.SetProgressAsync(chapter.Id.ToString(), 1);
            var bookmark = await new MangaBookmarkStore(db).AddAsync(chapter.Id.ToString(), 0, "start");
            var queue = new MangaDownloadQueue(db); await queue.EnqueueAsync(new[] { chapter.Id.ToString() });
            Assert.True((await db.Chapters.SingleAsync()).Read); Assert.Equal("start", bookmark.Note); Assert.Single(await queue.ListPendingAsync());
            var reader = new MangaReader(db); Assert.Equal(1, await reader.GetPageCountAsync(chapter.Id.ToString())); await using var stream = await reader.OpenPageAsync(chapter.Id.ToString(), 0); Assert.NotNull(stream);
        }
        finally { File.Delete(archivePath); }
    }
}
