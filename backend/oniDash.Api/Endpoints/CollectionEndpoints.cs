using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using oniDash.Application.Libraries;

namespace oniDash.Api.Endpoints;

/// <summary>Collection management and collection-content endpoints.</summary>
public static class CollectionEndpoints
{
    public static IEndpointRouteBuilder MapCollectionEndpoints(this IEndpointRouteBuilder app)
    {
        var collections = app.MapGroup("/api/libraries/{libraryId:guid}/collections").WithTags("Collections");

        collections.MapGet("/", (ICollectionService service, Guid libraryId, CancellationToken ct) =>
            ApiRequestHandler.RunAsync(async () =>
                Results.Ok(await service.ListAsync(libraryId, ct).ConfigureAwait(false))));

        collections.MapPost("/", (ICollectionService service, Guid libraryId, CreateCollectionRequest request, CancellationToken ct) =>
            ApiRequestHandler.RunAsync(async () =>
            {
                var dto = await service.CreateAsync(libraryId, request, ct).ConfigureAwait(false);
                return Results.Created($"/api/collections/{dto.Id}", dto);
            }));

        app.MapDelete("/api/collections/{collectionId:guid}", (ICollectionService service, Guid collectionId, CancellationToken ct) =>
            ApiRequestHandler.RunAsync(async () =>
            {
                await service.DeleteAsync(collectionId, ct).ConfigureAwait(false);
                return Results.NoContent();
            }));

        var collectionItems = app.MapGroup("/api/collections/{collectionId:guid}/items").WithTags("Collections");

        collectionItems.MapGet("/", (
            ICollectionService service,
            Guid collectionId,
            int? page,
            int? pageSize,
            CancellationToken ct) =>
            ApiRequestHandler.RunAsync(async () =>
                Results.Ok(await service
                    .ListItemsAsync(collectionId, page ?? 1, pageSize ?? 50, ct)
                    .ConfigureAwait(false))));

        collectionItems.MapPost("/", (ICollectionService service, Guid collectionId, AddCollectionItemRequest request, CancellationToken ct) =>
            ApiRequestHandler.RunAsync(async () =>
            {
                await service.AddItemAsync(collectionId, request, ct).ConfigureAwait(false);
                return Results.NoContent();
            }));

        collectionItems.MapDelete("/{mediaItemId:guid}", (
            ICollectionService service,
            Guid collectionId,
            Guid mediaItemId,
            CancellationToken ct) =>
            ApiRequestHandler.RunAsync(async () =>
            {
                await service.RemoveItemAsync(collectionId, mediaItemId, ct).ConfigureAwait(false);
                return Results.NoContent();
            }));

        return app;
    }
}
