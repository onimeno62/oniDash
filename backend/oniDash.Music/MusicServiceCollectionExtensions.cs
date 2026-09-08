using System;
using System.IO;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using oniDash.Application.Scanning;
using oniDash.Music.Cataloging;
using oniDash.Music.Endpoints;
using oniDash.Music.Persistence;
using oniDash.Music.Tagging;

namespace oniDash.Music;

/// <summary>Composition root of the music catalogue plugin.</summary>
public static class MusicServiceCollectionExtensions
{
    public static IServiceCollection AddMusic(this IServiceCollection services, IConfiguration configuration)
    {
        // Same database file as the core, its own migration history table (see
        // MusicDbContext). Foreign keys are enabled to match the core connection so
        // cross-catalogue cascades fire at the SQLite level.
        var connectionString = ResolveConnectionString(configuration);
        services.AddDbContext<MusicDbContext>(options => options.UseSqlite(connectionString));

        services.AddScoped<MusicCatalogService>();
        services.AddScoped<IAudioTagReader, TagLibAudioTagReader>();
        services.AddScoped<MusicReindexService>();
        services.AddScoped<MusicDatabaseInitializer>();
        services.AddScoped<IMediaFileLocator, AudioFileLocator>();
        services.AddScoped<IIndexedMediaHandler, MusicIndexedMediaHandler>();

        return services;
    }

    public static IEndpointRouteBuilder MapMusicEndpoints(this IEndpointRouteBuilder app) =>
        MusicEndpoints.MapMusicEndpoints(app);

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
