using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using oniDash.Application.Common;
using oniDash.Application.Libraries;
using oniDash.Application.Scanning;
using oniDash.Core.Domain;
using Xunit;

namespace oniDash.Application.Tests;

/// <summary>
/// Shared in-memory fakes for the library use cases. They record the arguments they were
/// called with so tests can assert exactly what the service persisted.
/// </summary>
public sealed class FakeLibraryRepository : ILibraryRepository
{
    public List<Library> Libraries { get; } = [];
    public List<Library> Deleted { get; } = [];

    public Task<IReadOnlyList<Library>> ListAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Library>>(Libraries.OrderBy(l => l.CreatedAtUtc).ToList());

    public Task<Library?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Libraries.FirstOrDefault(l => l.Id == id));

    public Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(Libraries.Any(l => string.Equals(l.Name, name, StringComparison.OrdinalIgnoreCase) && l.Id != excludeId));

    public Task AddAsync(Library library, CancellationToken cancellationToken = default)
    {
        Libraries.Add(library);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Library library, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task DeleteAsync(Library library, CancellationToken cancellationToken = default)
    {
        Libraries.Remove(library);
        Deleted.Add(library);
        return Task.CompletedTask;
    }
}

public sealed class FakeSourceRepository : ILibrarySourceRepository
{
    public List<LibrarySource> Sources { get; } = [];
    public List<(Guid SourceId, DateTimeOffset ScannedAtUtc)> LastScannedCalls { get; } = [];

    public Task<IReadOnlyList<LibrarySource>> ListByLibraryAsync(Guid libraryId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<LibrarySource>>(Sources.Where(s => s.LibraryId == libraryId).ToList());

    public Task<LibrarySource?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Sources.FirstOrDefault(s => s.Id == id));

    public Task AddAsync(LibrarySource source, CancellationToken cancellationToken = default)
    {
        Sources.Add(source);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(LibrarySource source, CancellationToken cancellationToken = default)
    {
        Sources.Remove(source);
        return Task.CompletedTask;
    }

    public Task UpdateLastScannedAsync(Guid sourceId, DateTimeOffset scannedAtUtc, CancellationToken cancellationToken = default)
    {
        LastScannedCalls.Add((sourceId, scannedAtUtc));
        var source = Sources.FirstOrDefault(s => s.Id == sourceId);
        if (source is not null)
        {
            source.LastScannedAtUtc = scannedAtUtc;
        }

        return Task.CompletedTask;
    }
}

public sealed class FakeMediaItemRepository : IMediaItemRepository
{
    public List<MediaItem> Items { get; } = [];
    public int PlaceholderCount => Items.Count;

    public Task<(IReadOnlyList<MediaItem> Items, int TotalCount)> ListByLibraryAsync(
        Guid libraryId, int skip, int take, CancellationToken cancellationToken = default)
    {
        var query = Items.Where(i => i.LibraryId == libraryId)
            .OrderByDescending(i => i.CreatedAtUtc)
            .ToList();
        return Task.FromResult<(IReadOnlyList<MediaItem>, int)>((query.Skip(skip).Take(take).ToList(), query.Count));
    }

    public Task<MediaItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(i => i.Id == id));

    public Task<MediaItem> AddPlaceholderAsync(Guid libraryId, string displayName, CancellationToken cancellationToken = default)
    {
        var item = new MediaItem { Id = Guid.NewGuid(), LibraryId = libraryId, DisplayName = displayName };
        Items.Add(item);
        return Task.FromResult(item);
    }
}

public sealed class RecordingIndexedMediaHandler : IIndexedMediaHandler
{
    public List<IndexedMediaContext> Handled { get; } = [];
    public List<Exception> Errors { get; } = [];

    public Task HandleAsync(IndexedMediaContext context, CancellationToken cancellationToken = default)
    {
        Handled.Add(context);
        return Task.CompletedTask;
    }
}

public sealed class FakeMediaFileRepository : IMediaFileRepository
{
    public List<MediaFile> Files { get; } = [];
    public List<MediaFile> Updated { get; } = [];
    public List<(IReadOnlyCollection<Guid> Ids, DateTimeOffset Since)> MissingCalls { get; } = [];

    public Task<IReadOnlyList<MediaFile>> ListBySourceAsync(Guid sourceId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<MediaFile>>(Files.Where(f => f.LibrarySourceId == sourceId).ToList());

    public Task<MediaFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Files.FirstOrDefault(f => f.Id == id));

    public Task AddAsync(MediaFile file, CancellationToken cancellationToken = default)
    {
        // Mirror the real repository: EF assigns the generated id before AddAsync returns.
        var stored = file.Id == Guid.Empty
            ? new MediaFile
            {
                Id = Guid.NewGuid(),
                MediaItemId = file.MediaItemId,
                LibrarySourceId = file.LibrarySourceId,
                RelativePath = file.RelativePath,
                IdentityKey = file.IdentityKey,
                MissingSinceUtc = file.MissingSinceUtc,
                Extension = file.Extension,
                SizeBytes = file.SizeBytes,
                LastWriteTimeUtc = file.LastWriteTimeUtc,
                CreatedAtUtc = file.CreatedAtUtc,
            }
            : file;
        Files.Add(stored);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(MediaFile file, CancellationToken cancellationToken = default)
    {
        Updated.Add(file);
        return Task.CompletedTask;
    }

    public Task MarkMissingAsync(IReadOnlyCollection<Guid> fileIds, DateTimeOffset missingSinceUtc, CancellationToken cancellationToken = default)
    {
        MissingCalls.Add((fileIds, missingSinceUtc));
        foreach (var file in Files.Where(f => fileIds.Contains(f.Id)))
        {
            file.MissingSinceUtc = missingSinceUtc;
        }

        return Task.CompletedTask;
    }
}

public sealed class FakeTagRepository : ITagRepository
{
    public List<Tag> Tags { get; } = [];
    public List<(Guid ItemId, Guid TagId)> Assignments { get; } = [];

