using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using oniDash.Movies.Cataloging;
using oniDash.Movies.Probing;
using Xunit;

namespace oniDash.Movies.Tests;

public sealed class MovieCatalogServiceTests
{
    private readonly MovieTestDatabase _database = new();
    private readonly FakeVideoProbeReader _probes = new();
    private readonly FakeVideoArtworkReader _artwork = new();
    private readonly MovieCatalogService _service;

    public MovieCatalogServiceTests()
    {
        _service = new MovieCatalogService(
            _database.CreateContext(),
            _probes,
            _artwork,
            NullLogger<MovieCatalogService>.Instance);
    }

    private static string PathFor(string name) =>
        Path.Combine(Path.GetTempPath(), $"onidash-movie-{name}.mkv");

    private async Task<Guid> CreateLibraryAsync()
    {
        var libraryId = Guid.NewGuid();
        await using var db = _database.CreateContext();
        await db.Database.ExecuteSqlRawAsync(
            "INSERT INTO Libraries (Id, Name, CreatedAtUtc) VALUES ({0}, {1}, {2})",
            libraryId, "lib", DateTime.UtcNow);
        return libraryId;
    }

    private async Task<Guid> CreateCoreItemAsync(Guid libraryId)
    {
        var itemId = Guid.NewGuid();
        await using var db = _database.CreateContext();
        await db.Database.ExecuteSqlRawAsync(
            "INSERT INTO MediaItems (Id, LibraryId, DisplayName, CreatedAtUtc) VALUES ({0}, {1}, {2}, {3})",
            itemId, libraryId, "item", DateTime.UtcNow);
        return itemId;
    }

    /// <summary>Indexes one fake video for a freshly created core item (like the scan does).</summary>
    private async Task<bool> IndexAsync(Guid libraryId, string path)
    {
        var itemId = await CreateCoreItemAsync(libraryId);
        return await _service.IndexFileAsync(libraryId, itemId, Guid.NewGuid(), path);
    }

    [Fact]
    public async Task Indexes_movie_from_filename_and_probe()
    {
        var path = Path.Combine(Path.GetTempPath(), "The Matrix (1999).mkv");
        _probes.ProbesByPath[path] = new VideoProbe(8166.0, 1920, 800);

        var libraryId = await CreateLibraryAsync();
        var indexed = await IndexAsync(libraryId, path);

        Assert.True(indexed);
        await using var db = _database.CreateContext();
        var movie = await db.Movies.SingleAsync();
        Assert.Equal("The Matrix", movie.Title);
        Assert.Equal(1999, movie.Year);
        Assert.Equal(8166.0, movie.DurationSeconds);
        Assert.Equal(".mkv", movie.Container);
    }

    [Fact]
    public async Task Non_video_extensions_are_ignored()
    {
        var libraryId = await CreateLibraryAsync();
        var indexed = await IndexAsync(libraryId, Path.Combine(Path.GetTempPath(), "onidash-movie-notes.txt"));

        Assert.False(indexed);
        await using var db = _database.CreateContext();
        Assert.Empty(await db.Movies.ToListAsync());
    }

    [Fact]
    public async Task Rescan_updates_the_same_movie_row()
    {
        var path = PathFor("re");
        var libraryId = await CreateLibraryAsync();
        var itemId = await CreateCoreItemAsync(libraryId);

        _probes.ProbesByPath[path] = new VideoProbe(100, 640, 360);
        await _service.IndexFileAsync(libraryId, itemId, Guid.NewGuid(), path);

        _probes.ProbesByPath[path] = new VideoProbe(120, 1920, 1080);
        var again = await _service.IndexFileAsync(libraryId, itemId, Guid.NewGuid(), path);

        Assert.True(again);
        await using var db = _database.CreateContext();
        Assert.Equal(120, (await db.Movies.SingleAsync(m => m.MediaItemId == itemId)).DurationSeconds);
        Assert.Equal(1, await db.Movies.CountAsync());
    }

