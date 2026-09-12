using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using oniDash.Infrastructure.Windows;

namespace oniDash.Api.Endpoints;

public static class PlatformEndpoints
{
    public static IEndpointRouteBuilder MapPlatformEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/platform/windows").WithTags("Windows Platform");

        group.MapGet("/config", (IWindowsProductService windows) => Results.Ok(windows.GetConfig()));

        group.MapPut("/config", (IWindowsProductService windows, WindowsAppConfig config) =>
        {
            windows.UpdateConfig(config);
            return Results.Ok(windows.GetConfig());
        });

        group.MapGet("/tray", (IWindowsProductService windows) => Results.Ok(new { visible = ((WindowsProductService)windows).IsTrayVisible }));

        group.MapPost("/tray/visibility", async (IWindowsProductService windows, VisibilityRequest request, CancellationToken ct) =>
        {
            await windows.SetVisibleAsync(request.Visible, ct);
            return Results.Ok(new { visible = request.Visible });
        });

        group.MapPost("/notifications", async (IWindowsProductService windows, NotificationRequest request, CancellationToken ct) =>
        {
            await windows.ShowNotificationAsync(request.Title, request.Message, ct);
            return Results.Accepted();
        });

        group.MapGet("/notifications/history", (IWindowsProductService windows) => Results.Ok(windows.GetNotificationHistory()));

        group.MapGet("/associations", (IWindowsProductService windows) => Results.Ok(new { extensions = ((WindowsProductService)windows).RegisteredExtensions }));

        group.MapPost("/associations/register", async (IWindowsProductService windows, ExtensionsRequest request, CancellationToken ct) =>
        {
            await windows.RegisterAsync(request.Extensions, ct);
            return Results.Ok(new { extensions = ((WindowsProductService)windows).RegisteredExtensions });
        });

        group.MapPost("/associations/unregister", async (IWindowsProductService windows, ExtensionsRequest request, CancellationToken ct) =>
        {
            await windows.UnregisterAsync(request.Extensions, ct);
            return Results.Ok(new { extensions = ((WindowsProductService)windows).RegisteredExtensions });
        });

        group.MapPost("/mediakeys", async (IWindowsProductService windows, MediaKeyRequest request, CancellationToken ct) =>
        {
            await windows.HandleMediaKeyAsync(request.Action, ct);
            return Results.Ok(new { handled = true, action = request.Action });
        });

        group.MapGet("/backup/export", async (IWindowsProductService windows, CancellationToken ct) =>
        {
            var ms = new MemoryStream();
            await windows.ExportAsync(ms, ct);
            ms.Position = 0;
            return Results.File(ms, "application/zip", $"onidash-backup-{DateTime.UtcNow:yyyyMMddHHmmss}.zip");
        });

        group.MapPost("/backup/import", async (IWindowsProductService windows, HttpRequest request, CancellationToken ct) =>
        {
            if (!request.HasFormContentType || request.Form.Files.Count == 0)
            {
                return Results.BadRequest(new { error = "Zip file required in form upload." });
            }

            var file = request.Form.Files[0];
            await using var stream = file.OpenReadStream();
            await windows.ImportAsync(stream, ct);
            return Results.Ok(new { imported = true, size = file.Length });
        });

        return app;
    }

    public sealed record VisibilityRequest(bool Visible);
    public sealed record NotificationRequest(string Title, string Message);
    public sealed record ExtensionsRequest(IReadOnlyCollection<string> Extensions);
    public sealed record MediaKeyRequest(string Action);
}