    public Task<IReadOnlyList<Tag>> ListByLibraryAsync(Guid libraryId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Tag>>(Tags.Where(t => t.LibraryId == libraryId).OrderBy(t => t.Name).ToList());

    public Task<Tag?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Tags.FirstOrDefault(t => t.Id == id));

    public Task<bool> ExistsByNameAsync(Guid libraryId, string name, CancellationToken cancellationToken = default) =>
        Task.FromResult(Tags.Any(t => t.LibraryId == libraryId && string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase)));

    public Task<bool> IsAssignedToItemAsync(Guid mediaItemId, Guid tagId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Assignments.Any(a => a.ItemId == mediaItemId && a.TagId == tagId));

    public Task AddAsync(Tag tag, CancellationToken cancellationToken = default)
    {
        Tags.Add(tag);
        return Task.CompletedTask;
    }

    public Task AssignToItemAsync(Guid mediaItemId, Guid tagId, CancellationToken cancellationToken = default)
    {
        Assignments.Add((mediaItemId, tagId));
        return Task.CompletedTask;
    }

    public Task UnassignFromItemAsync(Guid mediaItemId, Guid tagId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Assignments.RemoveAll(a => a.ItemId == mediaItemId && a.TagId == tagId));

    public Task DeleteAsync(Tag tag, CancellationToken cancellationToken = default)
    {
        Tags.Remove(tag);
        return Task.CompletedTask;
    }
}

public sealed class FakeCollectionRepository : ICollectionRepository
{
    public List<Collection> Collections { get; } = [];
    public List<(Guid CollectionId, Guid ItemId)> Items { get; } = [];

    public Task<IReadOnlyList<Collection>> ListByLibraryAsync(Guid libraryId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Collection>>(Collections.Where(c => c.LibraryId == libraryId).ToList());

    public Task<Collection?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Collections.FirstOrDefault(c => c.Id == id));

    public Task<bool> ExistsByNameAsync(Guid libraryId, string name, CancellationToken cancellationToken = default) =>
        Task.FromResult(Collections.Any(c => c.LibraryId == libraryId && string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase)));

    public Task<bool> ContainsItemAsync(Guid collectionId, Guid mediaItemId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.Any(i => i.CollectionId == collectionId && i.ItemId == mediaItemId));

    public Task AddAsync(Collection collection, CancellationToken cancellationToken = default)
    {
        Collections.Add(collection);
        return Task.CompletedTask;
    }

    public Task AddItemAsync(Guid collectionId, Guid mediaItemId, CancellationToken cancellationToken = default)
    {
        Items.Add((collectionId, mediaItemId));
        return Task.CompletedTask;
    }

    public Task RemoveItemAsync(Guid collectionId, Guid mediaItemId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.RemoveAll(i => i.CollectionId == collectionId && i.ItemId == mediaItemId));

    public Task<(IReadOnlyList<MediaItem> Items, int TotalCount)> ListItemsAsync(
        Guid collectionId, int skip, int take, CancellationToken cancellationToken = default) =>
        Task.FromResult<(IReadOnlyList<MediaItem>, int)>(([], 0));

    public Task DeleteAsync(Collection collection, CancellationToken cancellationToken = default)
    {
        Collections.Remove(collection);
        return Task.CompletedTask;
    }
}

public sealed class FakeFileSystemProbe(params string[] existingDirectories) : IFileSystemProbe
{
    private readonly HashSet<string> _existing = existingDirectories
        .Select(p => p.Replace('/', '\\').TrimEnd('\\'))
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    public List<string> ProbedPaths { get; } = [];

    public bool IsExistingDirectory(string path)
    {
        ProbedPaths.Add(path);
        var normalized = path.Replace('/', '\\').TrimEnd('\\');
        return _existing.Contains(normalized);
    }
}

public static class TestValues
{
    public static readonly Guid LibraryId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid ItemId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid SourceId = Guid.Parse("33333333-3333-3333-3333-333333333333");
}

/// <summary>
/// IServiceScopeFactory that always resolves the given singleton instance — lets the
/// singleton ScanJobManager run against fakes in unit tests.
/// </summary>
public sealed class FixedScopeFactory(IScanService scanService) : IServiceScopeFactory
{
    private sealed class FixedProvider(IScanService service) : IServiceProvider
    {
        public object? GetService(Type serviceType) =>
            serviceType == typeof(IScanService) ? service : null;
    }

    private sealed class FixedScope(IScanService service) : IServiceScope
    {
        public IServiceProvider ServiceProvider { get; } = new FixedProvider(service);
        public void Dispose()
        {
        }
    }

    public IServiceScope CreateScope() => new FixedScope(scanService);
}
