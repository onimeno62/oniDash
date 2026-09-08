using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using oniDash.Movies.Cataloging;
using Xunit;

namespace oniDash.Movies.Tests;

public sealed class MovieReindexServiceTests
{
    private readonly MovieTestDatabase _database = new();
    private readonly MovieCatalogService _service;

    public MovieReindexServiceTests()
    {
        _service = new MovieCatalogService(
            _database.CreateContext(),
            new FakeVideoProbeReader(),
            new FakeVideoArtworkReader(),
            NullLogger<MovieCatalogService>.Instance);
    }

    private async Task InsertFileAsync(Guid libraryId, Guid itemId, string relativePath, string extension)
    {
        await using var db = _database.CreateContext();
        var sourceId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        await db.Database.ExecuteSqlRawAsync(
            "INSERT INTO Sources (Id, LibraryId, Name, RootPath, CreatedAtUtc) VALUES ({0}, {1}, {2}, {3}, {4})",
            sourceId, libraryId, "src", @"C:\Video", DateTime.UtcNow);
        await db.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO Files (Id, MediaItemId, LibrarySourceId, RelativePath, IdentityKey, Extension, SizeBytes, LastWriteTimeUtc, MissingSinceUtc, CreatedAtUtc)
            VALUES ({0}, {1}, {2}, {3}, {4}, {5}, 1, {6}, NULL, {6})
            """,
            fileId, itemId, sourceId, relativePath, $"key-{fileId}", extension, DateTime.UtcNow.AddDays(-1));
    }

    [Fact]
    public async Task Reindexes_only_files_without_a_fresh_movie_row()
    {
        var libraryId = Guid.NewGuid();
        await using var db = _database.CreateContext();
        await db.Database.ExecuteSqlRawAsync(
            "INSERT INTO Libraries (Id, Name, CreatedAtUtc) VALUES ({0}, {1}, {2})",
            libraryId, "lib", DateTime.UtcNow);

        var item1 = Guid.NewGuid();
        var item2 = Guid.NewGuid();
        await db.Database.ExecuteSqlRawAsync(
            "INSERT INTO MediaItems (Id, LibraryId, DisplayName, CreatedAtUtc) VALUES ({0}, {1}, {2}, {3})",
            item1, libraryId, "one", DateTime.UtcNow);
        await db.Database.ExecuteSqlRawAsync(
            "INSERT INTO MediaItems (Id, LibraryId, DisplayName, CreatedAtUtc) VALUES ({0}, {1}, {2}, {3})",
            item2, libraryId, "two", DateTime.UtcNow);
        await InsertFileAsync(libraryId, item1, "fresh.mkv", ".mkv");
        await InsertFileAsync(libraryId, item2, "stale.mkv", ".mkv");

        // Index item1 directly (like a scan did): its movie row is fresh, so only
        // item2 should be a reindex candidate.
        Assert.True(await _service.IndexFileAsync(libraryId, item1, Guid.NewGuid(), Path.Combine(@"C:\Video", "fresh.mkv")));

        var reindex = new MovieReindexService(db, _service, NullLogger<MovieReindexService>.Instance);
        var indexed = await reindex.ReindexAsync(libraryId);

        Assert.Equal(1, indexed);
        var movies = await db.Movies.OrderBy(m => m.Title).ToListAsync();
        Assert.Equal(2, movies.Count);
        Assert.Contains(movies, m => m.MediaItemId == item2 && m.Title == "stale");
    }
}
