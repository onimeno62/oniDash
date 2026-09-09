using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using oniDash.Application.Scanning;

namespace oniDash.Application.Jobs;

public interface IJobService
{
    JobSnapshot? Get(Guid id);
    IReadOnlyList<JobSnapshot> List(int limit = 50);
    bool Cancel(Guid id);
}

/// <summary>Bridges the existing scanner runner into the canonical background-job API.</summary>
public sealed class JobService(IScanJobManager scans) : IJobService
{
    private readonly IScanJobManager _scans = scans;

    public JobSnapshot? Get(Guid id) => _scans.GetScan(id) is { } scan ? ToSnapshot(scan) : null;

    public IReadOnlyList<JobSnapshot> List(int limit = 50) =>
        _scans.ListScans(limit: Math.Clamp(limit, 1, 200)).Select(ToSnapshot).ToArray();

    public bool Cancel(Guid id) => _scans.TryCancel(id);

    private static JobSnapshot ToSnapshot(ScanProgress scan) => new(
        scan.ScanId,
        JobKind.Scan,
        scan.Status switch
        {
            ScanStatus.Running => JobStatus.Running,
            ScanStatus.Completed => JobStatus.Completed,
            ScanStatus.Cancelled => JobStatus.Cancelled,
            _ => JobStatus.Failed,
        },
        $"Scan {scan.SourceName}",
        scan.FilesProcessed,
        scan.FilesDiscovered,
        scan.StartedAtUtc,
        scan.StartedAtUtc,
        scan.CompletedAtUtc,
        scan.Error);
}
