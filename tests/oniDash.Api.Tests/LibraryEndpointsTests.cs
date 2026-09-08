using System;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using oniDash.Core.Domain;
using oniDash.Infrastructure.Persistence;
using Xunit;

namespace oniDash.Api.Tests;

/// <summary>
/// End-to-end tests for the core library HTTP surface: routing, JSON contract, error
/// semantics (problem+json), and persistence against real SQLite.
/// </summary>
public sealed class LibraryEndpointsTests(OniDashApiFactory factory) : IClassFixture<OniDashApiFactory>
{
    [Fact]
    public async Task Create_library_returns_201_with_camel_case_body_and_location()
    {
        var client = factory.CreateClient();
        var name = Unique("M02 Library");

        var response = await client.PostAsJsonAsync("/api/libraries", new { name });
        var body = await ReadJson(response);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(name, body.GetProperty("name").GetString());
        Assert.True(body.TryGetProperty("createdAtUtc", out _));
        Assert.Equal(
            $"/api/libraries/{IdOf(body)}",
            response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Create_library_with_duplicate_name_returns_409_problem()
    {
        var client = factory.CreateClient();
        var name = Unique("Dup Library");
        await client.PostAsJsonAsync("/api/libraries", new { name });

        var response = await client.PostAsJsonAsync("/api/libraries", new { name = name.ToUpperInvariant() });
        var problem = await ReadJson(response);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("Conflict", problem.GetProperty("title").GetString());
        Assert.Contains("already exists", problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Create_library_with_blank_name_returns_400_validation_problem()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/libraries", new { name = "   " });
        var problem = await ReadJson(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(problem.TryGetProperty("errors", out _));
    }

    [Fact]
    public async Task Rename_and_list_round_trip()
    {
        var client = factory.CreateClient();
        var name = Unique("Rename Me");
        var created = await PostLibrary(client, name);

        var renamed = await client.PutAsJsonAsync($"/api/libraries/{IdOf(created)}", new { name = "Renamed" });
        var list = await client.GetFromJsonAsync<JsonElement>("/api/libraries");

        Assert.Equal(HttpStatusCode.OK, renamed.StatusCode);
        Assert.Equal("Renamed", (await ReadJson(renamed)).GetProperty("name").GetString());
        Assert.Contains(
            list.EnumerateArray(),
            element => element.GetProperty("id").GetString() == IdOf(created));
    }

    [Fact]
    public async Task Delete_returns_204_and_subsequent_get_returns_404()
    {
        var client = factory.CreateClient();
        var created = await PostLibrary(client, Unique("Delete Me"));

        var deleted = await client.DeleteAsync($"/api/libraries/{IdOf(created)}");
        var after = await client.GetAsync($"/api/libraries/{IdOf(created)}");
        var problem = await ReadJson(after);

        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, after.StatusCode);
        Assert.Equal("Not found", problem.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Add_source_rejects_nonexistent_folder_with_400()
    {
        var client = factory.CreateClient();
        var library = await PostLibrary(client, Unique("Sources"));

        var response = await client.PostAsJsonAsync(
            $"/api/libraries/{IdOf(library)}/sources",
            new { name = "Main", rootPath = @"C:\definitely\not\here" });
        var problem = await ReadJson(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("does not exist", problem.GetProperty("errors").GetProperty("request")[0].GetString());
    }

    [Fact]
    public async Task Add_and_remove_source_end_to_end()
    {
        var client = factory.CreateClient();
        var library = await PostLibrary(client, Unique("Sources Ok"));
        var tempDir = CreateTempDirectory();
        try
        {
            var created = await client.PostAsJsonAsync(
                $"/api/libraries/{IdOf(library)}/sources",
                new { name = "Main", rootPath = tempDir });
            var body = await ReadJson(created);

            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            Assert.Equal(tempDir, body.GetProperty("rootPath").GetString());

            var duplicate = await client.PostAsJsonAsync(
                $"/api/libraries/{IdOf(library)}/sources",
                new { name = "Again", rootPath = tempDir.ToUpperInvariant() });
            Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

            var list = await client.GetFromJsonAsync<JsonElement>($"/api/libraries/{IdOf(library)}/sources");
            Assert.Single(list.EnumerateArray());

            var removed = await client.DeleteAsync($"/api/libraries/{IdOf(library)}/sources/{IdOf(body)}");
            Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);
        }
        finally
        {
            TryDeleteDirectory(tempDir);
        }
    }

    [Fact]
    public async Task List_items_returns_empty_paged_result_for_fresh_library()
    {
        var client = factory.CreateClient();
        var library = await PostLibrary(client, Unique("Items"));

        var response = await client.GetFromJsonAsync<JsonElement>($"/api/libraries/{IdOf(library)}/items");

        Assert.Equal(0, response.GetProperty("totalCount").GetInt32());
        Assert.Equal(1, response.GetProperty("page").GetInt32());
        Assert.Equal(50, response.GetProperty("pageSize").GetInt32());
        Assert.Empty(response.GetProperty("items").EnumerateArray());
    }

    [Fact]
    public async Task Tag_create_and_assign_to_seeded_item_round_trip()
    {
        var client = factory.CreateClient();
        var library = await PostLibrary(client, Unique("Tags"));
        var itemId = await SeedMediaItemAsync(factory, Guid.Parse(IdOf(library)), "Seeded Album");

        var tagResponse = await client.PostAsJsonAsync(
            $"/api/libraries/{IdOf(library)}/tags",
            new { name = "favorite" });
        var tag = await ReadJson(tagResponse);
        Assert.Equal(HttpStatusCode.Created, tagResponse.StatusCode);

        var assigned = await client.PostAsJsonAsync(
            $"/api/media-items/{itemId}/tags",
            new { tagId = IdOf(tag) });
        Assert.Equal(HttpStatusCode.NoContent, assigned.StatusCode);

        var duplicate = await client.PostAsJsonAsync(
            $"/api/media-items/{itemId}/tags",
            new { tagId = IdOf(tag) });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        var unassigned = await client.DeleteAsync($"/api/media-items/{itemId}/tags/{IdOf(tag)}");
        Assert.Equal(HttpStatusCode.NoContent, unassigned.StatusCode);

        var deleted = await client.DeleteAsync($"/api/tags/{IdOf(tag)}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
    }

    [Fact]
    public async Task Collection_create_add_remove_item_round_trip()
    {
        var client = factory.CreateClient();
        var library = await PostLibrary(client, Unique("Collections"));
        var itemId = await SeedMediaItemAsync(factory, Guid.Parse(IdOf(library)), "Seeded Album 2");

        var collectionResponse = await client.PostAsJsonAsync(
            $"/api/libraries/{IdOf(library)}/collections",
            new { name = "Road trip" });
        var collection = await ReadJson(collectionResponse);
        Assert.Equal(HttpStatusCode.Created, collectionResponse.StatusCode);

        var added = await client.PostAsJsonAsync(
            $"/api/collections/{IdOf(collection)}/items",
            new { mediaItemId = itemId });
        Assert.Equal(HttpStatusCode.NoContent, added.StatusCode);

        var items = await client.GetFromJsonAsync<JsonElement>(
            $"/api/collections/{IdOf(collection)}/items");
        Assert.Equal(1, items.GetProperty("totalCount").GetInt32());

        var removed = await client.DeleteAsync(
            $"/api/collections/{IdOf(collection)}/items/{itemId}");
        Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);
    }

    [Fact]
    public async Task Unknown_library_returns_404_for_nested_routes()
    {
        var client = factory.CreateClient();
        var unknownId = Guid.NewGuid();

        var tags = await client.GetAsync($"/api/libraries/{unknownId}/tags");
        var collections = await client.GetAsync($"/api/libraries/{unknownId}/collections");
        var items = await client.GetAsync($"/api/libraries/{unknownId}/items");

        Assert.Equal(HttpStatusCode.NotFound, tags.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, collections.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, items.StatusCode);
    }

    private static string IdOf(JsonElement element) =>
        element.GetProperty("id").GetString()
        ?? throw new InvalidOperationException("expected a non-null id");

    private static async Task<JsonElement> PostLibrary(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/api/libraries", new { name });
        response.EnsureSuccessStatusCode();
        return await ReadJson(response);
    }

    private static async Task<JsonElement> ReadJson(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(content).RootElement.Clone();
    }

    private static string Unique(string prefix) => $"{prefix} {Guid.NewGuid():N}".Trim();

    private static string CreateTempDirectory()
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"onidash-src-{Guid.NewGuid():N}");
        System.IO.Directory.CreateDirectory(path);
        return path;
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            System.IO.Directory.Delete(path, recursive: true);
        }
        catch (System.IO.IOException)
        {
        }
    }

    /// <summary>
    /// Phase 3 introduces the scanner; until then, tests seed media items directly through
    /// the same DbContext the API uses, against the factory's isolated database file.
    /// </summary>
    private static async Task<Guid> SeedMediaItemAsync(OniDashApiFactory factory, Guid libraryId, string displayName)
    {
        var options = new DbContextOptionsBuilder<OniDashDbContext>()
            .UseSqlite($"Data Source={factory.DatabasePath}")
            .Options;
        await using var context = new OniDashDbContext(options);

        var library = await context.Libraries.FindAsync(libraryId);
        Assert.NotNull(library);

        var item = new MediaItem { LibraryId = libraryId, DisplayName = displayName };
        context.MediaItems.Add(item);
        await context.SaveChangesAsync();
        return item.Id;
    }
}
