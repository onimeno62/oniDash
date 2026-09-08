using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace oniDash.Infrastructure.Persistence;

/// <summary>Applies pending EF Core migrations at startup, creating the database when missing.</summary>
public interface IDatabaseInitializer
{
    /// <summary>Brings the database up to the current model. Safe to call once per startup.</summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);
}

public sealed class DatabaseInitializer(OniDashDbContext dbContext, ILogger<DatabaseInitializer> logger)
    : IDatabaseInitializer
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Applying core database migrations");
        await dbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Core database is ready");
    }
}
