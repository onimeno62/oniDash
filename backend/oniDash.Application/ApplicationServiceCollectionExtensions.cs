using Microsoft.Extensions.DependencyInjection;
using oniDash.Application.Catalogue;
using oniDash.Application.Health;
using oniDash.Application.Jobs;
using oniDash.Application.Libraries;
using oniDash.Application.Media;
using oniDash.Application.Scanning;

namespace oniDash.Application;

/// <summary>Registers application-layer services (use cases).</summary>
public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ILibraryService, LibraryService>();
        services.AddScoped<ISourceService, SourceService>();
        services.AddScoped<IMediaItemService, MediaItemService>();
        services.AddScoped<ITagService, TagService>();
        services.AddScoped<ICollectionService, CollectionService>();
        services.AddScoped<IJobService, JobService>();
        services.AddScoped<IMediaItemResolver, PlaceholderMediaItemResolver>();
        services.AddScoped<IScanService, ScanService>();
        services.AddSingleton<IScanJobManager, ScanJobManager>();
        services.AddScoped<IMediaMetadataNormalizer, MediaMetadataNormalizer>();
        services.AddScoped<ILibraryHealthService, LibraryHealthService>();
        return services;
    }
}
