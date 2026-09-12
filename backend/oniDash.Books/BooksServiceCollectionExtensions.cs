using Microsoft.Extensions.DependencyInjection;
using oniDash.Books.Metadata;
using oniDash.Books.Reader;
using oniDash.Core.Contracts;

namespace oniDash.Books;

public static class BooksServiceCollectionExtensions
{
    public static IServiceCollection AddBooksServices(this IServiceCollection services)
    {
        services.AddSingleton<IMediaHandler, BookMediaHandler>();
        services.AddSingleton<IBookReaderService, LocalBookReaderService>();
        return services;
    }
}
