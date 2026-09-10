using Microsoft.Extensions.DependencyInjection;
using oniDash.Application.Media;

namespace oniDash.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IMediaMetadataNormalizer, MediaMetadataNormalizer>();
        services.AddScoped<Scanning.IScanService, Scanning.ScanService>();
        services.AddSingleton<Scanning.IScanJobManager, Scanning.ScanJobManager>();
        services.AddScoped<Health.IHealthService, Health.HealthService>();
        return services;
    }
}
