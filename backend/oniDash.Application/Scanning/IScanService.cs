using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace oniDash.Application.Scanning;

public interface IScanService
{
    Task<ScanOutcome> ScanSourceAsync(
        Guid sourceId,
        Action<ScanProgressUpdate>? onProgress,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Runs scans in the background, one at a time per source, and answers status/cancel/retry
/// queries with thread-safe snapshots. Progress is in memory; the indexed media data is
/// persisted and survives process restarts.
/// </summary>
public interface IScanJobManager
{
    StartScanResult StartScan(Guid libraryId, Guid sourceId, string sourceName);
    ScanProgress? GetScan(Guid scanId);
    bool TryCancel(Guid scanId);
    bool TryRetry(Guid scanId, out Guid retryScanId);
    IReadOnlyList<ScanProgress> ListScans(Guid? sourceId = null, int limit = 20);
}

public sealed record StartScanResult(bool Accepted, Guid ScanId, string? RejectionReason);
