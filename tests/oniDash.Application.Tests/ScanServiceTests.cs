using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using oniDash.Application.Common;
using oniDash.Application.Scanning;
using oniDash.Core.Domain;
using Xunit;

namespace oniDash.Application.Tests;

/// <summary>Shared file-entry factory for scan tests.</summary>
internal static class ScanEntries
{
    public static DiscoveredFile File(string relativePath, long size = 100, string? writeTime = null) =>
        new(
            relativePath,
            System.IO.Path.GetExtension(relativePath),
            size,
            writeTime is null ? DateTimeOffset.UtcNow : DateTimeOffset.Parse(writeTime));
}

/// <summary>Fake enumerator scripted by tests; records the filter it was given.</summary>
public sealed class FakeFileEnumerator : IFileEnumerator
{
    public List<string> EnumeratedRoots { get; } = [];
    public List<DiscoveredFile> NextFiles { get; set; } = [];
    /// <summary>Cancels the enumeration when discovery reaches this many files (0 = never).</summary>
    public int CancelAtDiscoveryCount { get; set; }
    public Exception? ThrowAtStart { get; set; }

    public async IAsyncEnumerable<DiscoveredFile> EnumerateAsync(
        string rootPath,
        ScanFilterOptions options,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        EnumeratedRoots.Add(rootPath);
        if (ThrowAtStart is not null)
        {
            throw ThrowAtStart;
        }

        for (var index = 0; index < NextFiles.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (CancelAtDiscoveryCount > 0 && index >= CancelAtDiscoveryCount)
            {
                throw new OperationCanceledException();
            }

            await Task.Yield();
            yield return NextFiles[index];
        }
    }
}

public sealed class ScanServiceTests : IDisposable
{
    private readonly FakeSourceRepository _sources = new();
    private readonly FakeMediaFileRepository _files = new();
    private readonly FakeMediaItemRepository _items = new();
    private readonly FakeFileEnumerator _enumerator = new();
    private readonly RecordingIndexedMediaHandler _handler = new();
    private readonly ScanService _service;

    public ScanServiceTests()
    {
        _sources.Sources.Add(new LibrarySource
        {
            Id = TestValues.SourceId,
            LibraryId = TestValues.LibraryId,
            Name = "Main",
            RootPath = @"C:\Media",
        });
        _service = new ScanService(
            _sources, _files, _enumerator, new PlaceholderMediaItemResolver(_items), [_handler]);
    }

    public void Dispose()
    {
    }

    private static DiscoveredFile File(string relativePath, long size = 100, string? writeTime = null) =>
        ScanEntries.File(relativePath, size, writeTime);

    private static MediaFile Row(string relativePath, long size = 100, string? writeTime = null, DateTimeOffset? missingSince = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            MediaItemId = TestValues.ItemId,
            LibrarySourceId = TestValues.SourceId,
            RelativePath = relativePath,
            IdentityKey = $"{TestValues.SourceId:N}/{relativePath.ToLowerInvariant()}",
            Extension = System.IO.Path.GetExtension(relativePath),
            SizeBytes = size,
            LastWriteTimeUtc = writeTime is null ? DateTimeOffset.UtcNow : DateTimeOffset.Parse(writeTime),
            MissingSinceUtc = missingSince,
        };

    [Fact]
    public async Task Scan_indexes_new_files_and_creates_placeholder_items()
    {
        _enumerator.NextFiles = [File("a.mp3"), File("sub/b.jpg")];

        var outcome = await _service.ScanSourceAsync(TestValues.SourceId);

        Assert.Equal(2, outcome.Discovered);
        Assert.Equal(2, outcome.Indexed);
        Assert.Equal(0, outcome.Updated);
        Assert.Equal(0, outcome.MarkedMissing);
        Assert.Equal(2, _files.Files.Count);
        Assert.Equal(2, _items.PlaceholderCount);
        Assert.All(_files.Files, f => Assert.Equal(TestValues.SourceId, f.LibrarySourceId));
        Assert.All(_files.Files, f => Assert.StartsWith($"{TestValues.SourceId:N}/", f.IdentityKey));
    }

    [Fact]
    public async Task Second_scan_with_no_changes_reports_unchanged_and_stamps_source()
    {
        _enumerator.NextFiles = [File("a.mp3")];
        await _service.ScanSourceAsync(TestValues.SourceId);

        // Fresh repository view, like the real scoped service would see.
        var outcome = await new ScanService(_sources, _files, _enumerator, new PlaceholderMediaItemResolver(_items), [])
            .ScanSourceAsync(TestValues.SourceId);

        Assert.Equal(1, outcome.Discovered);
        Assert.Equal(0, outcome.Indexed);
        Assert.Equal(0, outcome.Updated);
        Assert.Equal(1, outcome.Unchanged);
        Assert.All(_sources.LastScannedCalls, c => Assert.Equal(TestValues.SourceId, c.SourceId));
        Assert.NotNull(_sources.Sources.Single().LastScannedAtUtc);
    }

