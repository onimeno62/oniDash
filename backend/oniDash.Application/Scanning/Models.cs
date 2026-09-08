using System;

namespace oniDash.Application.Scanning;

/// <summary>Lifecycle of a scan run.</summary>
public enum ScanStatus
{
    Running = 0,
    Completed = 1,
    Failed = 2,
    Cancelled = 3,
}

/// <summary>What the scanner is currently doing within a run.</summary>
public enum ScanPhase
{
    Discovering = 0,
    Indexing = 1,
    Reconciling = 2,
    Done = 3,
}

/// <summary>
/// One file found on disk during enumeration. Paths are relative to the source root with
/// '/' separators, matching MediaFile.RelativePath.
/// </summary>
public sealed record DiscoveredFile(
    string RelativePath,
    string Extension,
    long SizeBytes,
    DateTimeOffset LastWriteTimeUtc);

/// <summary>
/// Filter knobs for enumeration. Hidden/system entries and junk directories are always
/// skipped by the enumerator; these options add exclusions on top.
/// </summary>
public sealed record ScanFilterOptions(
    IReadOnlyList<string> ExcludedExtensions,
    IReadOnlyList<string> ExcludedDirectoryNames)
{
    public static ScanFilterOptions Default { get; } = new([], []);
}

/// <summary>Incremental progress emitted by the scanner while a scan runs.</summary>
public sealed record ScanProgressUpdate(
    ScanPhase Phase,
    long FilesDiscovered,
    long FilesProcessed,
    long FilesIndexed,
    long FilesUpdated,
    long FilesUnchanged,
    long FilesMarkedMissing);

/// <summary>Final result of one completed scan pass over a source.</summary>
public sealed record ScanOutcome(
    long Discovered,
    long Indexed,
    long Updated,
    long Unchanged,
    long MarkedMissing,
    DateTimeOffset FinishedAtUtc);

/// <summary>Immutable snapshot of a scan run, safe to hand to API callers.</summary>
public sealed record ScanProgress(
    Guid ScanId,
    Guid LibraryId,
    Guid SourceId,
    string SourceName,
    ScanStatus Status,
    ScanPhase Phase,
    long FilesDiscovered,
    long FilesProcessed,
    long FilesIndexed,
    long FilesUpdated,
    long FilesUnchanged,
    long FilesMarkedMissing,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    string? Error);
