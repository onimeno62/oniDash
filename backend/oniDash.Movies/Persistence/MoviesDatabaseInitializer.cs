using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace oniDash.Movies.Persistence;

/// <summary>
/// Brings the movie catalogue schema up to date. Independent from the core initializer:
/// a movie migration failure must not take core library functions down (health reports
/// degradation through the API's startup logs).
/// </summary>
public sealed class MoviesDatabaseInitializer(MoviesDbContext dbContext, ILogger<MoviesDatabaseInitializer> logger)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Movie catalogue schema is up to date");
    }
}
