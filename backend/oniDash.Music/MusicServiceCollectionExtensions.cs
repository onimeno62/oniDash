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

public static class MusicServiceCollectionExtensions
{
    public static IServiceCollection AddMusic(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = ResolveConnectionString(configuration);
        services.AddDbContext<MusicDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<MusicCatalogService>(); services.AddScoped<IAudioTagReader, TagLibAudioTagReader>(); services.AddScoped<IAudioTagWriter, TagLibAudioTagWriter>(); services.AddScoped<MusicReindexService>(); services.AddScoped<MusicDatabaseInitializer>(); services.AddScoped<IMediaFileLocator, AudioFileLocator>(); services.AddScoped<IIndexedMediaHandler, MusicIndexedMediaHandler>();
        return services;
    }
    public static IEndpointRouteBuilder MapMusicEndpoints(this IEndpointRouteBuilder app) => MusicEndpoints.MapMusicEndpoints(app).MapMusicEngagementEndpoints().MapMusicPlaylistEndpoints().MapMusicMetadataEndpoints().MapMusicInsightsEndpoints();
    private static string ResolveConnectionString(IConfiguration configuration) { var configured = configuration.GetConnectionString("OniDash"); if (!string.IsNullOrWhiteSpace(configured)) return EnsureForeignKeys(configured); var dataDirectory = configuration["OniDash:DataDirectory"]; var directory = string.IsNullOrWhiteSpace(dataDirectory) ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "oniDash") : dataDirectory; Directory.CreateDirectory(directory); return EnsureForeignKeys(new SqliteConnectionStringBuilder { DataSource = Path.Combine(directory, "onidash.db"), Mode = SqliteOpenMode.ReadWriteCreate, Pooling = true }.ToString()); }
    private static string EnsureForeignKeys(string connectionString) { var builder = new SqliteConnectionStringBuilder(connectionString) { ForeignKeys = true }; return builder.ToString(); }
}
