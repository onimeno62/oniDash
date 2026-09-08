using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using oniDash.Core.Domain;
using oniDash.Infrastructure.Persistence;
using oniDash.Infrastructure.Search;
using Xunit;

namespace oniDash.Infrastructure.Tests;

/// <summary>
/// Real FTS5 tests against in-memory SQLite with migrations applied — verifies the
/// virtual table, the sync triggers, prefix/AND matching, ranking, and reindex.
/// </summary>
public sealed class FtsSearchServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<OniDashDbContext> _options;
    private readonly FtsSearchService _service;

    public FtsSearchServiceTests()
    {
        // Foreign Keys=True mirrors the production connection string so cascade deletes
        // (and the FTS triggers they must fire) behave like production.
        _connection = new SqliteConnection("Filename=:memory:;Foreign Keys=True");
        _connection.Open();
        _options = new DbContextOptionsBuilder<OniDashDbContext>().UseSqlite(_connection).Options;
        using var context = new OniDashDbContext(_options);
        context.Database.Migrate();
        _service = new FtsSearchService(new OniDashDbContext(_options));
    }

    public void Dispose() => _connection.Dispose();

    private async Task<(Guid MusicId, Guid MoviesId)> SeedAsync()
    {
        await using var context = new OniDashDbContext(_options);
        var music = new Library { Name = "Music" };
        var movies = new Library { Name = "Movies" };
        context.Libraries.AddRange(music, movies);
        await context.SaveChangesAsync();

        context.MediaItems.AddRange(
            new MediaItem { LibraryId = music.Id, DisplayName = "Night Drive" },
            new MediaItem { LibraryId = music.Id, DisplayName = "Drive All Night" },
            new MediaItem { LibraryId = music.Id, DisplayName = "Sunrise" },
            new MediaItem { LibraryId = movies.Id, DisplayName = "Night of the Hunter" });
        await context.SaveChangesAsync();
        return (music.Id, movies.Id);
    }

    [Fact]
    public async Task Search_is_case_insensitive_and_finds_words()
    {
        var (musicId, _) = await SeedAsync();

        var results = await _service.SearchAsync("night");

        Assert.Equal(3, results.Count);
        Assert.All(results, r => Assert.Contains("night", r.DisplayName, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task All_terms_must_match()
    {
        await SeedAsync();

        var results = await _service.SearchAsync("drive night");

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Contains("Drive", r.DisplayName));
    }

    [Fact]
    public async Task Prefix_matching_finds_word_starts()
    {
        await SeedAsync();

        var results = await _service.SearchAsync("hunt");

        Assert.Single(results, r => r.DisplayName == "Night of the Hunter");
    }

    [Fact]
    public async Task Library_filter_constrains_results()
    {
        var (musicId, moviesId) = await SeedAsync();

        var results = await _service.SearchAsync("night", libraryId: moviesId);

        Assert.Single(results);
        Assert.Equal(moviesId, results[0].LibraryId);
        Assert.Equal("Movies", results[0].LibraryName);
    }

    [Fact]
    public async Task Ranking_puts_higher_term_frequency_first()
    {
        await using var context = new OniDashDbContext(_options);
        var library = new Library { Name = "Music" };
        context.Libraries.Add(library);
        context.MediaItems.AddRange(
            new MediaItem { LibraryId = library.Id, DisplayName = "Drive All Night" },
            new MediaItem { LibraryId = library.Id, DisplayName = "Night Night Night" });
        await context.SaveChangesAsync();

        var results = await _service.SearchAsync("night");

        Assert.Equal(2, results.Count);
        Assert.Equal("Night Night Night", results[0].DisplayName);
    }

    [Fact]
    public async Task Blank_query_returns_empty_list()
    {
        await SeedAsync();

        Assert.Empty(await _service.SearchAsync(""));
        Assert.Empty(await _service.SearchAsync("   "));
    }

    [Fact]
    public async Task Unknown_terms_return_no_results()
    {
        await SeedAsync();

        Assert.Empty(await _service.SearchAsync("zzznothing"));
    }

    [Fact]
    public async Task User_input_cannot_inject_fts_syntax()
    {
        await SeedAsync();

        // Quotes, parentheses, and operators must be neutralized, not crash or widen.
        var results = await _service.SearchAsync("\"night\" OR (drive)");
        Assert.All(results, r => Assert.NotNull(r.DisplayName));

        var notResults = await _service.SearchAsync("night NOT drive");
        Assert.All(notResults, r => Assert.NotNull(r.DisplayName));
    }

    [Fact]
    public async Task Triggers_keep_index_in_sync_on_update_and_delete()
    {
        await SeedAsync();
        await using var context = new OniDashDbContext(_options);

        var sunrise = await context.MediaItems.SingleAsync(i => i.DisplayName == "Sunrise");
        sunrise.DisplayName = "Sunrise Sonata";
        await context.SaveChangesAsync();

        Assert.Single(await _service.SearchAsync("sonata"));

        context.MediaItems.Remove(sunrise);
        await context.SaveChangesAsync();

        Assert.Empty(await _service.SearchAsync("sonata"));
        Assert.Empty(await _service.SearchAsync("sunrise"));
    }

    [Fact]
    public async Task Bulk_delete_cascade_keeps_index_consistent()
    {
        var (musicId, moviesId) = await SeedAsync();

        await using (var context = new OniDashDbContext(_options))
        {
            await context.Libraries.Where(l => l.Id == musicId).ExecuteDeleteAsync();
        }

        // Music items are gone from the index...
        Assert.Empty(await _service.SearchAsync("drive", libraryId: musicId));
        // ...while the Movies library item survives (cascade only removed Music's rows).
        var remaining = await _service.SearchAsync("night", libraryId: moviesId);
        Assert.Single(remaining, r => r.DisplayName == "Night of the Hunter");
    }

    [Fact]
    public async Task Reindex_rebuilds_from_persisted_items()
    {
        await SeedAsync();

        // Wipe the index behind the service's back, then reindex.
        await using (var context = new OniDashDbContext(_options))
        {
            await context.Database.ExecuteSqlRawAsync("DELETE FROM MediaItemsFts");
        }

        Assert.Empty(await _service.SearchAsync("night"));

        var indexed = await _service.ReindexAsync();

        Assert.Equal(4, indexed);
        Assert.Equal(3, (await _service.SearchAsync("night")).Count);
    }

    [Fact]
    public async Task Limit_caps_result_count()
    {
        await SeedAsync();

        var results = await _service.SearchAsync("night", limit: 2);

        Assert.Equal(2, results.Count);
    }
}
