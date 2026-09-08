using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using oniDash.Application.Common;
using oniDash.Application.Libraries;
using oniDash.Application.Scanning;

namespace oniDash.Api.Endpoints;

/// <summary>
/// Scan lifecycle endpoints: start a background scan for a source, poll its progress,
/// request cancellation, and list recent runs.
/// </summary>
public static class ScanEndpoints
{
    public static IEndpointRouteBuilder MapScanEndpoints(this IEndpointRouteBuilder app)
    {
        var scans = app.MapGroup("/api/sources/{sourceId:guid}/scans").WithTags("Scans");

        scans.MapPost("/", (
            IScanJobManager manager,
            ILibrarySourceRepository sources,
            Guid sourceId,
            CancellationToken ct) =>
            ApiRequestHandler.RunAsync(async () =>
            {
                var source = await sources.GetByIdAsync(sourceId, ct).ConfigureAwait(false)
                    ?? throw new NotFoundException("Library source", sourceId);

                var result = manager.StartScan(source.LibraryId, sourceId, source.Name);
                if (!result.Accepted)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status409Conflict,
                        title: "Conflict",
                        detail: result.RejectionReason,
                        extensions: new Dictionary<string, object?> { ["scanId"] = result.ScanId });
                }

                return Results.Accepted($"/api/scans/{result.ScanId}", manager.GetScan(result.ScanId));
            }));

        app.MapGet("/api/scans/{scanId:guid}", (IScanJobManager manager, Guid scanId) =>
        {
            var progress = manager.GetScan(scanId);
            return progress is null
                ? Results.Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Not found",
                    detail: $"Scan '{scanId}' was not found.")
                : Results.Ok(progress);
        });

        app.MapPost("/api/scans/{scanId:guid}/cancel", (IScanJobManager manager, Guid scanId) =>
            Results.Ok(new { cancelled = manager.TryCancel(scanId) }));

        app.MapGet("/api/scans", (IScanJobManager manager, Guid? sourceId, int? limit) =>
            Results.Ok(manager.ListScans(sourceId, limit ?? 20)));

        return app;
    }
}
