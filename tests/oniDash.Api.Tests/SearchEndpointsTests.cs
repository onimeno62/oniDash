using System;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using oniDash.Core.Domain;
using oniDash.Infrastructure.Persistence;
using Xunit;

namespace oniDash.Api.Tests;

/// <summary>
/// End-to-end tests for the global search HTTP surface: FTS results, filters, ranking,
/// and the reindex command — over a real indexed SQLite database.
/// </summary>
public sealed class SearchEndpointsTests : IClassFixture<SearchEndpointsTests.SearchFactory>
{
    public sealed class SearchFactory : WebApplicationFactory<Program>
    {
        private readonly string _databasePath = Path.Combine(
            Path.GetTempPath(),
            $"onidash-search-{Guid.NewGuid():N}.db");

        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureHostConfiguration(config => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:OniDash"] = $"Data Source={_databasePath};Foreign Keys=True",
            }));
            return base.CreateHost(builder);
        }

        public string DatabasePath => _databasePath;
    }

    private readonly SearchFactory _factory;

    public SearchEndpointsTests(SearchFactory factory) => _factory = factory;

    [Fact]
    public async Task Search_returns_ranked_results_with_library_names()
    {
        var client = _factory.CreateClient();
        var libraryId = await SeedAsync("Night Music", ["Night Drive", "Drive All Night", "Sunrise"]);

        var response = await client.GetAsync("/api/search?q=night");
        response.EnsureSuccessStatusCode();
        var results = await ReadJson(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, results.GetArrayLength());
        Assert.All(results.EnumerateArray(), r =>
        {
            Assert.NotEqual(Guid.Empty.ToString(), r.GetProperty("itemId").GetString());
            // Seed appends a uniqueness suffix to library names.
            Assert.StartsWith("Night Music", r.GetProperty("libraryName").GetString());
        });
        Assert.Equal(
            libraryId.ToString(),
            results[0].GetProperty("libraryId").GetString());
    }

    [Fact]
    public async Task Blank_query_returns_empty_array()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/search?q=");
        response.EnsureSuccessStatusCode();
        var results = await ReadJson(response);

        Assert.Equal(0, results.GetArrayLength());
    }

    [Fact]
    public async Task Library_filter_constrains_results_via_query_string()
    {
        var client = _factory.CreateClient();
        var musicId = await SeedAsync("Filter Music", ["Alpha Track", "Alpha Theme"]);
        var moviesId = await SeedAsync("Filter Movies", ["Alpha Cut"]);

        var response = await client.GetAsync($"/api/search?q=alpha&libraryId={moviesId}");
        response.EnsureSuccessStatusCode();
        var results = await ReadJson(response);

        Assert.Equal(1, results.GetArrayLength());
        Assert.Equal(moviesId.ToString(), results[0].GetProperty("libraryId").GetString());
        Assert.NotEqual(musicId.ToString(), results[0].GetProperty("libraryId").GetString());
    }

    [Fact]
    public async Task Limit_is_respected()
    {
        var client = _factory.CreateClient();
        await SeedAsync("Limit Music", ["Echo One", "Echo Two", "Echo Three"]);

        var results = await ReadJson(await client.GetAsync("/api/search?q=echo&limit=2"));

        Assert.Equal(2, results.GetArrayLength());
    }

    [Fact]
    public async Task Results_follow_the_live_index_without_manual_reindex()
    {
        var client = _factory.CreateClient();
        await SeedAsync("Live Index", ["Phantom Thread"]);

        var before = await ReadJson(await client.GetAsync("/api/search?q=phantom"));
        Assert.Equal(1, before.GetArrayLength());

        // Rename an item directly; the FTS triggers must reflect it immediately.
        await using (var context = CreateContext())
        {
            var item = await context.MediaItems.SingleAsync(i => i.DisplayName == "Phantom Thread");
            item.DisplayName = "Phantom Opera";
            await context.SaveChangesAsync();
        }

        var afterRename = await ReadJson(await client.GetAsync("/api/search?q=phantom"));
        Assert.Equal("Phantom Opera", afterRename[0].GetProperty("displayName").GetString());
        Assert.Equal(0, (await ReadJson(await client.GetAsync("/api/search?q=thread"))).GetArrayLength());
    }

    [Fact]
    public async Task Reindex_reports_indexed_item_count()
    {
        var client = _factory.CreateClient();
        await SeedAsync("Reindex Music", ["Cherry Blossom", "Cherry Wine"]);

        var response = await client.PostAsync("/api/search/reindex", null);
        response.EnsureSuccessStatusCode();
        var body = await ReadJson(response);

        Assert.True(body.GetProperty("indexedItems").GetInt32() >= 2);
    }

    // ---- helpers -----------------------------------------------------------

    private OniDashDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<OniDashDbContext>()
            .UseSqlite($"Data Source={_factory.DatabasePath};Foreign Keys=True")
            .Options;
        return new OniDashDbContext(options);
    }

    private async Task<Guid> SeedAsync(string libraryName, string[] displayNames)
    {
        await using var context = CreateContext();
        var library = new Library { Name = $"{libraryName} {Guid.NewGuid():N}" };
        context.Libraries.Add(library);
        context.MediaItems.AddRange(
            displayNames.Select(name => new MediaItem { LibraryId = library.Id, DisplayName = name }));
        await context.SaveChangesAsync();
        return library.Id;
    }

    private static async Task<JsonElement> ReadJson(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(content).RootElement.Clone();
    }
}
