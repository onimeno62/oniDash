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
        services.AddDbContext<MangaDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("MangaConnection") ?? "Data Source=manga.db"));
        services.AddScoped<MangaDatabaseInitializer>();
        services.AddScoped<IMangaSourceAdapter, LocalMangaSource>();
        services.AddScoped<IMangaPluginManager, MangaPluginManager>();
        services.AddScoped<IMangaChapterCatalogue, MangaChapterCatalogue>();
        services.AddScoped<IMangaBookmarkStore, MangaBookmarkStore>();
        services.AddScoped<MangaDownloadQueue>();
        services.AddScoped<IMangaDownloadQueue>(sp => sp.GetRequiredService<MangaDownloadQueue>());
        services.AddScoped<IMangaDownloadProcessor>(sp => sp.GetRequiredService<MangaDownloadQueue>());
        services.AddScoped<IMediaReader, MangaReader>();
        services.AddScoped<IMangaUpdateService, MangaUpdateService>();
        services.AddScoped<IMangaSyncService, MangaSyncService>();
        return services;
    }
}
