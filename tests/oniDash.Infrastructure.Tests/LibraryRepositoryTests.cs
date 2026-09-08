using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using oniDash.Core.Domain;
using oniDash.Infrastructure.Persistence;
using oniDash.Infrastructure.Repositories;
using Xunit;

namespace oniDash.Infrastructure.Tests;

/// <summary>
/// Shared fixture: a real SQLite database (in-memory, kept alive by an open connection) with
/// the exact model configuration and migrations the production app uses.
/// </summary>
public sealed class RepoTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<OniDashDbContext> _options;

    public RepoTests()
    {
        // Foreign Keys=True mirrors the production connection string (cascade + integrity parity).
        _connection = new SqliteConnection("Filename=:memory:;Foreign Keys=True");
        _connection.Open();

        _options = new DbContextOptionsBuilder<OniDashDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = CreateContext();
        context.Database.Migrate();
    }

    public OniDashDbContext CreateContext() => new(_options);

    public async Task<Library> SeedLibraryAsync(string name = "Test Library")
    {
        await using var context = CreateContext();
        var library = new Library { Name = name };
        context.Libraries.Add(library);
        await context.SaveChangesAsync();
        return library;
    }

    public async Task<MediaItem> SeedItemAsync(Guid libraryId, string displayName = "Some Album")
    {
        await using var context = CreateContext();
        var item = new MediaItem { LibraryId = libraryId, DisplayName = displayName };
        context.MediaItems.Add(item);
        await context.SaveChangesAsync();
        return item;
    }

    public void Dispose() => _connection.Dispose();
}

public sealed class LibraryRepositoryTests(RepoTests fixture) : IClassFixture<RepoTests>
{
    [Fact]
    public async Task Add_then_list_round_trips_a_library()
    {
        var repository = new LibraryRepository(fixture.CreateContext());

        await repository.AddAsync(new Library { Name = "Music" });
        var all = await repository.ListAsync();

        Assert.Single(all, l => l.Name == "Music");
    }

    [Fact]
    public async Task ExistsByName_is_case_insensitive_and_respects_exclusion()
    {
        var repository = new LibraryRepository(fixture.CreateContext());
        var library = new Library { Name = "Movies" };
        await repository.AddAsync(library);

        Assert.True(await repository.ExistsByNameAsync("movies"));
        Assert.False(await repository.ExistsByNameAsync("MOVIES", excludeId: library.Id));
    }

    [Fact]
    public async Task Delete_cascades_to_sources_items_and_join_tables()
    {
        var library = await fixture.SeedLibraryAsync("Cascade");
        var item = await fixture.SeedItemAsync(library.Id);
        var sourceId = Guid.NewGuid();
        var tagId = Guid.NewGuid();
        var collectionId = Guid.NewGuid();

        await using (var context = fixture.CreateContext())
        {
            context.Sources.Add(new LibrarySource
            {
                Id = sourceId,
                LibraryId = library.Id,
                Name = "Src",
                RootPath = @"C:\media\cascade",
            });
            context.Tags.Add(new Tag { Id = tagId, LibraryId = library.Id, Name = "fav" });
            context.Collections.Add(new Collection { Id = collectionId, LibraryId = library.Id, Name = "Best" });
            context.MediaItemTags.Add(new MediaItemTag { MediaItemId = item.Id, TagId = tagId });
            context.CollectionItems.Add(new CollectionItem { CollectionId = collectionId, MediaItemId = item.Id });
            await context.SaveChangesAsync();
        }

        var repository = new LibraryRepository(fixture.CreateContext());
        var loaded = await repository.GetByIdAsync(library.Id);
        await repository.DeleteAsync(loaded!);

        await using var verify = fixture.CreateContext();
        Assert.Empty(verify.Sources.Where(s => s.Id == sourceId));
        Assert.Empty(verify.MediaItems.Where(i => i.Id == item.Id));
        Assert.Empty(verify.Tags.Where(t => t.Id == tagId));
        Assert.Empty(verify.Collections.Where(c => c.Id == collectionId));
        Assert.Empty(verify.MediaItemTags);
        Assert.Empty(verify.CollectionItems);
    }
}

public sealed class SourceRepositoryTests(RepoTests fixture) : IClassFixture<RepoTests>
{
    [Fact]
    public async Task Add_and_list_by_library_returns_only_that_librarys_sources()
    {
        var libraryA = await fixture.SeedLibraryAsync("A");
        var libraryB = await fixture.SeedLibraryAsync("B");
        var repository = new LibrarySourceRepository(fixture.CreateContext());

        await repository.AddAsync(new LibrarySource { LibraryId = libraryA.Id, Name = "A1", RootPath = @"C:\a1" });
        await repository.AddAsync(new LibrarySource { LibraryId = libraryB.Id, Name = "B1", RootPath = @"C:\b1" });

        var sources = await repository.ListByLibraryAsync(libraryA.Id);
        Assert.Single(sources, s => s.RootPath == @"C:\a1");
    }