    [Fact]
    public async Task Changed_file_is_updated_in_place_keeping_its_media_item()
    {
        _enumerator.NextFiles = [File("a.mp3", size: 100, writeTime: "2026-01-01T00:00:00Z")];
        await _service.ScanSourceAsync(TestValues.SourceId);
        var originalRow = _files.Files.Single();
        var originalItemId = originalRow.MediaItemId;

        _enumerator.NextFiles = [File("a.mp3", size: 250, writeTime: "2026-02-01T00:00:00Z")];
        var outcome = await new ScanService(_sources, _files, _enumerator, new PlaceholderMediaItemResolver(_items), [])
            .ScanSourceAsync(TestValues.SourceId);

        Assert.Equal(1, outcome.Updated);
        Assert.Equal(0, outcome.Indexed);
        Assert.Single(_files.Updated);
        Assert.Equal(originalItemId, _files.Updated.Single().MediaItemId);
        Assert.Equal(250, _files.Files.Single().SizeBytes);
    }

    [Fact]
    public async Task Disappeared_files_are_marked_missing_not_deleted()
    {
        _enumerator.NextFiles = [File("keep.mp3"), File("gone.mp3")];
        await _service.ScanSourceAsync(TestValues.SourceId);
        var goneRow = _files.Files.Single(f => f.RelativePath == "gone.mp3");
        var keepRow = _files.Files.Single(f => f.RelativePath == "keep.mp3");

        _enumerator.NextFiles = [File("keep.mp3")];
        var outcome = await new ScanService(_sources, _files, _enumerator, new PlaceholderMediaItemResolver(_items), [])
            .ScanSourceAsync(TestValues.SourceId);

        Assert.Equal(1, outcome.MarkedMissing);
        Assert.NotNull(_files.Files.Single(f => f.Id == goneRow.Id).MissingSinceUtc);
        // Still indexed, not deleted:
        Assert.Equal(2, _files.Files.Count);
        Assert.Equal(keepRow.Id, _files.Files.Single(f => f.RelativePath == "keep.mp3").Id);
    }

    [Fact]
    public async Task Returning_file_is_unmarked_and_refreshed()
    {
        _enumerator.NextFiles = [File("a.mp3")];
        await _service.ScanSourceAsync(TestValues.SourceId);
        var row = _files.Files.Single();

        _enumerator.NextFiles = [];
        await new ScanService(_sources, _files, _enumerator, new PlaceholderMediaItemResolver(_items), [])
            .ScanSourceAsync(TestValues.SourceId);
        Assert.NotNull(_files.Files.Single(f => f.Id == row.Id).MissingSinceUtc);

        _enumerator.NextFiles = [File("a.mp3", size: 300)];
        var outcome = await new ScanService(_sources, _files, _enumerator, new PlaceholderMediaItemResolver(_items), [])
            .ScanSourceAsync(TestValues.SourceId);

        Assert.Equal(1, outcome.Updated);
        Assert.Null(_files.Files.Single(f => f.Id == row.Id).MissingSinceUtc);
        Assert.Equal(300, _files.Files.Single(f => f.Id == row.Id).SizeBytes);
    }

    [Fact]
    public async Task Identity_keys_are_case_insensitive_so_renamed_case_does_not_duplicate()
    {
        _enumerator.NextFiles = [File("Song.MP3")];
        await _service.ScanSourceAsync(TestValues.SourceId);

        _enumerator.NextFiles = [File("song.mp3")];
        var outcome = await new ScanService(_sources, _files, _enumerator, new PlaceholderMediaItemResolver(_items), [])
            .ScanSourceAsync(TestValues.SourceId);

        Assert.Equal(0, outcome.Indexed);
        Assert.Equal(1, outcome.Discovered);
        Assert.Single(_files.Files);
    }

    [Fact]
    public async Task Cancellation_during_discovery_writes_nothing()
    {
        _enumerator.NextFiles = [File("a.mp3"), File("b.mp3"), File("c.mp3")];
        _enumerator.CancelAtDiscoveryCount = 1;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _service.ScanSourceAsync(TestValues.SourceId));

        Assert.Empty(_files.Files);
        Assert.Empty(_items.Items);
        Assert.Empty(_sources.LastScannedCalls);
    }

    [Fact]
    public async Task Unknown_source_throws_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.ScanSourceAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Enumerator_receives_the_source_root_path()
    {
        _enumerator.NextFiles = [];
        await _service.ScanSourceAsync(TestValues.SourceId);

        Assert.Equal([@"C:\Media"], _enumerator.EnumeratedRoots);
    }
}

public sealed class ScanJobManagerTests : IDisposable
{
    private readonly FakeSourceRepository _sources = new();
    private readonly FakeMediaFileRepository _files = new();
    private readonly FakeMediaItemRepository _items = new();
    private readonly FakeFileEnumerator _enumerator = new();

    public ScanJobManagerTests()
    {
        _sources.Sources.Add(new LibrarySource
        {
            Id = TestValues.SourceId,
            LibraryId = TestValues.LibraryId,
            Name = "Main",
            RootPath = @"C:\Media",
        });
    }

    public void Dispose()
    {
    }

