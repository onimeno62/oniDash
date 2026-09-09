using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using oniDash.Manga.Endpoints;

namespace oniDash.Manga;

public static class MangaServiceCollectionExtensions
{
    public static IServiceCollection AddManga(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<SuwayomiOptions>()
            .Bind(configuration.GetSection(SuwayomiOptions.SectionName));

        services.AddHttpClient<SuwayomiClient>();
        return services;
    }

    public static IEndpointRouteBuilder MapMangaPlugin(this IEndpointRouteBuilder app)
        => app.MapMangaEndpoints();
}
