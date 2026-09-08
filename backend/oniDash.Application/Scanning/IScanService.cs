using System;
using System.Threading;
using System.Threading.Tasks;

namespace oniDash.Application.Scanning;

public interface IScanService
{
    /// <summary>
    /// Performs one full scan pass over the source: discover files, upsert index rows,
    /// mark disappeared files as missing (never delete them), and stamp the source's
    /// LastScannedAtUtc on success. Throws NotFoundException for an unknown source and
    /// OperationCanceledException on cancellation.
    /// </summary>
    Task<ScanOutcome> ScanSourceAsync(
        Guid sourceId,
        Action<ScanProgressUpdate>? onProgress,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Runs scans in the background, one at a time per source, and answers status/cancel
/// queries with thread-safe snapshots. Progress is kept in memory (single-user,
/// local-first app) and is not persisted across restarts.
/// </summary>
public interface IScanJobManager
{
    /// <summary>Starts a scan for the source, unless one is already running for it.</summary>
    StartScanResult StartScan(Guid libraryId, Guid sourceId, string sourceName);

    ScanProgress? GetScan(Guid scanId);

    /// <summary>Requests cancellation; false when the scan is unknown or already finished.</summary>
    bool TryCancel(Guid scanId);

    IReadOnlyList<ScanProgress> ListScans(Guid? sourceId = null, int limit = 20);
}

public sealed record StartScanResult(bool Accepted, Guid ScanId, string? RejectionReason);
