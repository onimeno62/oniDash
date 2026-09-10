using System.Text.Json.Serialization;
using oniDash.Api.Endpoints;
using oniDash.Api.Services;
using oniDash.Application;
using oniDash.Application.Abstractions;
using oniDash.Application.Health;
using oniDash.Infrastructure;
using oniDash.Infrastructure.Persistence;
using oniDash.Music;
using oniDash.Music.Persistence;
using oniDash.Movies;
using oniDash.Movies.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
    .AllowAnyHeader()
    .AllowAnyMethod()));

builder.Services
    .AddRouting(options => options.LowercaseUrls = true)
    .ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(
        new JsonStringEnumConverter()));

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddMusic(builder.Configuration);
builder.Services.AddMovies(builder.Configuration);

builder.Services.AddSingleton<IAppVersionProvider, AssemblyAppVersionProvider>();
builder.Services.AddScoped<IHealthService, HealthService>();

var app = builder.Build();
app.UseCors();

app.MapHealthEndpoints();
app.MapLibraryEndpoints();
app.MapTagEndpoints();
app.MapCollectionEndpoints();
app.MapScanEndpoints();
app.MapSearchEndpoints();
app.MapMusicEndpoints();
app.MapMovieEndpoints();
app.MapBooksEndpoints();
app.MapMangaEndpoints();

// API endpoints are registered before the SPA fallback so an unmatched /api/* request
// cannot be satisfied by index.html. This keeps API clients on JSON/error semantics and
// prevents HTML from masquerading as a successful API response.
var webRoot = app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot");
var spaIndex = Path.Combine(webRoot, "index.html");
app.Map("/api/{**path}", (HttpContext context) =>
{
    context.Response.StatusCode = StatusCodes.Status404NotFound;
    context.Response.ContentType = "application/json";
    return context.Response.WriteAsJsonAsync(new { code = "not_found", message = $"No API endpoint matches '{context.Request.Path}'." });
});
if (File.Exists(spaIndex))
{
    app.UseDefaultFiles();
    app.UseStaticFiles();
    app.MapFallbackToFile("index.html");
}

try
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>().InitializeAsync().ConfigureAwait(false);
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "Database initialization failed; the API will report unhealthy database status");
}

try
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<MusicDatabaseInitializer>().InitializeAsync().ConfigureAwait(false);
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "Music catalogue initialization failed; music endpoints may be unavailable");
}

try
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<MoviesDatabaseInitializer>().InitializeAsync().ConfigureAwait(false);
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "Movie catalogue initialization failed; movie endpoints may be unavailable");
}

app.Run();
public partial class Program;
