using Microsoft.Extensions.DependencyInjection;
using oniDash.Application.Media;
using oniDash.Books.Metadata;
using oniDash.Books.Reader;

namespace oniDash.Books;

public static class BooksServiceCollectionExtensions
{
    public static IServiceCollection AddBooksServices(this IServiceCollection services)
    {
        // Scoped: MediaHandlerRegistry is scoped and materializes handlers eagerly; the
        // document metadata reader parses per-request against the file system.
        services.AddScoped<IBookDocumentMetadataReader, LocalBookDocumentMetadataReader>();
        services.AddScoped<IMediaHandler, BookMediaHandler>();
        services.AddScoped<IBookReaderService, LocalBookReaderService>();
        return services;
    }
}
