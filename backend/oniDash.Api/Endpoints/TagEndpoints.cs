using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using oniDash.Application.Libraries;

namespace oniDash.Api.Endpoints;

/// <summary>Tag management and tag-to-item assignment endpoints.</summary>
public static class TagEndpoints
{
    public static IEndpointRouteBuilder MapTagEndpoints(this IEndpointRouteBuilder app)
    {
        var tags = app.MapGroup("/api/libraries/{libraryId:guid}/tags").WithTags("Tags");

        tags.MapGet("/", (ITagService service, Guid libraryId, CancellationToken ct) =>
            ApiRequestHandler.RunAsync(async () =>
                Results.Ok(await service.ListAsync(libraryId, ct).ConfigureAwait(false))));

        tags.MapPost("/", (ITagService service, Guid libraryId, CreateTagRequest request, CancellationToken ct) =>
            ApiRequestHandler.RunAsync(async () =>
            {
                var dto = await service.CreateAsync(libraryId, request, ct).ConfigureAwait(false);
                return Results.Created($"/api/tags/{dto.Id}", dto);
            }));

        app.MapDelete("/api/tags/{tagId:guid}", (ITagService service, Guid tagId, CancellationToken ct) =>
            ApiRequestHandler.RunAsync(async () =>
            {
                await service.DeleteAsync(tagId, ct).ConfigureAwait(false);
                return Results.NoContent();
            }));

        var itemTags = app.MapGroup("/api/media-items/{mediaItemId:guid}/tags").WithTags("Tags");

        itemTags.MapPost("/", (ITagService service, Guid mediaItemId, AssignTagRequest request, CancellationToken ct) =>
            ApiRequestHandler.RunAsync(async () =>
            {
                await service.AssignToItemAsync(mediaItemId, request, ct).ConfigureAwait(false);
                return Results.NoContent();
            }));

        itemTags.MapDelete("/{tagId:guid}", (ITagService service, Guid mediaItemId, Guid tagId, CancellationToken ct) =>
            ApiRequestHandler.RunAsync(async () =>
            {
                await service.UnassignFromItemAsync(mediaItemId, tagId, ct).ConfigureAwait(false);
                return Results.NoContent();
            }));

        return app;
    }
}
