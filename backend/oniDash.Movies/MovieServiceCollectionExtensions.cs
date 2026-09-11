using System;
using System.IO;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using oniDash.Application.Catalogue;
using oniDash.Application.Media;
using oniDash.Application.Scanning;
using oniDash.Movies.Cataloging;
using oniDash.Movies.Endpoints;
using oniDash.Movies.Metadata;
using oniDash.Movies.Persistence;
using oniDash.Movies.Probing;

namespace oniDash.Movies;

public static class MovieServiceCollectionExtensions
{
    public static IServiceCollection AddMovies(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = ResolveConnectionString(configuration);
        services.AddDbContext<MoviesDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<MovieCatalogService>();
        services.AddScoped<IVideoProbeReader, FfprobeVideoProbeReader>();
        services.AddScoped<IVideoArtworkReader, TagLibVideoArtworkReader>();
        services.AddScoped<IMediaHandler, VideoMediaHandler>();
        services.AddScoped<MovieReindexService>();
        services.AddScoped<MoviesDatabaseInitializer>();
        services.AddScoped<IVideoFileLocator, VideoFileLocator>();
        services.AddScoped<IIndexedMediaHandler, MovieIndexedMediaHandler>();
        services.AddScoped<IAnimeSuggestionResolver, NullAnimeSuggestionResolver>();
        services.AddScoped<IAnimeMetadataProvider, OptionalAnimeMetadataProvider>();
        return services;
    }

    public static IEndpointRouteBuilder MapMovieEndpoints(this IEndpointRouteBuilder app) => MovieEndpoints.MapMovieEndpoints(app);

    private static string ResolveConnectionString(IConfiguration configuration)
    {
        var configured = configuration.GetConnectionString("OniDash");
        if (!string.IsNullOrWhiteSpace(configured)) return EnsureForeignKeys(configured);
        var configuredDirectory = configuration["OniDash:DataDirectory"];
        var directory = string.IsNullOrWhiteSpace(configuredDirectory)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "oniDash")
            : configuredDirectory;
        Directory.CreateDirectory(directory);
        return EnsureForeignKeys(new SqliteConnectionStringBuilder { DataSource = Path.Combine(directory, "onidash.db"), Mode = SqliteOpenMode.ReadWriteCreate, Pooling = true }.ToString());
    }

    private static string EnsureForeignKeys(string connectionString)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString) { ForeignKeys = true };
        return builder.ToString();
    }
}
