using System;
using System.Threading;
using System.Threading.Tasks;

namespace oniDash.Application.Health;

public sealed record LibraryHealthReport(
    int LibraryCount,
    int SourceCount,
    int MediaItemCount,
    int MediaFileCount,
    int MissingFileCount,
    int ArtworkCount,
    DateTimeOffset GeneratedAtUtc);

public interface ILibraryHealthProbe
{
    Task<LibraryHealthReport> GetReportAsync(CancellationToken cancellationToken = default);
}

public interface ILibraryHealthService
{
    Task<LibraryHealthReport> GetReportAsync(CancellationToken cancellationToken = default);
}

public sealed class LibraryHealthService(ILibraryHealthProbe probe) : ILibraryHealthService
{
    public Task<LibraryHealthReport> GetReportAsync(CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(probe);
        return probe.GetReportAsync(cancellationToken);
    }
}