    [Fact]
    public async Task Duplicate_root_path_in_same_library_is_rejected_by_unique_index()
    {
        var library = await fixture.SeedLibraryAsync("Dup");
        var repository = new LibrarySourceRepository(fixture.CreateContext());

        await repository.AddAsync(new LibrarySource { LibraryId = library.Id, Name = "One", RootPath = @"C:\same" });

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            repository.AddAsync(new LibrarySource { LibraryId = library.Id, Name = "Two", RootPath = @"C:\same" }));
    }
}

public sealed class MediaItemRepositoryTests(RepoTests fixture) : IClassFixture<RepoTests>
{
    [Fact]
    public async Task ListByLibrary_pages_with_stable_ordering_and_total_count()
    {
        var library = await fixture.SeedLibraryAsync("Paging");
        for (var i = 1; i <= 5; i++)
        {
            await using var context = fixture.CreateContext();
            context.MediaItems.Add(new MediaItem
            {
                LibraryId = library.Id,
                DisplayName = $"Item {i}",
                CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(i),
            });
            await context.SaveChangesAsync();
        }

        var repository = new MediaItemRepository(fixture.CreateContext());

        var page1 = await repository.ListByLibraryAsync(library.Id, skip: 0, take: 2);
        var page3 = await repository.ListByLibraryAsync(library.Id, skip: 4, take: 2);

        Assert.Equal(5, page1.TotalCount);
        Assert.Equal(2, page1.Items.Count);
        Assert.Equal("Item 5", page1.Items[0].DisplayName);
        Assert.Single(page3.Items, i => i.DisplayName == "Item 1");
    }

    [Fact]
    public async Task GetById_returns_null_for_unknown_id()
    {
        var repository = new MediaItemRepository(fixture.CreateContext());
        Assert.Null(await repository.GetByIdAsync(Guid.NewGuid()));
    }
}

public sealed class TagAndCollectionRepositoryTests(RepoTests fixture) : IClassFixture<RepoTests>
{
    [Fact]
    public async Task Tag_assignment_is_idempotent_guarded_and_cascade_removed()
    {
        var library = await fixture.SeedLibraryAsync("Tags");
        var item = await fixture.SeedItemAsync(library.Id);
        var tags = new TagRepository(fixture.CreateContext());

        await tags.AddAsync(new Tag { LibraryId = library.Id, Name = "favorite" });
        var tag = (await tags.ListByLibraryAsync(library.Id)).Single();

        Assert.False(await tags.IsAssignedToItemAsync(item.Id, tag.Id));
        await tags.AssignToItemAsync(item.Id, tag.Id);
        Assert.True(await tags.IsAssignedToItemAsync(item.Id, tag.Id));

        // Cascade: deleting the tag removes assignments.
        await tags.DeleteAsync(tag);
        Assert.False(await tags.IsAssignedToItemAsync(item.Id, tag.Id));
    }

    [Fact]
    public async Task Duplicate_tag_name_in_library_is_rejected_by_unique_index()
    {
        var library = await fixture.SeedLibraryAsync("TagDup");
        var tags = new TagRepository(fixture.CreateContext());

        await tags.AddAsync(new Tag { LibraryId = library.Id, Name = "rock" });

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            tags.AddAsync(new Tag { LibraryId = library.Id, Name = "Rock" }));
    }

    [Fact]
    public async Task Collection_add_remove_and_list_items_round_trip()
    {
        var library = await fixture.SeedLibraryAsync("Coll");
        var item1 = await fixture.SeedItemAsync(library.Id, "One");
        var item2 = await fixture.SeedItemAsync(library.Id, "Two");
        var collections = new CollectionRepository(fixture.CreateContext());

        await collections.AddAsync(new Collection { LibraryId = library.Id, Name = "Road trip" });
        var collection = (await collections.ListByLibraryAsync(library.Id)).Single();

        await collections.AddItemAsync(collection.Id, item1.Id);
        await collections.AddItemAsync(collection.Id, item2.Id);

        var listed = await collections.ListItemsAsync(collection.Id, skip: 0, take: 10);
        Assert.Equal(2, listed.TotalCount);
        Assert.Contains(listed.Items, i => i.Id == item1.Id);

        Assert.True(await collections.ContainsItemAsync(collection.Id, item1.Id));
        await collections.RemoveItemAsync(collection.Id, item1.Id);
        Assert.False(await collections.ContainsItemAsync(collection.Id, item1.Id));
    }
}
