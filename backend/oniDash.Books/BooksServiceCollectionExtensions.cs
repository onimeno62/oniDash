using Microsoft.Extensions.DependencyInjection;
using oniDash.Application.Media;
using oniDash.Books.Metadata;

namespace oniDash.Books;
public static class BooksServiceCollectionExtensions
{
    public static IServiceCollection AddBooks(this IServiceCollection services)
    {
        services.AddScoped<IBookDocumentMetadataReader, LocalBookDocumentMetadataReader>();
        services.AddScoped<IMediaHandler, BookMediaHandler>();
        return services;
    }
}
