using System;
using System.Collections.Generic;

namespace oniDash.Application.Scanning;

public enum ScanStatus { Running = 0, Completed = 1, Failed = 2, Cancelled = 3 }
public enum ScanPhase { Discovering = 0, Indexing = 1, Reconciling = 2, Done = 3 }

public sealed record DiscoveredFile(string RelativePath, string Extension, long SizeBytes, DateTimeOffset LastWriteTimeUtc);

public sealed record ScanFilterOptions(IReadOnlyList<string> ExcludedExtensions, IReadOnlyList<string> ExcludedDirectoryNames)
{
    public static ScanFilterOptions Default { get; } = new([], []);
}

public sealed record ScanProgressUpdate(ScanPhase Phase, long FilesDiscovered, long FilesProcessed, long FilesIndexed,
    long FilesUpdated, long FilesUnchanged, long FilesMarkedMissing);

public enum ScanDiagnosticSeverity { Warning = 1, Error = 2 }
public sealed record ScanDiagnostic(string RelativePath, ScanDiagnosticSeverity Severity, string Code, string Message);

public sealed record ScanOutcome(long Discovered, long Indexed, long Updated, long Unchanged, long MarkedMissing,
    DateTimeOffset FinishedAtUtc, IReadOnlyList<ScanDiagnostic>? Diagnostics = null);

public sealed record ScanProgress(Guid ScanId, Guid LibraryId, Guid SourceId, string SourceName, ScanStatus Status,
    ScanPhase Phase, long FilesDiscovered, long FilesProcessed, long FilesIndexed, long FilesUpdated,
    long FilesUnchanged, long FilesMarkedMissing, DateTimeOffset StartedAtUtc, DateTimeOffset? CompletedAtUtc, string? Error);
