using System;

namespace oniDash.Application.Common;

/// <summary>Common lifecycle for long-running local operations.</summary>
public enum JobStatus
{
    Queued = 0,
    Running = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4,
}

/// <summary>
/// Canonical job snapshot shared by scanner, enrichment, artwork, search and maintenance
/// operations. Implementations may add operation-specific progress fields separately.
/// </summary>
public sealed record Job(
    Guid Id,
    string Type,
    JobStatus Status,
    int ProgressPercent,
    string? CurrentItem,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    int RetryCount,
    string? Error,
    string? CorrelationId);
