using Microsoft.EntityFrameworkCore;
using oniDash.Application.Health;
using oniDash.Infrastructure.Persistence;

namespace oniDash.Infrastructure.Health;

public sealed class SqliteLibraryHealthProbe(OniDashDbContext dbContext) : ILibraryHealthProbe
{
    public async Task<LibraryHealthReport> GetReportAsync(CancellationToken cancellationToken = default)
    {
        var report = new LibraryHealthReport(
            await dbContext.Libraries.CountAsync(cancellationToken).ConfigureAwait(false),
            await dbContext.Sources.CountAsync(cancellationToken).ConfigureAwait(false),
            await dbContext.MediaItems.CountAsync(cancellationToken).ConfigureAwait(false),
            await dbContext.Files.CountAsync(cancellationToken).ConfigureAwait(false),
            await dbContext.Files.CountAsync(file => file.MissingSinceUtc != null, cancellationToken).ConfigureAwait(false),
            await dbContext.Artwork.CountAsync(cancellationToken).ConfigureAwait(false),
            DateTimeOffset.UtcNow);
        return report;
    }
}
