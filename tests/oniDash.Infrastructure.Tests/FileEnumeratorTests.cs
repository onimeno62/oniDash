using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using oniDash.Application.Scanning;
using oniDash.Core.Domain;
using oniDash.Infrastructure.Persistence;
using oniDash.Infrastructure.Repositories;
using oniDash.Infrastructure.Scanning;
using Xunit;

namespace oniDash.Infrastructure.Tests;

/// <summary>
/// Real-filesystem tests for the read-only enumerator (AGENTS.md rule 11: it must only
/// read). Each test builds a scratch tree under the test's temp folder.
/// </summary>
public sealed class FileEnumeratorTests : IDisposable
{
    private readonly string _root;
    private readonly FileEnumerator _enumerator = new();

    public FileEnumeratorTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "onidash-enumtests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_root, recursive: true);
        }
        catch (DirectoryNotFoundException)
        {
        }
    }

    private string Dir(string relative) => Path.Combine(_root, relative);

    private void Write(string relative, string content = "data")
    {
        var fullPath = Path.Combine(_root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content);
    }

    private async Task<List<DiscoveredFile>> EnumerateAsync(ScanFilterOptions? options = null) =>
        await _enumerator.EnumerateAsync(_root, options ?? ScanFilterOptions.Default).ToListAsync();

    [Fact]
    public async Task Walks_nested_directories_and_reports_relative_paths()
    {
        Write("a.mp3", "aaaa");
        Write("nested/b.flac", "bb");
        Write("nested/deeper/c.mkv", "ccc");

        var files = await EnumerateAsync();

        Assert.Equal(3, files.Count);
        Assert.All(files, f => Assert.DoesNotContain('\\', f.RelativePath));
        Assert.Contains(files, f => f.RelativePath == "a.mp3" && f.SizeBytes == 4);
        Assert.Contains(files, f => f.RelativePath == "nested/b.flac");
        Assert.Contains(files, f => f.RelativePath == "nested/deeper/c.mkv");
        Assert.All(files, f => Assert.Equal(Path.GetExtension(f.RelativePath), f.Extension));
    }

    [Fact]
    public async Task Skips_hidden_files_and_directories()
    {
        Write("visible.mp3");
        Write("hidden.mp3");
        File.SetAttributes(Path.Combine(_root, "hidden.mp3"), FileAttributes.Hidden);
        Write(".secret/track.mp3");
        File.SetAttributes(Dir(".secret"), FileAttributes.Hidden);

        var files = await EnumerateAsync();

        Assert.Single(files, f => f.RelativePath == "visible.mp3");
    }

    [Fact]
    public async Task Skips_junk_directories()
    {
        Write("keep.mp3");
        Write("$RECYCLE.BIN/junk.mp3");
        Write("node_modules/pkg/x.mp3");
        Write(".git/config.mp3");

        var files = await EnumerateAsync();

        Assert.Single(files, f => f.RelativePath == "keep.mp3");
    }

    [Fact]
    public async Task Applies_extension_exclusions_from_options()
    {
        Write("song.mp3");
        Write("partial.tmp");

        var files = await EnumerateAsync(new ScanFilterOptions([".tmp"], []));

        Assert.Single(files, f => f.RelativePath == "song.mp3");
    }

    [Fact]
    public async Task Empty_root_yields_no_files()
    {
        var files = await EnumerateAsync();

        Assert.Empty(files);
    }

    [Fact]
    public async Task Missing_root_throws_DirectoryNotFoundException()
    {
        Directory.Delete(_root);

        await Assert.ThrowsAsync<DirectoryNotFoundException>(
            () => _enumerator.EnumerateAsync(_root, ScanFilterOptions.Default).ToListAsync().AsTask());
    }

    [Fact]
    public async Task Reports_last_write_time_in_utc()
    {
        Write("a.mp3");
        File.SetLastWriteTimeUtc(Path.Combine(_root, "a.mp3"), new DateTime(2026, 3, 4, 5, 6, 7, DateTimeKind.Utc));

        var files = await EnumerateAsync();

        Assert.Equal(new DateTimeOffset(2026, 3, 4, 5, 6, 7, TimeSpan.Zero), files.Single().LastWriteTimeUtc);
    }

    [Fact]
    public async Task Last_write_times_are_quantized_to_whole_milliseconds()
    {
        // SQLite rounds DateTimeOffset on write (up to ~100us loss); the enumerator must
        // pre-quantize to milliseconds so stored and re-discovered values compare equal.
        Write("a.mp3");
        var withSubMs = new DateTime(2026, 3, 4, 5, 6, 7, DateTimeKind.Utc).AddTicks(604);
        File.SetLastWriteTimeUtc(Path.Combine(_root, "a.mp3"), withSubMs);

        var files = await EnumerateAsync();

        var reported = files.Single().LastWriteTimeUtc;
        Assert.Equal(0, reported.Ticks % TimeSpan.TicksPerMillisecond);
        Assert.Equal(new DateTimeOffset(2026, 3, 4, 5, 6, 7, TimeSpan.Zero), reported);
    }
}

