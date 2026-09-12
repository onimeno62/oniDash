using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using oniDash.Application.Catalogue;
using oniDash.Manga.Persistence;

namespace oniDash.Manga;

public static class MangaServiceCollectionExtensions
{
    public static IServiceCollection AddManga(this IServiceCollection services, IConfiguration configuration)
    {
        var configured = configuration.GetConnectionString("OniDash");
        var directory = configuration["OniDash:DataDirectory"];
        if (string.IsNullOrWhiteSpace(directory)) directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "oniDash");
        Directory.CreateDirectory(directory);
        var connection = string.IsNullOrWhiteSpace(configured) ? new SqliteConnectionStringBuilder { DataSource = Path.Combine(directory, "onidash.db"), ForeignKeys = true }.ToString() : configured;
        services.AddDbContext<MangaDbContext>(options => options.UseSqlite(connection));
        services.AddScoped<MangaDatabaseInitializer>(); services.AddScoped<IMangaSourceAdapter, LocalMangaSource>(); services.AddScoped<IMangaChapterSource>(x => x.GetRequiredService<IMangaSourceAdapter>());
        services.AddScoped<IMangaPluginManager, MangaPluginManager>(); services.AddScoped<IMediaPluginManager>(x => x.GetRequiredService<IMangaPluginManager>());
        services.AddScoped<IMangaChapterCatalogue, MangaChapterCatalogue>(); services.AddScoped<IMangaBookmarkStore, MangaBookmarkStore>();
        services.AddScoped<MangaDownloadQueue>(); services.AddScoped<IMangaDownloadQueue>(x => x.GetRequiredService<MangaDownloadQueue>()); services.AddScoped<IMangaDownloadProcessor>(x => x.GetRequiredService<MangaDownloadQueue>());
        services.AddScoped<IMediaReader, MangaReader>();
        return services;
    }
}
