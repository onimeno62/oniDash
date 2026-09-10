using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using oniDash.Application.Abstractions;
using oniDash.Application.Health;
using oniDash.Application.Libraries;
using oniDash.Application.Media;
using oniDash.Application.Scanning;
using oniDash.Application.Search;
using oniDash.Infrastructure.Health;
using oniDash.Infrastructure.Persistence;
using oniDash.Infrastructure.Repositories;
using oniDash.Infrastructure.Scanning;
using oniDash.Infrastructure.Search;

namespace oniDash.Infrastructure;

/// <summary>Registers persistence-layer services (EF Core + SQLite).</summary>
public static class InfrastructureServiceCollectionExtensions
{
    private const string ConnectionStringName = "OniDash";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = ResolveConnectionString(configuration);

        services.AddDbContext<OniDashDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<IDatabaseHealthProbe, SqliteDatabaseHealthProbe>();
        services.AddScoped<IDatabaseInitializer, DatabaseInitializer>();
        services.AddScoped<ILibraryHealthProbe, SqliteLibraryHealthProbe>();

        services.AddSingleton<IFileSystemProbe, FileSystemProbe>();
        services.AddScoped<ILibraryRepository, LibraryRepository>();
        services.AddScoped<ILibrarySourceRepository, LibrarySourceRepository>();
        services.AddScoped<IMediaItemRepository, MediaItemRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<ICollectionRepository, CollectionRepository>();
        services.AddScoped<IMediaFileRepository, MediaFileRepository>();
        services.AddSingleton<IFileEnumerator, FileEnumerator>();
        services.AddSingleton<IMediaTypeDetector, MediaTypeDetector>();
        services.AddSingleton<MediaHandlerRegistry>();
        services.AddScoped<ISearchService, FtsSearchService>();

        return services;
    }

    /// <summary>
    /// Local-first default: <c>%LOCALAPPDATA%/oniDash/onidash.db</c>. An explicit
    /// <c>ConnectionStrings:OniDash</c> or <c>OniDash:DataDirectory</c> overrides it, which is
    /// how tests isolate their database.
    /// </summary>
    private static string ResolveConnectionString(IConfiguration configuration)
    {
        var configured = configuration.GetConnectionString(ConnectionStringName);
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        var dataDirectory = configuration["OniDash:DataDirectory"];
        var directory = string.IsNullOrWhiteSpace(dataDirectory)
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "oniDash")
            : dataDirectory;

        Directory.CreateDirectory(directory);

        return new SqliteConnectionStringBuilder
        {
            DataSource = Path.Combine(directory, "onidash.db"),
            Mode = SqliteOpenMode.ReadWriteCreate,
            Pooling = true,
            // The model declares ON DELETE CASCADE relationships; SQLite only enforces
            // foreign keys when asked, so cascade deletes and integrity checks actually work.
            ForeignKeys = true,
        }.ToString();
    }
}
