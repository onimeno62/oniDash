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

public sealed class SearchEndpointsTests : IClassFixture<SearchEndpointsTests.SearchFactory>
{
    public sealed class SearchFactory : WebApplicationFactory<Program>
    {
        private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"onidash-search-{Guid.NewGuid():N}.db");
        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureHostConfiguration(config => config.AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:OniDash"] = $"Data Source={_databasePath};Foreign Keys=True" }));
            return base.CreateHost(builder);
        }
        public string DatabasePath => _databasePath;
    }
    private readonly SearchFactory _factory;
    public SearchEndpointsTests(SearchFactory factory) => _factory = factory;
    [Fact]
    public async Task Offset_skips_ranked_results()
    {
        var client = _factory.CreateClient();
        await SeedAsync("Offset Search", ["Echo One", "Echo Two", "Echo Three"]);
        var first = await ReadJson(await client.GetAsync("/api/search?q=echo&limit=1&offset=0"));
        var second = await ReadJson(await client.GetAsync("/api/search?q=echo&limit=1&offset=1"));
        Assert.Equal(1, first.GetArrayLength()); Assert.Equal(1, second.GetArrayLength()); Assert.NotEqual(first[0].GetProperty("displayName").GetString(), second[0].GetProperty("displayName").GetString());
    }
    private OniDashDbContext CreateContext() => new(new DbContextOptionsBuilder<OniDashDbContext>().UseSqlite($"Data Source={_factory.DatabasePath};Foreign Keys=True").Options);
    private async Task SeedAsync(string libraryName, string[] displayNames)
    {
        await using var context = CreateContext(); var library = new Library { Name = $"{libraryName} {Guid.NewGuid():N}" }; context.Libraries.Add(library); context.MediaItems.AddRange(displayNames.Select(name => new MediaItem { LibraryId = library.Id, DisplayName = name })); await context.SaveChangesAsync();
    }
    private static async Task<JsonElement> ReadJson(HttpResponseMessage response) => JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();
}
