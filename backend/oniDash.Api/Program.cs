using System.Text.Json.Serialization;
using oniDash.Api.Endpoints;
using oniDash.Api.Services;
using oniDash.Application;
using oniDash.Application.Abstractions;
using oniDash.Application.Catalogue;
using oniDash.Application.Health;
using oniDash.Books;
using oniDash.Infrastructure;
using oniDash.Infrastructure.Persistence;
using oniDash.Manga;
using oniDash.Manga.Persistence;
using oniDash.Music;
using oniDash.Music.Persistence;
using oniDash.Movies;
using oniDash.Movies.Persistence;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy.WithOrigins("http://localhost:5173", "http://127.0.0.1:5173").AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddRouting(options => options.LowercaseUrls = true).ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddInfrastructure(builder.Configuration); builder.Services.AddApplication(); builder.Services.AddCatalogueContracts(); builder.Services.AddMusic(builder.Configuration); builder.Services.AddMovies(builder.Configuration); builder.Services.AddBooks(); builder.Services.AddManga(builder.Configuration);
builder.Services.AddSingleton<IAppVersionProvider, AssemblyAppVersionProvider>(); builder.Services.AddScoped<IHealthService, HealthService>();
var app = builder.Build(); app.UseCors();
app.MapHealthEndpoints(); app.MapCatalogueContractEndpoints(); app.MapLibraryEndpoints(); app.MapTagEndpoints(); app.MapCollectionEndpoints(); app.MapScanEndpoints(); app.MapSearchEndpoints(); app.MapMusicEndpoints(); app.MapMovieEndpoints(); app.MapBooksEndpoints(); app.MapMangaPluginEndpoints();
var webRoot = app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot"); var spaIndex = Path.Combine(webRoot, "index.html");
if (File.Exists(spaIndex)) { app.UseDefaultFiles(); app.UseStaticFiles(); app.MapFallbackToFile("index.html"); }
try { using var scope = app.Services.CreateScope(); await scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>().InitializeAsync().ConfigureAwait(false); } catch (Exception ex) { app.Logger.LogError(ex, "Database initialization failed"); }
try { using var scope = app.Services.CreateScope(); await scope.ServiceProvider.GetRequiredService<MusicDatabaseInitializer>().InitializeAsync().ConfigureAwait(false); } catch (Exception ex) { app.Logger.LogError(ex, "Music catalogue initialization failed"); }
try { using var scope = app.Services.CreateScope(); await scope.ServiceProvider.GetRequiredService<MoviesDatabaseInitializer>().InitializeAsync().ConfigureAwait(false); } catch (Exception ex) { app.Logger.LogError(ex, "Movie catalogue initialization failed"); }
try { using var scope = app.Services.CreateScope(); await scope.ServiceProvider.GetRequiredService<MangaDatabaseInitializer>().InitializeAsync().ConfigureAwait(false); } catch (Exception ex) { app.Logger.LogError(ex, "Manga catalogue initialization failed"); }
app.Run();
public partial class Program;
