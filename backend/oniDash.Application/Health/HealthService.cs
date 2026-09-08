using oniDash.Application.Abstractions;

namespace oniDash.Application.Health;

/// <summary>
/// Composes an application health report from infrastructure probes. Kept free of endpoint,
/// EF, and JSON concerns so it is unit-testable in isolation.
/// </summary>
public sealed class HealthService(IDatabaseHealthProbe databaseProbe, IAppVersionProvider appVersion) : IHealthService
{
    public async Task<AppHealthReport> GetReportAsync(CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(databaseProbe);
        ArgumentNullException.ThrowIfNull(appVersion);

        var databaseStatus = await databaseProbe.ProbeAsync(cancellationToken).ConfigureAwait(false);
        var status = databaseStatus == DatabaseHealthStatus.Ok
            ? AppHealthStatus.Healthy
            : AppHealthStatus.Unhealthy;

        return new AppHealthReport(
            Status: status,
            Version: appVersion.Version,
            DatabaseStatus: databaseStatus,
            Timestamp: DateTimeOffset.UtcNow);
    }
}
