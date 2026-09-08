using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using oniDash.Music.Cataloging;
using oniDash.Music.Persistence;
using oniDash.Music.Tagging;
using Xunit;

namespace oniDash.Music.Tests;

/// <summary>
/// Catalogue grouping/upsert logic against a real (in-memory) SQLite database with the
/// plugin's migration applied — tags come from a scripted reader so tests do not depend
/// on audio file internals. Core rows (MediaItems) are created exactly like the scan
/// pipeline would, so cross-catalogue foreign keys hold.
/// </summary>
public sealed class MusicCatalogServiceTests : IDisposable
{
    private readonly MusicTestDatabase _database = new();
    private readonly FakeAudioTagReader _tags = new();
    private readonly MusicCatalogService _service;

    public MusicCatalogServiceTests()
    {
        _service = new MusicCatalogService(
            _database.CreateContext(), _tags, NullLogger<MusicCatalogService>.Instance);
    }

    public void Dispose() => _database.Dispose();

    private static AudioTags Tags(
        string? title = "Untitled",
        string? artist = "Artist",
        string? albumArtist = null,
        string? album = "Album",
        int? trackNumber = null,
        int? year = 2024,
        byte[]? cover = null) => new(
        title, artist, albumArtist, album, trackNumber, null, year, 180.5, "Rock", cover,
        cover is null ? null : "image/jpeg");

    private static string PathFor(string name) =>
        Path.Combine(Path.GetTempPath(), $"onidash-music-{name}.mp3");

    /// <summary>Creates a core MediaItems row in the given library (like the scan does).</summary>
    private async Task<Guid> CreateCoreItemAsync(Guid libraryId)
    {
        var itemId = Guid.NewGuid();
        await using var db = _database.CreateContext();
        await db.Database.ExecuteSqlRawAsync(
            "INSERT INTO MediaItems (Id, LibraryId, DisplayName, CreatedAtUtc) VALUES ({0}, {1}, {2}, {3})",
            itemId, libraryId, "item", DateTime.UtcNow);
        return itemId;
    }

    private async Task<Guid> CreateLibraryAsync()
    {
        var libraryId = Guid.NewGuid();
        await using var db = _database.CreateContext();
        await db.Database.ExecuteSqlRawAsync(
            "INSERT INTO Libraries (Id, Name, CreatedAtUtc) VALUES ({0}, {1}, {2})",
            libraryId, "lib", DateTime.UtcNow);
        return libraryId;
    }

    /// <summary>Indexes one fake file for a freshly created core item (like the scan does).</summary>
    private async Task<bool> IndexAsync(Guid libraryId, string path, AudioTags? tags)
    {
        if (tags is not null)
        {
            _tags.TagsByPath[path] = tags;
        }

        var itemId = await CreateCoreItemAsync(libraryId);
        return await _service.IndexFileAsync(libraryId, itemId, Guid.NewGuid(), path);
    }

    [Fact]
    public async Task Indexes_track_album_and_artist_from_tags()
    {
        var libraryId = await CreateLibraryAsync();
        var indexed = await IndexAsync(
            libraryId,
            PathFor("a1"),
            Tags(title: "Night Drive", artist: "Kavinsky", album: "OutRun", year: 2013));

        Assert.True(indexed);
        await using var db = _database.CreateContext();
        var track = await db.Tracks.Include(t => t.Album).Include(t => t.Artist).SingleAsync();
        Assert.Equal("Night Drive", track.Title);
        Assert.Equal("OutRun", track.Album!.Title);
        Assert.Equal("Kavinsky", track.Artist!.Name);
        Assert.Equal(2013, track.Album.Year);
        Assert.Equal(180.5, track.DurationSeconds);
        Assert.Equal("Rock", track.Genre);
        Assert.Single(db.Albums);
        Assert.Single(db.Artists);
    }

    [Fact]
    public async Task Falls_back_to_file_name_when_title_tag_is_missing()
    {
        var libraryId = await CreateLibraryAsync();
        await IndexAsync(libraryId, PathFor("notitle"), Tags(title: null));

        await using var db = _database.CreateContext();
        Assert.Equal("onidash-music-notitle", (await db.Tracks.SingleAsync()).Title);
    }

