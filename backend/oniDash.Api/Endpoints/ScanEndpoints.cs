using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using oniDash.Application.Common;
using oniDash.Application.Jobs;
using oniDash.Application.Libraries;
using oniDash.Application.Scanning;

namespace oniDash.Api.Endpoints;

/// <summary>Scan lifecycle and canonical background-job status endpoints.</summary>
public static class ScanEndpoints
{
    public static IEndpointRouteBuilder MapScanEndpoints(this IEndpointRouteBuilder app)
    {
        var scans = app.MapGroup("/api/sources/{sourceId:guid}/scans").WithTags("Scans");
        scans.MapPost("/", (IScanJobManager manager, ILibrarySourceRepository sources, Guid sourceId, CancellationToken ct) =>
            ApiRequestHandler.RunAsync(async () =>
            {
                var source = await sources.GetByIdAsync(sourceId, ct).ConfigureAwait(false)
                    ?? throw new NotFoundException("Library source", sourceId);
                var result = manager.StartScan(source.LibraryId, sourceId, source.Name);
                if (!result.Accepted)
                    return Results.Problem(statusCode: StatusCodes.Status409Conflict, title: "Conflict", detail: result.RejectionReason,
                        extensions: new Dictionary<string, object?> { ["scanId"] = result.ScanId });
                return Results.Accepted($"/api/jobs/{result.ScanId}", manager.GetScan(result.ScanId));
            }));

        app.MapGet("/api/scans/{scanId:guid}", (IScanJobManager manager, Guid scanId) =>
        {
            var progress = manager.GetScan(scanId);
            return progress is null
                ? Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Not found", detail: $"Scan '{scanId}' was not found.")
                : Results.Ok(progress);
        });
        app.MapPost("/api/scans/{scanId:guid}/cancel", (IScanJobManager manager, Guid scanId) =>
            Results.Ok(new { cancelled = manager.TryCancel(scanId) }));
        app.MapPost("/api/scans/{scanId:guid}/retry", (IScanJobManager manager, Guid scanId) =>
        {
            if (!manager.TryRetry(scanId, out var retryId))
                return Results.Problem(statusCode: StatusCodes.Status409Conflict, title: "Cannot retry", detail: "The scan is running, unknown, or its source already has an active scan.");
            return Results.Accepted($"/api/jobs/{retryId}", new { scanId = retryId });
        });
        app.MapGet("/api/scans", (IScanJobManager manager, Guid? sourceId, int? limit) =>
            Results.Ok(manager.ListScans(sourceId, Math.Clamp(limit ?? 20, 1, 200))));

        app.MapGet("/api/jobs", (IJobService service, int? limit) => Results.Ok(service.List(limit ?? 50)));
        app.MapGet("/api/jobs/{id:guid}", (IJobService service, Guid id) =>
        {
            var job = service.Get(id);
            return job is null
                ? Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Not found", detail: $"Job '{id}' was not found.")
                : Results.Ok(job);
        });
        app.MapPost("/api/jobs/{id:guid}/cancel", (IJobService service, Guid id) =>
            service.Get(id) is null
                ? Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Not found", detail: $"Job '{id}' was not found.")
                : Results.Ok(new { cancelled = service.Cancel(id) }));
        app.MapPost("/api/jobs/{id:guid}/retry", (IJobService service, IScanJobManager scans, Guid id) =>
        {
            if (service.Get(id) is null) return Results.NotFound();
            return scans.TryRetry(id, out var retryId)
                ? Results.Accepted($"/api/jobs/{retryId}", new { jobId = retryId })
                : Results.Conflict(new { error = "Job cannot be retried while running or without a terminal scan state." });
        });
        return app;
    }
}
