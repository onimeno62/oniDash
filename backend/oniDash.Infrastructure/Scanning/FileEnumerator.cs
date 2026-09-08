using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using oniDash.Application.Scanning;

namespace oniDash.Infrastructure.Scanning;

/// <summary>
/// Streams files below a root directory. Skips hidden and system entries (by attribute),
/// well-known junk directories, and any directory name excluded via options. Never follows
/// reparse points (junctions, symlinks) so cycles and external volumes are not traversed.
/// Read-only: nothing on disk is created, modified, or deleted.
/// </summary>
public sealed class FileEnumerator : IFileEnumerator
{
    private static readonly HashSet<string> JunkDirectoryNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "$recycle.bin", "system volume information", "$windows.~ws", "$windows.~bt",
        "windows", "program files", "program files (x86)", "programdata", "appdata",
        "node_modules", ".git", ".svn", ".hg", "__macosx",
    };

    public async IAsyncEnumerable<DiscoveredFile> EnumerateAsync(
        string rootPath,
        ScanFilterOptions options,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(rootPath))
        {
            throw new DirectoryNotFoundException($"Source folder '{rootPath}' does not exist.");
        }

        var excludedExtensions = NormalizeExtensions(options.ExcludedExtensions);
        var excludedDirectories = new HashSet<string>(options.ExcludedDirectoryNames, StringComparer.OrdinalIgnoreCase);

        foreach (var file in EnumerateFilesSafe(rootPath, excludedDirectories, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var extension = Path.GetExtension(file.Name);
            if (excludedExtensions.Contains(extension.ToLowerInvariant()))
            {
                continue;
            }

            FileInfo info;
            try
            {
                info = new FileInfo(file.FullName);
                info.Refresh();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // An individual file can disappear during a scan; the next scan will
                // reconcile it. Directory-level failures are not swallowed below.
                continue;
            }

            yield return new DiscoveredFile(
                RelativePath: ToRelativePath(rootPath, file.FullName),
                Extension: extension,
                SizeBytes: info.Length,
                LastWriteTimeUtc: QuantizeToMilliseconds(new DateTimeOffset(info.LastWriteTimeUtc, TimeSpan.Zero)));
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    private static IEnumerable<FileInfo> EnumerateFilesSafe(
        string rootPath,
        HashSet<string> excludedDirectories,
        CancellationToken cancellationToken)
    {
        var pending = new Stack<DirectoryInfo>();
        pending.Push(new DirectoryInfo(rootPath));

        while (pending.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var directory = pending.Pop();
            FileSystemInfo[] entries;
            try
            {
                entries = directory.GetFileSystemInfos();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or DirectoryNotFoundException)
            {
                // Do not silently omit a subtree. ScanService must fail before
                // reconciliation, otherwise valid indexed files could be marked missing.
                throw new IOException($"Could not fully enumerate source directory '{directory.FullName}'.", ex);
            }

            foreach (var entry in entries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (entry.Attributes.HasFlag(FileAttributes.Hidden)
                    || entry.Attributes.HasFlag(FileAttributes.System)
                    || entry.Attributes.HasFlag(FileAttributes.ReparsePoint))
                {
                    continue;
                }

                if (entry is DirectoryInfo sub)
                {
                    if (JunkDirectoryNames.Contains(sub.Name) || excludedDirectories.Contains(sub.Name))
                    {
                        continue;
                    }

                    pending.Push(sub);
                }
                else if (entry is FileInfo file)
                {
                    yield return file;
                }
            }
        }
    }

    private static string ToRelativePath(string rootPath, string fullPath) =>
        Path.GetRelativePath(rootPath, fullPath).Replace('\\', '/');

    private static DateTimeOffset QuantizeToMilliseconds(DateTimeOffset value) =>
        new(value.UtcTicks - (value.UtcTicks % TimeSpan.TicksPerMillisecond), TimeSpan.Zero);

    private static HashSet<string> NormalizeExtensions(IReadOnlyList<string> extensions) =>
        extensions.Select(extension => extension.StartsWith('.')
                ? extension.ToLowerInvariant()
                : $".{extension.ToLowerInvariant()}")
            .ToHashSet();
}