    [Fact]
    public async Task Two_files_of_same_artist_and_album_group_into_one_album()
    {
        var libraryId = await CreateLibraryAsync();
        await IndexAsync(libraryId, PathFor("t1"), Tags(title: "One", artist: "Kavinsky", album: "OutRun", trackNumber: 1));
        await IndexAsync(libraryId, PathFor("t2"), Tags(title: "Two", artist: "Kavinsky", album: "OutRun", trackNumber: 2));

        await using var db = _database.CreateContext();
        Assert.Equal(2, await db.Tracks.CountAsync());
        Assert.Single(await db.Albums.ToListAsync());
        Assert.Single(await db.Artists.ToListAsync());
        Assert.Equal([1, 2], await db.Tracks.OrderBy(t => t.TrackNumber).Select(t => t.TrackNumber).ToListAsync());
    }

    [Fact]
    public async Task Artist_names_merge_case_insensitively()
    {
        var libraryId = await CreateLibraryAsync();
        await IndexAsync(libraryId, PathFor("m1"), Tags(artist: "Daft Punk"));
        await IndexAsync(libraryId, PathFor("m2"), Tags(artist: "daft punk"));

        await using var db = _database.CreateContext();
        Assert.Single(await db.Artists.ToListAsync());
    }

    [Fact]
    public async Task Album_artist_wins_for_album_grouping()
    {
        var libraryId = await CreateLibraryAsync();
        // Different track artists, same album artist+album => one album.
        await IndexAsync(libraryId, PathFor("va1"), Tags(title: "A", artist: "Singer One", albumArtist: "Various", album: "Compilation"));
        await IndexAsync(libraryId, PathFor("va2"), Tags(title: "B", artist: "Singer Two", albumArtist: "Various", album: "Compilation"));

        await using var db = _database.CreateContext();
        Assert.Single(await db.Albums.ToListAsync());
        Assert.Equal("Various", (await db.Albums.SingleAsync()).ArtistName);
        Assert.Equal(3, await db.Artists.CountAsync()); // 2 track artists + album artist
    }

    [Fact]
    public async Task Missing_album_tag_leaves_track_albumless()
    {
        var libraryId = await CreateLibraryAsync();
        await IndexAsync(libraryId, PathFor("noalbum"), Tags(title: "Loose Track", album: null));

        await using var db = _database.CreateContext();
        Assert.Empty(await db.Albums.ToListAsync());
        Assert.Null((await db.Tracks.SingleAsync()).AlbumId);
    }

    [Fact]
    public async Task Cover_blob_is_captured_once_per_album()
    {
        var libraryId = await CreateLibraryAsync();
        var cover1 = new byte[] { 1, 2, 3 };
        var cover2 = new byte[] { 9, 9, 9 };
        await IndexAsync(libraryId, PathFor("c1"), Tags(title: "One", album: "Cov", cover: cover1));
        await IndexAsync(libraryId, PathFor("c2"), Tags(title: "Two", album: "Cov", cover: cover2));

        await using var db = _database.CreateContext();
        var album = await db.Albums.SingleAsync();
        Assert.Equal(cover1, album.CoverBlob);
    }

    [Fact]
    public async Task Same_album_in_two_libraries_stays_separate()
    {
        var library1 = await CreateLibraryAsync();
        var library2 = await CreateLibraryAsync();
        await IndexAsync(library1, PathFor("l1"), Tags(title: "X", album: "Shared"));
        await IndexAsync(library2, PathFor("l2"), Tags(title: "X", album: "Shared"));

        await using var db = _database.CreateContext();
        Assert.Equal(2, await db.Albums.CountAsync());
    }

    [Fact]
    public async Task Reindex_updates_the_same_track_row()
    {
        var path = PathFor("re");
        _tags.TagsByPath[path] = Tags(title: "Before");
        await using var db = _database.CreateContext();
        var libraryId = await CreateLibraryAsync();
        var mediaItemId = Guid.NewGuid();
        await db.Database.ExecuteSqlRawAsync(
            "INSERT INTO MediaItems (Id, LibraryId, DisplayName, CreatedAtUtc) VALUES ({0}, {1}, {2}, {3})",
            mediaItemId, libraryId, "item", DateTime.UtcNow);

        await _service.IndexFileAsync(libraryId, mediaItemId, Guid.NewGuid(), path);

        _tags.TagsByPath[path] = Tags(title: "After", trackNumber: 7);
        var sameFile = await _service.IndexFileAsync(libraryId, mediaItemId, Guid.NewGuid(), path);

        Assert.True(sameFile);
        var track = await db.Tracks.SingleAsync(t => t.MediaItemId == mediaItemId);
        Assert.Equal("After", track.Title);
        Assert.Equal(7, track.TrackNumber);
        Assert.Equal(1, await db.Tracks.CountAsync());
    }

