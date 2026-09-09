using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using oniDash.Application.Common;
using oniDash.Application.Jobs;

namespace oniDash.Api.Endpoints;

public static class JobEndpoints
{
    public static IEndpointRouteBuilder MapJobEndpoints(this IEndpointRouteBuilder app)
    {
        var jobs = app.MapGroup("/api/jobs").WithTags("Jobs");

        jobs.MapGet("/", (IJobService service, int? limit) =>
            Results.Ok(service.List(limit ?? 50)));

        jobs.MapGet("/{id:guid}", (IJobService service, Guid id) =>
        {
            var job = service.Get(id);
            return job is null
                ? Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Not found", detail: $"Job '{id}' was not found.")
                : Results.Ok(job);
        });

        jobs.MapPost("/{id:guid}/cancel", (IJobService service, Guid id) =>
            service.Get(id) is null
                ? Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Not found", detail: $"Job '{id}' was not found.")
                : Results.Ok(new { cancelled = service.Cancel(id) }));

        return app;
    }
}
