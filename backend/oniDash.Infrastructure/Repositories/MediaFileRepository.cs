using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using oniDash.Application.Libraries;
using oniDash.Core.Domain;
using oniDash.Infrastructure.Persistence;

namespace oniDash.Infrastructure.Repositories;

/// <summary>
/// Index-row persistence for the scanner. Adds/updates save the tracked row; marking
/// missing is a bulk update; listing is read-only. Rows are never deleted by scans.
/// </summary>
public sealed class MediaFileRepository(OniDashDbContext dbContext) : IMediaFileRepository
{
    public async Task<IReadOnlyList<MediaFile>> ListBySourceAsync(Guid sourceId, CancellationToken cancellationToken = default) =>
        await dbContext.Files
            .AsNoTracking()
            .Where(f => f.LibrarySourceId == sourceId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<MediaFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.Files
            .AsNoTracking()
            .SingleOrDefaultAsync(f => f.Id == id, cancellationToken)
            .ConfigureAwait(false);

    public async Task AddAsync(MediaFile file, CancellationToken cancellationToken = default)
    {
        await dbContext.Files.AddAsync(file, cancellationToken).ConfigureAwait(false);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(MediaFile file, CancellationToken cancellationToken = default)
    {
        // Re-attach the detached row (ListBySourceAsync is AsNoTracking).
        dbContext.Files.Attach(file);
        dbContext.Entry(file).State = EntityState.Modified;
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task MarkMissingAsync(
        IReadOnlyCollection<Guid> fileIds,
        DateTimeOffset missingSinceUtc,
        CancellationToken cancellationToken = default)
    {
        await dbContext.Files
            .Where(f => fileIds.Contains(f.Id))
            .ExecuteUpdateAsync(
                updates => updates
                    .SetProperty(f => f.MissingSinceUtc, missingSinceUtc),
                cancellationToken)
            .ConfigureAwait(false);
    }
}
