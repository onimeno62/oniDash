using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using oniDash.Application.Libraries;

namespace oniDash.Api.Endpoints;

/// <summary>Library, source, and media-item endpoints — the core library surface.</summary>
public static class LibraryEndpoints
{
    public static IEndpointRouteBuilder MapLibraryEndpoints(this IEndpointRouteBuilder app)
    {
        var libraries = app.MapGroup("/api/libraries").WithTags("Libraries");

        libraries.MapGet("/", (ILibraryService service, CancellationToken ct) =>
            ApiRequestHandler.RunAsync(async () =>
                Results.Ok(await service.ListAsync(ct).ConfigureAwait(false))));

        libraries.MapPost("/", (ILibraryService service, CreateLibraryRequest request, CancellationToken ct) =>
            ApiRequestHandler.RunAsync(async () =>
            {
                var dto = await service.CreateAsync(request, ct).ConfigureAwait(false);
                return Results.Created($"/api/libraries/{dto.Id}", dto);
            }));

        libraries.MapGet("/{id:guid}", (ILibraryService service, Guid id, CancellationToken ct) =>
            ApiRequestHandler.RunAsync(async () =>
                Results.Ok(await service.GetAsync(id, ct).ConfigureAwait(false))));

        libraries.MapPut("/{id:guid}", (ILibraryService service, Guid id, UpdateLibraryRequest request, CancellationToken ct) =>
            ApiRequestHandler.RunAsync(async () =>
                Results.Ok(await service.RenameAsync(id, request, ct).ConfigureAwait(false))));

        libraries.MapDelete("/{id:guid}", (ILibraryService service, Guid id, CancellationToken ct) =>
            ApiRequestHandler.RunAsync(async () =>
            {
                await service.DeleteAsync(id, ct).ConfigureAwait(false);
                return Results.NoContent();
            }));

        var sources = libraries.MapGroup("/{libraryId:guid}/sources").WithTags("Sources");

        sources.MapGet("/", (ISourceService service, Guid libraryId, CancellationToken ct) =>
            ApiRequestHandler.RunAsync(async () =>
                Results.Ok(await service.ListAsync(libraryId, ct).ConfigureAwait(false))));

        sources.MapPost("/", (ISourceService service, Guid libraryId, CreateSourceRequest request, CancellationToken ct) =>
            ApiRequestHandler.RunAsync(async () =>
            {
                var dto = await service.AddAsync(libraryId, request, ct).ConfigureAwait(false);
                return Results.Created($"/api/libraries/{libraryId}/sources/{dto.Id}", dto);
            }));

        sources.MapDelete("/{sourceId:guid}", (ISourceService service, Guid sourceId, CancellationToken ct) =>
            ApiRequestHandler.RunAsync(async () =>
            {
                await service.RemoveAsync(sourceId, ct).ConfigureAwait(false);
                return Results.NoContent();
            }));

        var items = libraries.MapGroup("/{libraryId:guid}/items").WithTags("MediaItems");

        items.MapGet("/", (
            IMediaItemService service,
            Guid libraryId,
            int? page,
            int? pageSize,
            CancellationToken ct) =>
            ApiRequestHandler.RunAsync(async () =>
                Results.Ok(await service
                    .ListByLibraryAsync(libraryId, page ?? 1, pageSize ?? 50, ct)
                    .ConfigureAwait(false))));

        return app;
    }
}
