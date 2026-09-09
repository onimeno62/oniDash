using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace oniDash.Api.Tests;

public sealed class OniDashApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"onidash-tests-{Guid.NewGuid():N}.db");
    private readonly string _mediaRoot = Path.Combine(Path.GetTempPath(), $"onidash-media-{Guid.NewGuid():N}");

    protected override IHost CreateHost(IHostBuilder builder)
    {
        Directory.CreateDirectory(_mediaRoot);
        builder.UseEnvironment("Testing");
        builder.ConfigureHostConfiguration(config => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:OniDash"] = $"Data Source={_databasePath};Foreign Keys=True",
        }));
        return base.CreateHost(builder);
    }

    public string DatabasePath => _databasePath;
    public string MediaRoot => _mediaRoot;

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        try { if (File.Exists(_databasePath)) File.Delete(_databasePath); } catch (IOException) { }
        try { if (Directory.Exists(_mediaRoot)) Directory.Delete(_mediaRoot, recursive: true); } catch (IOException) { }
    }
}

public sealed class HealthEndpointTests(OniDashApiFactory factory) : IClassFixture<OniDashApiFactory>
{
    [Fact]
    public async Task Health_endpoint_returns_healthy_report_from_sqlite()
    {
        var response = await factory.CreateClient().GetAsync("/api/health");
        var report = await response.Content.ReadFromJsonAsync<AppHealthReportDto>();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(report);
        Assert.Equal("Healthy", report.Status);
        Assert.Equal("Ok", report.DatabaseStatus);
        Assert.False(string.IsNullOrWhiteSpace(report.Version));
    }

    [Fact]
    public async Task Health_endpoint_serializes_enums_and_camel_case_properties()
    {
        var response = await factory.CreateClient().GetAsync("/api/health");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Healthy", document.RootElement.GetProperty("status").GetString());
        Assert.Equal("Ok", document.RootElement.GetProperty("databaseStatus").GetString());
        Assert.True(document.RootElement.TryGetProperty("timestamp", out _));
    }

    [Fact]
    public void Test_factory_provisions_isolated_sqlite_and_media_roots()
    {
        Assert.True(Directory.Exists(factory.MediaRoot));
        Assert.True(factory.MediaRoot.StartsWith(Path.GetTempPath(), StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Startup_applies_migrations_and_creates_the_sqlite_file()
    {
        _ = factory.CreateClient();
        Assert.True(File.Exists(factory.DatabasePath));
    }

    [Fact]
    public async Task Broken_database_reports_unhealthy_with_service_unavailable()
    {
        using var brokenFactory = new BrokenDatabaseFactory();
        var response = await brokenFactory.CreateClient().GetAsync("/api/health");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("Unhealthy", document.RootElement.GetProperty("status").GetString());
        Assert.Equal("Unavailable", document.RootElement.GetProperty("databaseStatus").GetString());
    }

    private sealed class BrokenDatabaseFactory : WebApplicationFactory<Program>
    {
        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureHostConfiguration(config => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:OniDash"] = $"Data Source={Path.Combine(Path.GetTempPath(), $"no-such-dir-{Guid.NewGuid():N}", "x.db")}",
            }));
            return base.CreateHost(builder);
        }
    }

    private sealed record AppHealthReportDto(string Status, string Version, string DatabaseStatus, DateTimeOffset Timestamp);
}