/// <summary>SQLite-backed tests for the scanner's index-row repository.</summary>
public sealed class MediaFileRepositoryTests(RepoTests fixture) : IClassFixture<RepoTests>
{
    private async Task<(Library Library, LibrarySource Source, MediaItem Item)> SeedAsync()
    {
        var library = await fixture.SeedLibraryAsync();
        var item = await fixture.SeedItemAsync(library.Id);

        await using var context = fixture.CreateContext();
        var source = new LibrarySource { LibraryId = library.Id, Name = "Main", RootPath = @"C:\Media" };
        context.Sources.Add(source);
        await context.SaveChangesAsync();
        return (library, source, item);
    }

    private static MediaFile NewFile(Guid sourceId, Guid itemId, string relativePath, long size = 10) => new()
    {
        MediaItemId = itemId,
        LibrarySourceId = sourceId,
        RelativePath = relativePath,
        IdentityKey = $"{sourceId:N}/{relativePath.ToLowerInvariant()}",
        Extension = Path.GetExtension(relativePath),
        SizeBytes = size,
        LastWriteTimeUtc = DateTimeOffset.UtcNow,
    };

    [Fact]
    public async Task Add_then_list_returns_rows_for_the_source()
    {
        var (library, source, item) = await SeedAsync();
        var repository = new MediaFileRepository(fixture.CreateContext());
        await repository.AddAsync(NewFile(source.Id, item.Id, "a.mp3"));
        await repository.AddAsync(NewFile(source.Id, item.Id, "sub/b.flac"));

        var rows = await repository.ListBySourceAsync(source.Id);

        Assert.Equal(2, rows.Count);
        Assert.All(rows, f => Assert.StartsWith($"{source.Id:N}/", f.IdentityKey));
    }

    [Fact]
    public async Task Update_persists_changes_to_a_detached_row()
    {
        var (library, source, item) = await SeedAsync();
        var writer = new MediaFileRepository(fixture.CreateContext());
        await writer.AddAsync(NewFile(source.Id, item.Id, "a.mp3", size: 10));

        var reader = new MediaFileRepository(fixture.CreateContext());
        var row = (await reader.ListBySourceAsync(source.Id)).Single();

        row.SizeBytes = 999;
        row.MissingSinceUtc = null;
        await reader.UpdateAsync(row);

        var verify = new MediaFileRepository(fixture.CreateContext());
        var saved = (await verify.ListBySourceAsync(source.Id)).Single();
        Assert.Equal(999, saved.SizeBytes);
    }

    [Fact]
    public async Task MarkMissing_sets_missing_since_for_the_given_rows_only()
    {
        var (library, source, item) = await SeedAsync();
        var repository = new MediaFileRepository(fixture.CreateContext());
        await repository.AddAsync(NewFile(source.Id, item.Id, "a.mp3"));
        await repository.AddAsync(NewFile(source.Id, item.Id, "b.mp3"));

        var reader = new MediaFileRepository(fixture.CreateContext());
        var rows = await reader.ListBySourceAsync(source.Id);
        var first = rows.Single(f => f.RelativePath == "a.mp3");
        var stamp = DateTimeOffset.UtcNow;

        await reader.MarkMissingAsync([first.Id], stamp);

        var verify = new MediaFileRepository(fixture.CreateContext());
        var savedRows = await verify.ListBySourceAsync(source.Id);
        // SQLite round-trips DateTimeOffset at sub-millisecond precision loss; compare within 1 ms.
        var savedMissing = savedRows.Single(f => f.RelativePath == "a.mp3").MissingSinceUtc!.Value;
        Assert.True(
            Math.Abs((savedMissing - stamp).TotalMilliseconds) < 1,
            $"expected ~{stamp:O}, got {savedMissing:O}");
        Assert.Null(savedRows.Single(f => f.RelativePath == "b.mp3").MissingSinceUtc);
    }

    [Fact]
    public async Task Unique_index_rejects_duplicate_identity_keys()
    {
        var (library, source, item) = await SeedAsync();
        var repository = new MediaFileRepository(fixture.CreateContext());
        await repository.AddAsync(NewFile(source.Id, item.Id, "a.mp3"));

        await using var context = fixture.CreateContext();
        context.Files.Add(NewFile(source.Id, item.Id, "a.mp3", size: 55));
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }
}
