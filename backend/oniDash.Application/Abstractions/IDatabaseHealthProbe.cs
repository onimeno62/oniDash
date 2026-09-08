namespace oniDash.Application.Abstractions;

/// <summary>
/// Outcome of probing the configured persistence layer.
/// </summary>
public enum DatabaseHealthStatus
{
    /// <summary>The database accepted a trivial query.</summary>
    Ok,

    /// <summary>The database could not be reached or failed the probe.</summary>
    Unavailable,
}

/// <summary>
/// Probes persistence-layer availability. Implemented by Infrastructure; consumed by
/// application services so health reporting never depends on a concrete provider.
/// </summary>
public interface IDatabaseHealthProbe
{
    /// <summary>Runs a cheap, cancellation-aware probe. Never throws for provider failures.</summary>
    Task<DatabaseHealthStatus> ProbeAsync(CancellationToken cancellationToken = default);
}
