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

// Local-only API by default (docs/SECURITY.md); the SPA is served from the same origin.
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

// Serve the built SPA when its assets are deployed to wwwroot (production / packaging).
// During frontend development the Vite dev server proxies /api instead.
var webRoot = app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot");
var spaIndex = Path.Combine(webRoot, "index.html");
if (File.Exists(spaIndex))
{
    app.UseDefaultFiles();
    app.UseStaticFiles();
    app.MapFallbackToFile("index.html");
}

app.MapHealthEndpoints();
app.MapLibraryEndpoints();
app.MapTagEndpoints();
app.MapCollectionEndpoints();
app.MapScanEndpoints();
app.MapSearchEndpoints();
app.MapMusicEndpoints();
app.MapMovieEndpoints();

// Bring the local database up to date without failing startup; health reports degradation.
try
{
    using (var scope = app.Services.CreateScope())
    {
        await scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>()
            .InitializeAsync()
            .ConfigureAwait(false);
    }
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "Database initialization failed; the API will report unhealthy database status");
}

// The music plugin owns its schema and migrates independently; its failure must not
// take core library functions down.
try
{
    using (var scope = app.Services.CreateScope())
    {
        await scope.ServiceProvider.GetRequiredService<MusicDatabaseInitializer>()
            .InitializeAsync()
            .ConfigureAwait(false);
    }
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "Music catalogue initialization failed; music endpoints may be unavailable");
}

// The movie plugin owns its schema and migrates independently; its failure must not
// take core (or music) functions down.
try
{
    using (var scope = app.Services.CreateScope())
    {
        await scope.ServiceProvider.GetRequiredService<MoviesDatabaseInitializer>()
            .InitializeAsync()
            .ConfigureAwait(false);
    }
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "Movie catalogue initialization failed; movie endpoints may be unavailable");
}

app.Run();

// Exposed for WebApplicationFactory-based integration tests.
public partial class Program;
