using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace oniDash.Music.Persistence;

/// <summary>
/// Brings the music catalogue schema up to date. Independent from the core initializer:
/// a music migration failure must not take core library functions down (health reports
/// it separately through the API's startup logs).
/// </summary>
public sealed class MusicDatabaseInitializer(MusicDbContext dbContext, ILogger<MusicDatabaseInitializer> logger)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Music catalogue schema is up to date");
    }
}
