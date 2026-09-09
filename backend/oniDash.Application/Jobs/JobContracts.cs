using System;
using System.Collections.Generic;

namespace oniDash.Application.Jobs;

/// <summary>Canonical background-work taxonomy shared by scanners and future catalogues.</summary>
public enum JobKind
{
    Scan = 1,
    Metadata = 2,
    Artwork = 3,
    SearchRebuild = 4,
    LibraryMaintenance = 5,
    Import = 6,
    Export = 7,
}

public enum JobStatus
{
    Queued = 1,
    Running = 2,
    Completed = 3,
    Failed = 4,
    Cancelled = 5,
}

/// <summary>Transport-neutral snapshot for observable background work.</summary>
public sealed record JobSnapshot(
    Guid Id,
    JobKind Kind,
    JobStatus Status,
    string Title,
    long Completed,
    long Total,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    string? Error,
    string? ParentId = null)
{
    public double? Percent => Total <= 0 ? null : Math.Clamp((double)Completed / Total * 100d, 0d, 100d);
}

public sealed record JobPage(IReadOnlyList<JobSnapshot> Items, int TotalCount, int Limit, int Offset);
