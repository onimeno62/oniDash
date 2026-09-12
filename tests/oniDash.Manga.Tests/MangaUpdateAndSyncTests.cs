using System;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using oniDash.Manga.Domain;
using oniDash.Manga.Persistence;
using Xunit;

namespace oniDash.Manga.Tests;

/// <summary>
/// The manga update/sync services are exercised against SQLite in memory (the production
/// provider) rather than the in-memory provider, so these tests also validate the real
/// InitialMangaPlatform migration and the SQLite type constraints the queries must obey.
/// </summary>
public class MangaUpdateAndSyncTests
{
    private static async Task<(MangaDbContext Db, SqliteConnection Connection)> CreateDatabaseAsync()
    {
        var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<MangaDbContext>()
            .UseSqlite(connection)
            .Options;
        var db = new MangaDbContext(options);
        await db.Database.MigrateAsync();
        return (db, connection);
    }

    [Fact]
    public async Task CheckForUpdates_CreatesNotificationsForNewChapters()
    {
        var (db, connection) = await CreateDatabaseAsync();
        await using (connection)
        await using (db)
        {
            var mangaId = Guid.NewGuid();
            db.Titles.Add(new MangaTitle
            {
                Id = mangaId,
                Title = "Berserk",
                NormalizedTitle = "berserk",
                SourceId = "local",
                ExternalId = "berserk-1"
            });
            db.Chapters.Add(new MangaChapter
            {
                Id = Guid.NewGuid(),
                MangaId = mangaId,
                SourceChapterId = "c1",
                Title = "Chapter 1",
                Number = 1
            });
            await db.SaveChangesAsync();

            var updateService = new MangaUpdateService(db);
            var created = await updateService.CheckForUpdatesAsync();

            Assert.Equal(1, created);
            var notifications = await updateService.GetNotificationsAsync();
            Assert.Single(notifications);
            Assert.Equal("Berserk", notifications[0].MangaTitle);
            Assert.False(notifications[0].Read);

            await updateService.MarkNotificationReadAsync(notifications[0].Id);
            var unread = await updateService.GetNotificationsAsync(unreadOnly: true);
            Assert.Empty(unread);
        }
    }

    [Fact]
    public async Task SyncService_SavesAndSyncsReadChapters()
    {
        var (db, connection) = await CreateDatabaseAsync();
        await using (connection)
        await using (db)
        {
            var mangaId = Guid.NewGuid();
            db.Titles.Add(new MangaTitle
            {
                Id = mangaId,
                Title = "One Piece",
                NormalizedTitle = "one-piece",
                SourceId = "local",
                ExternalId = "op-1"
            });
            db.Chapters.Add(new MangaChapter
            {
                Id = Guid.NewGuid(),
                MangaId = mangaId,
                SourceChapterId = "c100",
                Title = "Chapter 100",
                Number = 100,
                Read = true
            });
            await db.SaveChangesAsync();

            var syncService = new MangaSyncService(db);
            await syncService.SaveTrackerAsync(mangaId, "AniList", "12345", 50, "reading", 9);

            var trackers = await syncService.GetTrackersAsync(mangaId);
            Assert.Single(trackers);
            Assert.Equal("AniList", trackers[0].TrackerName);
            Assert.Equal(50, trackers[0].LastSyncedChapter);

            var syncedCount = await syncService.SyncAllAsync();
            Assert.Equal(1, syncedCount);

            var updatedTrackers = await syncService.GetTrackersAsync(mangaId);
            Assert.Equal(100, updatedTrackers[0].LastSyncedChapter);
        }
    }
}
