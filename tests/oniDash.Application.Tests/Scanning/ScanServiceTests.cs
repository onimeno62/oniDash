using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using oniDash.Application.Libraries;
using oniDash.Application.Scanning;
using oniDash.Core.Domain;
using Xunit;

namespace oniDash.Application.Tests.Scanning;

public sealed class ScanServiceTests
{
    [Fact]
    public async Task ScanSourceAsync_indexes_new_files_and_reports_progress()
    {
        var source = new LibrarySource { Id = Guid.NewGuid(), LibraryId = Guid.NewGuid(), Name = "Music", RootPath = "C:/Media" };
        var file = new DiscoveredFile("Artist/Album/01.flac", ".flac", 1234, DateTimeOffset.UtcNow);
        var files = new InMemoryFileRepository();
        var items = new InMemoryItemRepository();
        var progress = new List<ScanProgressUpdate>();
        var service = Create(source, [file], files, items);

        var outcome = await service.ScanSourceAsync(source.Id, progress.Add);

        Assert.Equal(1, outcome.Discovered);
        Assert.Equal(1, outcome.Indexed);
        Assert.Single(files.Rows);
        Assert.Equal("Artist/Album/01.flac", files.Rows[0].RelativePath);
        Assert.Equal(ScanPhase.Done, progress.Last().Phase);
    }

    [Fact]
    public async Task ScanSourceAsync_marks_previous_rows_missing_without_deleting_them()
    {
        var source = new LibrarySource { Id = Guid.NewGuid(), LibraryId = Guid.NewGuid(), Name = "Movies", RootPath = "C:/Media" };
        var existing = new MediaFile
        {
            Id = Guid.NewGuid(), MediaItemId = Guid.NewGuid(), LibrarySourceId = source.Id,
            RelativePath = "old/movie.mkv", IdentityKey = $"{source.Id:N}/old/movie.mkv", Extension = ".mkv",
            SizeBytes = 10, LastWriteTimeUtc = DateTimeOffset.UtcNow.AddDays(-2)
        };
        var files = new InMemoryFileRepository(existing);
        var service = Create(source, [], files, new InMemoryItemRepository());

        var outcome = await service.ScanSourceAsync(source.Id);

        Assert.Equal(1, outcome.MarkedMissing);
        Assert.Single(files.Rows);
        Assert.NotNull(files.Rows[0].MissingSinceUtc);
    }

    [Fact]
    public async Task ScanSourceAsync_isolates_handler_failures_and_continues_indexing()
    {
        var source = new LibrarySource { Id = Guid.NewGuid(), LibraryId = Guid.NewGuid(), Name = "Books", RootPath = "C:/Media" };
        var files = new InMemoryFileRepository();
        var items = new InMemoryItemRepository();
        var service = Create(source,
            [new DiscoveredFile("one.epub", ".epub", 1, DateTimeOffset.UtcNow), new DiscoveredFile("two.epub", ".epub", 2, DateTimeOffset.UtcNow)],
            files, items, new ThrowingHandler());

        var outcome = await service.ScanSourceAsync(source.Id);

        Assert.Equal(2, outcome.Indexed);
        Assert.Equal(2, outcome.Diagnostics!.Count);
        Assert.All(outcome.Diagnostics, d => Assert.Equal("HANDLER_FAILED", d.Code));
    }

    private static ScanService Create(LibrarySource source, IReadOnlyList<DiscoveredFile> discovered,
        InMemoryFileRepository files, InMemoryItemRepository items, params IIndexedMediaHandler[] handlers)
        => new(new InMemorySourceRepository(source), files, new FakeEnumerator(discovered), new PlaceholderMediaItemResolver(items), handlers);

    private sealed class FakeEnumerator(IReadOnlyList<DiscoveredFile> files) : IFileEnumerator
    {
        public async IAsyncEnumerable<DiscoveredFile> EnumerateAsync(string rootPath, ScanFilterOptions options,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return file;
                await Task.Yield();
            }
        }
    }

    private sealed class InMemorySourceRepository(LibrarySource source) : ILibrarySourceRepository
    {
        public Task<IReadOnlyList<LibrarySource>> ListByLibraryAsync(Guid libraryId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<LibrarySource>>([source]);
        public Task<LibrarySource?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult<LibrarySource?>(id == source.Id ? source : null);
        public Task AddAsync(LibrarySource source, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(LibrarySource source, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateLastScannedAsync(Guid sourceId, DateTimeOffset scannedAtUtc, CancellationToken cancellationToken = default)
        { source.LastScannedAtUtc = scannedAtUtc; return Task.CompletedTask; }
    }

    private sealed class InMemoryFileRepository(params MediaFile[] initial) : IMediaFileRepository
    {
        public List<MediaFile> Rows { get; } = initial.ToList();
        public Task<IReadOnlyList<MediaFile>> ListBySourceAsync(Guid sourceId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<MediaFile>>(Rows.Where(x => x.LibrarySourceId == sourceId).ToList());
        public Task<MediaFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Rows.SingleOrDefault(x => x.Id == id));
        public Task AddAsync(MediaFile file, CancellationToken cancellationToken = default) { Rows.Add(file); return Task.CompletedTask; }
        public Task UpdateAsync(MediaFile file, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task MarkMissingAsync(IReadOnlyCollection<Guid> fileIds, DateTimeOffset missingSinceUtc, CancellationToken cancellationToken = default)
        { foreach (var row in Rows.Where(x => fileIds.Contains(x.Id))) row.MissingSinceUtc = missingSinceUtc; return Task.CompletedTask; }
    }

    private sealed class InMemoryItemRepository : IMediaItemRepository
    {
        public Task<(IReadOnlyList<MediaItem> Items, int TotalCount)> ListByLibraryAsync(Guid libraryId, int skip, int take, CancellationToken cancellationToken = default)
            => Task.FromResult<(IReadOnlyList<MediaItem>, int)>(([], 0));
        public Task<MediaItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<MediaItem?>(null);
        public Task<MediaItem> AddPlaceholderAsync(Guid libraryId, string displayName, CancellationToken cancellationToken = default)
        { var item = new MediaItem { LibraryId = libraryId, DisplayName = displayName }; return Task.FromResult(item); }
    }

    private sealed class ThrowingHandler : IIndexedMediaHandler
    {
        public Task HandleAsync(IndexedMediaContext context, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("parser failure");
    }
}
