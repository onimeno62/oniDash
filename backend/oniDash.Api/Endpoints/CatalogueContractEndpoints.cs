using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using oniDash.Application.Catalogue;

namespace oniDash.Api.Endpoints;

public static class CatalogueContractEndpoints
{
    public static IEndpointRouteBuilder MapCatalogueContractEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/catalogue/series", async (ISeriesCatalogue catalogue, Guid? libraryId, CancellationToken ct) => Results.Ok(await catalogue.ListAsync(libraryId, ct)))
            .WithTags("Catalogue contracts");
        return app;
    }
}
