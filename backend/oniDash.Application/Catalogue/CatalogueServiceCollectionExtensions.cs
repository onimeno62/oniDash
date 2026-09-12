using Microsoft.Extensions.DependencyInjection;

namespace oniDash.Application.Catalogue;

public static class CatalogueServiceCollectionExtensions
{
    public static IServiceCollection AddCatalogueContracts(this IServiceCollection services)
    {
        services.AddSingleton<ISeriesCatalogue, EmptySeriesCatalogue>();
        services.AddSingleton<IBookReader, UnsupportedBookReader>();
        return services;
    }

    private sealed class EmptySeriesCatalogue : ISeriesCatalogue
    {
        public Task<IReadOnlyList<SeriesSummary>> ListAsync(Guid? libraryId = null, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<SeriesSummary>>([]);
    }

    private sealed class UnsupportedBookReader : IBookReader
    {
        public Task<Stream?> OpenAsync(Guid bookId, CancellationToken cancellationToken = default) => Task.FromResult<Stream?>(null);
    }
}
