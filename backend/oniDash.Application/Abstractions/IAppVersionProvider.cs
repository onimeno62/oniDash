namespace oniDash.Application.Abstractions;

/// <summary>
/// Provides the running application version. Implemented by the host (API), because only the
/// host knows which assembly actually represents the product shell.
/// </summary>
public interface IAppVersionProvider
{
    /// <summary>Semantic version string of the running application, e.g. <c>0.1.0</c>.</summary>
    string Version { get; }
}
