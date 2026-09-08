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
/// well-known junk directories (recycler, system volume information, node_modules, …),
/// and any directory name excluded via options. Never follows reparse points (junctions,
/// symlinks) so cycles and external volumes are not traversed. Read-only: nothing on disk
/// is created, modified, or deleted (AGENTS.md rule 11).
/// </summary>
public sealed class FileEnumerator : IFileEnumerator
{
    // Junk/system directories skipped everywhere by name (case-insensitive).
    private static readonly HashSet<string> JunkDirectoryNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "$recycle.bin",
        "system volume information",
        "$windows.~ws",
        "$windows.~bt",
        "windows",
        "program files",
        "program files (x86)",
        "programdata",
        "appdata",
        "node_modules",
        ".git",
        ".svn",
        ".hg",
        "__macosx",
    };

    public async IAsyncEnumerable<DiscoveredFile> EnumerateAsync(
        string rootPath,
        ScanFilterOptions options,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Fail fast on a vanished root: the scan must report an error instead of
        // silently marking every indexed file as missing.
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
                // The file vanished or cannot be stat'ed: skip it, the next scan re-checks.
                continue;
            }

            yield return new DiscoveredFile(
                RelativePath: ToRelativePath(rootPath, file.FullName),
                Extension: extension,
                SizeBytes: info.Length,
                // Quantized to whole milliseconds: SQLite stores DateTimeOffset at coarser
                // precision than NTFS, and without quantization every re-scan would see
                // every file as changed.
                LastWriteTimeUtc: QuantizeToMilliseconds(new DateTimeOffset(info.LastWriteTimeUtc, TimeSpan.Zero)));
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <summary>
    /// Manual stack walk instead of EnumerationOptions-compatible enumerator: gives precise
    /// control over hidden/system skipping, reparse-point avoidance, and access errors.
    /// </summary>
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

            DirectoryInfo directory;
            try
            {
                directory = pending.Pop();
                if (directory.FullName.Length >= 240)
                {
                    continue;
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            {
                continue;
            }

            FileSystemInfo[] entries;
            try
            {
                entries = directory.GetFileSystemInfos();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or DirectoryNotFoundException)
            {
                continue; // Unreadable subtree: skip quietly, do not fail the whole scan.
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

    private static string ToRelativePath(string rootPath, string fullPath)
    {
        var relative = Path.GetRelativePath(rootPath, fullPath);
        return relative.Replace('\\', '/');
    }

    private static DateTimeOffset QuantizeToMilliseconds(DateTimeOffset value) =>
        new(value.UtcTicks - (value.UtcTicks % TimeSpan.TicksPerMillisecond), TimeSpan.Zero);

    private static HashSet<string> NormalizeExtensions(IReadOnlyList<string> extensions) =>
        extensions
            .Select(extension => extension.StartsWith('.') ? extension.ToLowerInvariant() : $".{extension.ToLowerInvariant()}")
            .ToHashSet();
}