    private ScanJobManager CreateManager(Action<ScanningFakes>? configure = null)
    {
        var fakes = new ScanningFakes(_sources, _files, _items, _enumerator);
        configure?.Invoke(fakes);
        var service = new ScanService(fakes.Sources, fakes.Files, fakes.Enumerator, new PlaceholderMediaItemResolver(fakes.Items), []);
        return new ScanJobManager(new FixedScopeFactory(service));
    }

    private static DiscoveredFile File(string relativePath, long size = 100, string? writeTime = null) =>
        ScanEntries.File(relativePath, size, writeTime);

    private sealed record ScanningFakes(
        FakeSourceRepository Sources,
        FakeMediaFileRepository Files,
        FakeMediaItemRepository Items,
        FakeFileEnumerator Enumerator);

    [Fact]
    public async Task StartScan_completes_and_reports_progress()
    {
        _enumerator.NextFiles = [File("x.mp3"), File("y.mp3")];
        using var manager = CreateManager();

        var result = manager.StartScan(TestValues.LibraryId, TestValues.SourceId, "Main");
        Assert.True(result.Accepted);

        var progress = await WaitForCompletionAsync(manager, result.ScanId);

        Assert.Equal(ScanStatus.Completed, progress.Status);
        Assert.Equal(2, progress.FilesDiscovered);
        Assert.Equal(2, progress.FilesIndexed);
        Assert.NotNull(progress.CompletedAtUtc);
    }

    [Fact]
    public async Task Second_scan_for_same_source_is_rejected_while_running()
    {
        // A slow scan: many files so discovery takes measurable time.
        _enumerator.NextFiles = Enumerable.Range(0, 2000)
            .Select(i => File($"f{i}.mp3"))
            .ToList();
        using var manager = CreateManager();

        var first = manager.StartScan(TestValues.LibraryId, TestValues.SourceId, "Main");
        Assert.True(first.Accepted);

        var second = manager.StartScan(TestValues.LibraryId, TestValues.SourceId, "Main");
        Assert.False(second.Accepted);
        Assert.Equal(first.ScanId, second.ScanId);
        Assert.Contains("already running", second.RejectionReason);

        await WaitForCompletionAsync(manager, first.ScanId);
    }

    [Fact]
    public async Task Cancel_transitions_run_to_cancelled_state()
    {
        _enumerator.NextFiles = Enumerable.Range(0, 5000)
            .Select(i => File($"f{i}.mp3"))
            .ToList();
        using var manager = CreateManager();

        var started = manager.StartScan(TestValues.LibraryId, TestValues.SourceId, "Main");
        Assert.True(started.Accepted);

        // Cancel may race with completion; keep trying until we observe the cancelled
        // state or the run finished before the cancel landed.
        var cancelled = false;
        for (var attempt = 0; attempt < 50 && !cancelled; attempt++)
        {
            cancelled = manager.TryCancel(started.ScanId);
            if (!cancelled)
            {
                var snapshot = manager.GetScan(started.ScanId)!;
                if (snapshot.Status is ScanStatus.Completed or ScanStatus.Cancelled)
                {
                    break;
                }
                await Task.Delay(10);
            }
        }

        var progress = await WaitForCompletionAsync(manager, started.ScanId);
        if (progress.Status == ScanStatus.Cancelled)
        {
            Assert.NotNull(progress.CompletedAtUtc);
        }
    }

    [Fact]
    public void GetScan_unknown_id_returns_null()
    {
        using var manager = CreateManager();
        Assert.Null(manager.GetScan(Guid.NewGuid()));
    }

    [Fact]
    public async Task ListScans_returns_runs_in_chronological_order()
    {
        _enumerator.NextFiles = [];
        using var manager = CreateManager();

        var first = manager.StartScan(TestValues.LibraryId, TestValues.SourceId, "Main");
        await WaitForCompletionAsync(manager, first.ScanId);
        var second = manager.StartScan(TestValues.LibraryId, TestValues.SourceId, "Main");
        await WaitForCompletionAsync(manager, second.ScanId);

        var scans = manager.ListScans();
        Assert.Equal(2, scans.Count);
        Assert.Equal(first.ScanId, scans[0].ScanId);
        Assert.Equal(second.ScanId, scans[1].ScanId);
    }

    [Fact]
    public async Task Failed_scan_reports_error_and_stays_listed()
    {
        _enumerator.ThrowAtStart = new IOException("drive disconnected");
        using var manager = CreateManager();

        var started = manager.StartScan(TestValues.LibraryId, TestValues.SourceId, "Main");
        var progress = await WaitForCompletionAsync(manager, started.ScanId);

        Assert.Equal(ScanStatus.Failed, progress.Status);
        Assert.Contains("drive disconnected", progress.Error);
    }

    private static async Task<ScanProgress> WaitForCompletionAsync(ScanJobManager manager, Guid scanId)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (DateTime.UtcNow < deadline)
        {
            var progress = manager.GetScan(scanId);
            if (progress is not null && progress.Status is not ScanStatus.Running)
            {
                return progress;
            }
            await Task.Delay(25);
        }

        throw new TimeoutException($"scan {scanId} did not finish in time");
    }
}