    [Fact]
    public async Task Unprobeable_file_still_indexes_from_the_filename()
    {
        var libraryId = await CreateLibraryAsync();
        var indexed = await IndexAsync(libraryId, "Edge of Tomorrow 2014 1080p WEB-DL x264.mkv");

        await using var db = _database.CreateContext();
        var movie = await db.Movies.SingleAsync();
        Assert.True(indexed);
        Assert.Equal("Edge of Tomorrow", movie.Title);
        Assert.Equal(2014, movie.Year);
        Assert.Null(movie.DurationSeconds);
    }

    [Fact]
    public async Task Missing_probe_path_indexes_from_the_filename_only()
    {
        var libraryId = await CreateLibraryAsync();
        // No probe registered for this path at all.
        var indexed = await IndexAsync(libraryId, "contact.mkv");

        await using var db = _database.CreateContext();
        var movie = await db.Movies.SingleAsync();
        Assert.True(indexed);
        Assert.Equal("contact", movie.Title);
        Assert.Null(movie.Year);
    }

    [Fact]
    public async Task Poster_is_captured_once_and_not_overwritten()
    {
        var path = PathFor("poster");
        var libraryId = await CreateLibraryAsync();
        var itemId = await CreateCoreItemAsync(libraryId);

        _artwork.ArtworkByPath[path] = new VideoArtwork([1, 2, 3], "image/png");
        await _service.IndexFileAsync(libraryId, itemId, Guid.NewGuid(), path);

        _artwork.ArtworkByPath[path] = new VideoArtwork([9, 9, 9], "image/png");
        await _service.IndexFileAsync(libraryId, itemId, Guid.NewGuid(), path);

        await using var db = _database.CreateContext();
        var movie = await db.Movies.SingleAsync();
        Assert.Equal([1, 2, 3], movie.PosterBlob);
    }

    [Fact]
    public async Task Watch_state_survives_rescans_and_can_be_cleared()
    {
        var path = PathFor("watch");
        var libraryId = await CreateLibraryAsync();
        var itemId = await CreateCoreItemAsync(libraryId);

        await _service.IndexFileAsync(libraryId, itemId, Guid.NewGuid(), path);

        Guid movieId;
        await using (var db = _database.CreateContext())
        {
            movieId = (await db.Movies.SingleAsync()).Id;
        }

        Assert.True(await _service.SaveProgressAsync(movieId, 300.5));
        Assert.True(await _service.SetWatchedAsync(movieId, watched: true));

        // A later reindex (file touched again) must not lose watch state.
        await _service.IndexFileAsync(libraryId, itemId, Guid.NewGuid(), path);

        await using (var db = _database.CreateContext())
        {
            var movie = await db.Movies.SingleAsync();
            Assert.True(movie.Watched);
            Assert.NotNull(movie.WatchedAtUtc);
            Assert.Null(movie.WatchProgressSeconds); // cleared by SetWatchedAsync
        }

        // Un-watching clears the watch date and resume position.
        Assert.True(await _service.SetWatchedAsync(movieId, watched: false));
        await using (var db = _database.CreateContext())
        {
            var movie = await db.Movies.SingleAsync();
            Assert.False(movie.Watched);
            Assert.Null(movie.WatchedAtUtc);
            Assert.Null(movie.WatchProgressSeconds);
        }
    }

    [Fact]
    public async Task SaveProgress_rejects_unknown_movies()
    {
        Assert.False(await _service.SaveProgressAsync(Guid.NewGuid(), 42));
    }

    [Fact]
    public void Video_extensions_match_case_insensitively()
    {
        Assert.True(MovieCatalogService.VideoExtensionsMatch(".MP4"));
        Assert.True(MovieCatalogService.VideoExtensionsMatch("mkv"));
        Assert.False(MovieCatalogService.VideoExtensionsMatch(".mp3"));
    }
}
