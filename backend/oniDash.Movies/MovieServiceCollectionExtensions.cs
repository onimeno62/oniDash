using System;
using System.IO;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using oniDash.Application.Scanning;
using oniDash.Movies.Cataloging;
using oniDash.Movies.Endpoints;
using oniDash.Movies.Persistence;
using oniDash.Movies.Probing;

namespace oniDash.Movies;

/// <summary>Composition root of the movie catalogue plugin.</summary>
public static class MovieServiceCollectionExtensions
{
    public static IServiceCollection AddMovies(this IServiceCollection services, IConfiguration configuration)
    {
        // Same database file as the core, its own migration history table (see
        // MoviesDbContext). Foreign keys are enabled to match the core connection so
        // cross-catalogue cascades fire at the SQLite level.
        var connectionString = ResolveConnectionString(configuration);
        services.AddDbContext<MoviesDbContext>(options => options.UseSqlite(connectionString));

        services.AddScoped<MovieCatalogService>();
        services.AddScoped<IVideoProbeReader, FfprobeVideoProbeReader>();
        services.AddScoped<IVideoArtworkReader, TagLibVideoArtworkReader>();
        services.AddScoped<MovieReindexService>();
        services.AddScoped<MoviesDatabaseInitializer>();
        services.AddScoped<IVideoFileLocator, VideoFileLocator>();
        services.AddScoped<IIndexedMediaHandler, MovieIndexedMediaHandler>();

        return services;
    }

    public static IEndpointRouteBuilder MapMovieEndpoints(this IEndpointRouteBuilder app) =>
        MovieEndpoints.MapMovieEndpoints(app);

    /// <summary>
    /// Mirrors the core connection resolution: an explicit connection string wins, then
    /// <c>OniDash:DataDirectory</c>, then the default <c>%LOCALAPPDATA%\oniDash</c>.
    /// </summary>
    private static string ResolveConnectionString(IConfiguration configuration)
    {
        var configured = configuration.GetConnectionString("OniDash");
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return EnsureForeignKeys(configured);
        }

        var dataDirectory = configuration["OniDash:DataDirectory"];
        var directory = string.IsNullOrWhiteSpace(dataDirectory)
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "oniDash")
            : dataDirectory;

        Directory.CreateDirectory(directory);

        return EnsureForeignKeys(new SqliteConnectionStringBuilder
        {
            DataSource = Path.Combine(directory, "onidash.db"),
            Mode = SqliteOpenMode.ReadWriteCreate,
            Pooling = true,
        }.ToString());
    }

    private static string EnsureForeignKeys(string connectionString)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString);
        builder.ForeignKeys = true;
        return builder.ToString();
    }
}
