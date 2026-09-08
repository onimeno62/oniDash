using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using oniDash.Application.Abstractions;
using oniDash.Infrastructure.Persistence;

namespace oniDash.Infrastructure.Persistence;

/// <summary>
/// Verifies the SQLite database answers a trivial query. Never throws for provider failures —
/// an unreachable database is a health result, not a host crash.
/// </summary>
public sealed class SqliteDatabaseHealthProbe(OniDashDbContext dbContext, ILogger<SqliteDatabaseHealthProbe> logger)
    : IDatabaseHealthProbe
{
    public async Task<DatabaseHealthStatus> ProbeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var connection = dbContext.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            }

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1;";
            await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            return DatabaseHealthStatus.Ok;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Database health probe failed");
            return DatabaseHealthStatus.Unavailable;
        }
    }
}