    [Fact]
    public async Task Unreadable_file_returns_false_and_writes_nothing()
    {
        var libraryId = await CreateLibraryAsync();
        // tags: null keeps the path out of the fake => reader returns null.
        var indexed = await IndexAsync(libraryId, PathFor("bad"), tags: null);

        Assert.False(indexed);
        await using var db = _database.CreateContext();
        Assert.Empty(await db.Tracks.ToListAsync());
    }

    [Fact]
    public void Audio_extensions_match_case_insensitively()
    {
        Assert.True(MusicCatalogService.AudioExtensionsMatch(".MP3"));
        Assert.True(MusicCatalogService.AudioExtensionsMatch("flac"));
        Assert.False(MusicCatalogService.AudioExtensionsMatch(".txt"));
        Assert.False(MusicCatalogService.AudioExtensionsMatch(".jpg"));
    }

    [Fact]
    public async Task ReindexService_catalogues_only_missing_or_stale_tracks()
    {
        await using var db = _database.CreateContext();
        var libraryId = Guid.NewGuid();
        var itemId1 = Guid.NewGuid();
        var itemId2 = Guid.NewGuid();
        var fileId1 = Guid.NewGuid();
        var fileId2 = Guid.NewGuid();
        await db.Database.ExecuteSqlRawAsync(
            "INSERT INTO MediaItems (Id, LibraryId, DisplayName, CreatedAtUtc) VALUES ({0}, {1}, {2}, {3})",
            itemId1, libraryId, "one", DateTime.UtcNow);
        await db.Database.ExecuteSqlRawAsync(
            "INSERT INTO MediaItems (Id, LibraryId, DisplayName, CreatedAtUtc) VALUES ({0}, {1}, {2}, {3})",
            itemId2, libraryId, "two", DateTime.UtcNow);
        await InsertFileAsync(db, fileId1, itemId1, libraryId, "one.mp3", DateTime.UtcNow.AddDays(-1));
        await InsertFileAsync(db, fileId2, itemId2, libraryId, "two.mp3", DateTime.UtcNow.AddDays(-1));

        // Reindex resolves candidate paths from the source root (C:\Media), not temp.
        _tags.TagsByPath[PathFor("one")] = Tags(title: "One", album: null);
        _tags.TagsByPath["C:\\Media\\two.mp3"] = Tags(title: "Two", album: null);

        // Track 1 becomes fresh (UpdatedAtUtc newer than its file); track 2 stays uncatalogued.
        await _service.IndexFileAsync(libraryId, itemId1, fileId1, PathFor("one"));

        var reindex = new MusicReindexService(db, _service, NullLogger<MusicReindexService>.Instance);
        var indexed = await reindex.ReindexAsync(libraryId);

        Assert.Equal(1, indexed); // only the stale one
        Assert.Equal(2, await db.Tracks.CountAsync());
    }

    private static async Task InsertFileAsync(
        MusicDbContext db, Guid fileId, Guid itemId, Guid libraryId, string relativePath, DateTimeOffset written)
    {
        var sourceId = Guid.NewGuid();
        await db.Database.ExecuteSqlRawAsync(
            "INSERT INTO Sources (Id, LibraryId, Name, RootPath, CreatedAtUtc) VALUES ({0}, {1}, {2}, {3}, {4})",
            sourceId, libraryId, "Src", @"C:\Media", DateTime.UtcNow);
        await db.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO Files (Id, MediaItemId, LibrarySourceId, RelativePath, IdentityKey, Extension, SizeBytes, LastWriteTimeUtc, CreatedAtUtc)
            VALUES ({0}, {1}, {2}, {3}, {4}, '.mp3', 1, {5}, {5})
            """,
            fileId, itemId, sourceId, relativePath, $"{sourceId:N}/{relativePath.ToLowerInvariant()}", written);
    }
}
