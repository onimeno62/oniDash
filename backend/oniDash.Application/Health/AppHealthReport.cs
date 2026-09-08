using oniDash.Application.Abstractions;

namespace oniDash.Application.Health;

/// <summary>
/// Overall application health, derived from the state of critical subsystems.
/// </summary>
public enum AppHealthStatus
{
    /// <summary>All critical subsystems responded.</summary>
    Healthy,

    /// <summary>At least one critical subsystem (currently the database) is unavailable.</summary>
    Unhealthy,
}

/// <summary>
/// Transport-shaped health report returned by the API. This is a DTO, not an EF entity.
/// </summary>
/// <param name="Status">Aggregate application status.</param>
/// <param name="Version">Version of the running application.</param>
/// <param name="DatabaseStatus">Outcome of the last database probe.</param>
/// <param name="Timestamp">UTC instant the report was produced.</param>
public sealed record AppHealthReport(
    AppHealthStatus Status,
    string Version,
    DatabaseHealthStatus DatabaseStatus,
    DateTimeOffset Timestamp);
