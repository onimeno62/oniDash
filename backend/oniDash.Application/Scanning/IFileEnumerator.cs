using System.Collections.Generic;
using System.Threading;

namespace oniDash.Application.Scanning;

/// <summary>
/// Streams files below a root directory. Implemented by Infrastructure (real filesystem)
/// and faked in tests. Must be cancellable and must never follow into hidden/system/junk
/// directories.
/// </summary>
public interface IFileEnumerator
{
    IAsyncEnumerable<DiscoveredFile> EnumerateAsync(
        string rootPath,
        ScanFilterOptions options,
        CancellationToken cancellationToken = default);
}
