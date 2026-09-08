namespace oniDash.Application.Libraries;

/// <summary>
/// Filesystem knowledge the application layer needs, without depending on IO directly.
/// Implemented by Infrastructure; faked in tests.
/// </summary>
public interface IFileSystemProbe
{
    /// <summary>True when the given path is an existing directory on disk.</summary>
    bool IsExistingDirectory(string path);
}
