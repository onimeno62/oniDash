using oniDash.Application.Health;

namespace oniDash.Application.Health;

/// <summary>Read-side use case: inspect overall application health.</summary>
public interface IHealthService
{
    /// <summary>Builds a health report. Returns a report even when subsystems are down.</summary>
    Task<AppHealthReport> GetReportAsync(CancellationToken cancellationToken = default);
}
