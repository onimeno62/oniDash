using System;
using System.Linq;
using System.Threading.Tasks;
using oniDash.Application.Common;
using oniDash.Application.Libraries;
using oniDash.Core.Domain;
using Xunit;

namespace oniDash.Application.Tests;

public sealed class LibraryServiceTests : IDisposable
{
    private readonly FakeLibraryRepository _libraries = new();

    public void Dispose()
    {
        // Nothing to clean up — in-memory fakes.
    }

    [Fact]
    public async Task Create_trims_name_and_returns_dto()
    {
        var service = new LibraryService(_libraries);

        var dto = await service.CreateAsync(new CreateLibraryRequest("  Music  "));

        Assert.Equal("Music", dto.Name);
        Assert.Single(_libraries.Libraries, l => l.Name == "Music");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_rejects_missing_name(string? name)
    {
        var service = new LibraryService(_libraries);

        var error = await Assert.ThrowsAsync<ValidationException>(
            () => service.CreateAsync(new CreateLibraryRequest(name!)));

        Assert.Contains("required", error.Message);
    }

    [Fact]
    public async Task Create_rejects_duplicate_name_case_insensitively()
    {
        var service = new LibraryService(_libraries);
        await service.CreateAsync(new CreateLibraryRequest("Music"));

        var error = await Assert.ThrowsAsync<ConflictException>(
            () => service.CreateAsync(new CreateLibraryRequest("MUSIC")));

        Assert.Contains("already exists", error.Message);
    }

    [Fact]
    public async Task Get_unknown_id_throws_not_found()
    {
        var service = new LibraryService(_libraries);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Rename_updates_the_library()
    {
        var service = new LibraryService(_libraries);
        var created = await service.CreateAsync(new CreateLibraryRequest("Old"));

        var renamed = await service.RenameAsync(created.Id, new UpdateLibraryRequest("New"));

        Assert.Equal("New", renamed.Name);
        Assert.Equal("New", _libraries.Libraries.Single(l => l.Id == created.Id).Name);
    }

    [Fact]
    public async Task Delete_removes_the_library()
    {
        var service = new LibraryService(_libraries);
        var created = await service.CreateAsync(new CreateLibraryRequest("Doomed"));

        await service.DeleteAsync(created.Id);

        Assert.Empty(_libraries.Libraries);
        Assert.Single(_libraries.Deleted, l => l.Id == created.Id);
    }
}

public sealed class SourceServiceTests : IDisposable
{
    private readonly FakeLibraryRepository _libraries = new();
    private readonly FakeSourceRepository _sources = new();
    private readonly FakeFileSystemProbe _fs;

    public SourceServiceTests()
    {
        _libraries.Libraries.Add(new Library { Id = TestValues.LibraryId, Name = "Music" });
        _fs = new FakeFileSystemProbe(@"C:\Media\Music");
    }

    public void Dispose()
    {
    }

    private SourceService CreateService() => new(_libraries, _sources, _fs);

    [Fact]
    public async Task Add_normalizes_and_stores_the_source()
    {
        var service = CreateService();

        var dto = await service.AddAsync(
            TestValues.LibraryId,
            new CreateSourceRequest("Main", @"C:\Media\Music\"));

        Assert.Equal(@"C:\Media\Music", dto.RootPath);
        Assert.Single(_sources.Sources, s => s.Id == dto.Id);
    }

    [Fact]
    public async Task Add_accepts_forward_slashes_and_normalizes_them()
    {
        var service = CreateService();

        var dto = await service.AddAsync(
            TestValues.LibraryId,
            new CreateSourceRequest("Main", "C:/Media/Music"));

        Assert.Equal(@"C:\Media\Music", dto.RootPath);
    }

    [Fact]
    public async Task Add_rejects_nonexistent_folder()
    {
        var service = CreateService();

        var error = await Assert.ThrowsAsync<ValidationException>(() => service.AddAsync(
            TestValues.LibraryId,
            new CreateSourceRequest("Main", @"C:\No\Such\Dir")));

        Assert.Contains("does not exist", error.Message);
        Assert.Contains(@"C:\No\Such\Dir", _fs.ProbedPaths);
    }

    [Fact]
    public async Task Add_rejects_relative_path()
    {
        var service = CreateService();

        var error = await Assert.ThrowsAsync<ValidationException>(() => service.AddAsync(
            TestValues.LibraryId,
            new CreateSourceRequest("Main", "relative/path")));

        Assert.Contains("absolute", error.Message);
    }

    [Fact]
    public async Task Add_rejects_duplicate_path_in_same_library_case_insensitively()
    {
        var service = CreateService();
        await service.AddAsync(TestValues.LibraryId, new CreateSourceRequest("Main", @"C:\Media\Music"));

        var error = await Assert.ThrowsAsync<ConflictException>(() => service.AddAsync(
            TestValues.LibraryId,
            new CreateSourceRequest("Again", @"c:\media\music")));

        Assert.Contains("already configured", error.Message);
    }

    [Fact]
    public async Task Add_for_unknown_library_throws_not_found()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<NotFoundException>(() => service.AddAsync(
            Guid.NewGuid(),
            new CreateSourceRequest("Main", @"C:\Media\Music")));
    }

    [Fact]
    public async Task Remove_deletes_the_source()
    {
        var service = CreateService();
        var source = await service.AddAsync(TestValues.LibraryId, new CreateSourceRequest("Main", @"C:\Media\Music"));

        await service.RemoveAsync(source.Id);

        Assert.Empty(_sources.Sources);
    }

    [Fact]
    public async Task Remove_unknown_source_throws_not_found()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<NotFoundException>(() => service.RemoveAsync(Guid.NewGuid()));
    }
}

public sealed class MediaItemServiceTests : IDisposable
{
    private readonly FakeLibraryRepository _libraries = new();
    private readonly FakeMediaItemRepository _items = new();

    public MediaItemServiceTests()
    {
        _libraries.Libraries.Add(new Library { Id = TestValues.LibraryId, Name = "Music" });
    }

    public void Dispose()
    {
    }

    [Fact]
    public async Task ListByLibrary_validates_paging_and_maps_to_dtos()
    {
        var service = new MediaItemService(_libraries, _items);
        for (var i = 1; i <= 3; i++)
        {
            _items.Items.Add(new MediaItem
            {
                LibraryId = TestValues.LibraryId,
                DisplayName = $"Item {i}",
                CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(i),
            });
        }

        var result = await service.ListByLibraryAsync(TestValues.LibraryId, page: 2, pageSize: 2);

        Assert.Equal(3, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task ListByLibrary_rejects_invalid_paging()
    {
        var service = new MediaItemService(_libraries, _items);

        await Assert.ThrowsAsync<ValidationException>(
            () => service.ListByLibraryAsync(TestValues.LibraryId, page: 0, pageSize: 10));
        await Assert.ThrowsAsync<ValidationException>(
            () => service.ListByLibraryAsync(TestValues.LibraryId, page: 1, pageSize: 500));
    }
}

public sealed class TagServiceTests : IDisposable
{
    private readonly FakeLibraryRepository _libraries = new();
    private readonly FakeMediaItemRepository _items = new();
    private readonly FakeTagRepository _tags = new();

    public TagServiceTests()
    {
        _libraries.Libraries.Add(new Library { Id = TestValues.LibraryId, Name = "Music" });
        _items.Items.Add(new MediaItem { Id = TestValues.ItemId, LibraryId = TestValues.LibraryId, DisplayName = "Album" });
    }

    public void Dispose()
    {
    }

    private TagService CreateService() => new(_libraries, _items, _tags);

    [Fact]
    public async Task Create_trims_and_stores_the_tag()
    {
        var service = CreateService();

        var dto = await service.CreateAsync(TestValues.LibraryId, new CreateTagRequest("  rock  "));

        Assert.Equal("rock", dto.Name);
        Assert.Single(_tags.Tags);
    }

    [Fact]
    public async Task Create_rejects_duplicates_within_the_same_library()
    {
        var service = CreateService();
        await service.CreateAsync(TestValues.LibraryId, new CreateTagRequest("rock"));

        await Assert.ThrowsAsync<ConflictException>(
            () => service.CreateAsync(TestValues.LibraryId, new CreateTagRequest("ROCK")));
    }

    [Fact]
    public async Task Assign_links_tag_to_item()
    {
        var service = CreateService();
        var tag = await service.CreateAsync(TestValues.LibraryId, new CreateTagRequest("rock"));

        await service.AssignToItemAsync(TestValues.ItemId, new AssignTagRequest(tag.Id));

        Assert.Equal((TestValues.ItemId, tag.Id), _tags.Assignments.Single());
    }

    [Fact]
    public async Task Assign_rejects_tag_from_another_library()
    {
        var otherLibraryId = Guid.NewGuid();
        _libraries.Libraries.Add(new Library { Id = otherLibraryId, Name = "Other" });
        _tags.Tags.Add(new Tag { Id = Guid.NewGuid(), LibraryId = otherLibraryId, Name = "foreign" });
        var service = CreateService();
        var foreignTagId = _tags.Tags.Single().Id;

        var error = await Assert.ThrowsAsync<ValidationException>(
            () => service.AssignToItemAsync(TestValues.ItemId, new AssignTagRequest(foreignTagId)));

        Assert.Contains("different library", error.Message);
        Assert.Empty(_tags.Assignments);
    }

    [Fact]
    public async Task Assign_twice_conflicts()
    {
        var service = CreateService();
        var tag = await service.CreateAsync(TestValues.LibraryId, new CreateTagRequest("rock"));
        await service.AssignToItemAsync(TestValues.ItemId, new AssignTagRequest(tag.Id));

        await Assert.ThrowsAsync<ConflictException>(
            () => service.AssignToItemAsync(TestValues.ItemId, new AssignTagRequest(tag.Id)));
    }

    [Fact]
    public async Task Assign_unknown_tag_throws_not_found()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<NotFoundException>(
            () => service.AssignToItemAsync(TestValues.ItemId, new AssignTagRequest(Guid.NewGuid())));
    }

    [Fact]
    public async Task Unassign_removes_the_link()
    {
        var service = CreateService();
        var tag = await service.CreateAsync(TestValues.LibraryId, new CreateTagRequest("rock"));
        await service.AssignToItemAsync(TestValues.ItemId, new AssignTagRequest(tag.Id));

        await service.UnassignFromItemAsync(TestValues.ItemId, tag.Id);

        Assert.Empty(_tags.Assignments);
    }

    [Fact]
    public async Task Delete_removes_the_tag()
    {
        var service = CreateService();
        var tag = await service.CreateAsync(TestValues.LibraryId, new CreateTagRequest("rock"));

        await service.DeleteAsync(tag.Id);

        Assert.Empty(_tags.Tags);
    }
}

public sealed class CollectionServiceTests : IDisposable
{
    private readonly FakeLibraryRepository _libraries = new();
    private readonly FakeMediaItemRepository _items = new();
    private readonly FakeCollectionRepository _collections = new();

    public CollectionServiceTests()
    {
        _libraries.Libraries.Add(new Library { Id = TestValues.LibraryId, Name = "Music" });
        _items.Items.Add(new MediaItem { Id = TestValues.ItemId, LibraryId = TestValues.LibraryId, DisplayName = "Album" });
    }

    public void Dispose()
    {
    }

    private CollectionService CreateService() => new(_libraries, _items, _collections);

    [Fact]
    public async Task Create_stores_the_collection()
    {
        var service = CreateService();

        var dto = await service.CreateAsync(TestValues.LibraryId, new CreateCollectionRequest("Favorites"));

        Assert.Equal("Favorites", dto.Name);
        Assert.Single(_collections.Collections, c => c.Id == dto.Id);
    }

    [Fact]
    public async Task Create_rejects_duplicates()
    {
        var service = CreateService();
        await service.CreateAsync(TestValues.LibraryId, new CreateCollectionRequest("Favorites"));

        await Assert.ThrowsAsync<ConflictException>(
            () => service.CreateAsync(TestValues.LibraryId, new CreateCollectionRequest("favorites")));
    }

    [Fact]
    public async Task AddItem_links_item_to_collection()
    {
        var service = CreateService();
        var collection = await service.CreateAsync(TestValues.LibraryId, new CreateCollectionRequest("Favorites"));

        await service.AddItemAsync(collection.Id, new AddCollectionItemRequest(TestValues.ItemId));

        Assert.Equal((collection.Id, TestValues.ItemId), _collections.Items.Single());
    }

    [Fact]
    public async Task AddItem_rejects_item_from_another_library()
    {
        var service = CreateService();
        var collection = await service.CreateAsync(TestValues.LibraryId, new CreateCollectionRequest("Favorites"));
        var foreignItemId = Guid.NewGuid();
        _items.Items.Add(new MediaItem { Id = foreignItemId, LibraryId = Guid.NewGuid(), DisplayName = "Foreign" });

        var error = await Assert.ThrowsAsync<ValidationException>(
            () => service.AddItemAsync(collection.Id, new AddCollectionItemRequest(foreignItemId)));

        Assert.Contains("different library", error.Message);
        Assert.Empty(_collections.Items);
    }

    [Fact]
    public async Task AddItem_twice_conflicts()
    {
        var service = CreateService();
        var collection = await service.CreateAsync(TestValues.LibraryId, new CreateCollectionRequest("Favorites"));
        await service.AddItemAsync(collection.Id, new AddCollectionItemRequest(TestValues.ItemId));

        await Assert.ThrowsAsync<ConflictException>(
            () => service.AddItemAsync(collection.Id, new AddCollectionItemRequest(TestValues.ItemId)));
    }

    [Fact]
    public async Task RemoveItem_unlinks_the_item()
    {
        var service = CreateService();
        var collection = await service.CreateAsync(TestValues.LibraryId, new CreateCollectionRequest("Favorites"));
        await service.AddItemAsync(collection.Id, new AddCollectionItemRequest(TestValues.ItemId));

        await service.RemoveItemAsync(collection.Id, TestValues.ItemId);

        Assert.Empty(_collections.Items);
    }

    [Fact]
    public async Task Delete_removes_the_collection()
    {
        var service = CreateService();
        var collection = await service.CreateAsync(TestValues.LibraryId, new CreateCollectionRequest("Favorites"));

        await service.DeleteAsync(collection.Id);

        Assert.Empty(_collections.Collections);
    }
}
