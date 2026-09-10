using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using oniDash.Application.Health;

namespace oniDash.Api.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/health").WithTags("Health");
        group.MapGet("/", async (IHealthService healthService, CancellationToken cancellationToken) =>
            {
                var report = await healthService.GetReportAsync(cancellationToken).ConfigureAwait(false);
                return report.Status == AppHealthStatus.Healthy ? Results.Ok(report) : Results.Json(report, statusCode: StatusCodes.Status503ServiceUnavailable);
            })
            .WithName("GetHealth")
            .Produces<AppHealthReport>(StatusCodes.Status200OK)
            .Produces<AppHealthReport>(StatusCodes.Status503ServiceUnavailable);
        group.MapGet("/library", async (ILibraryHealthService healthService, CancellationToken cancellationToken) =>
            Results.Ok(await healthService.GetReportAsync(cancellationToken).ConfigureAwait(false)))
            .WithName("GetLibraryHealth")
            .WithSummary("Actionable local library counts, including missing files and artwork coverage.")
            .Produces<LibraryHealthReport>(StatusCodes.Status200OK);
        return app;
    }
}
