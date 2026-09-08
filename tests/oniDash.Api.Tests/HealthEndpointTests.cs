using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace oniDash.Api.Tests;

/// <summary>
/// Boots the real API host against an isolated SQLite file per test collection.
/// </summary>
public sealed class OniDashApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath = Path.Combine(
        Path.GetTempPath(),
        $"onidash-tests-{Guid.NewGuid():N}.db");

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureHostConfiguration(config => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            // Foreign Keys=True mirrors the production connection string (cascades + integrity).
            ["ConnectionStrings:OniDash"] = $"Data Source={_databasePath};Foreign Keys=True",
        }));

        return base.CreateHost(builder);
    }

    public string DatabasePath => _databasePath;

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        try
        {
            if (File.Exists(_databasePath))
            {
                File.Delete(_databasePath);
            }
        }
        catch (IOException)
        {
            // Best-effort cleanup; leftover temp files are harmless.
        }
    }
}

public sealed class HealthEndpointTests(OniDashApiFactory factory) : IClassFixture<OniDashApiFactory>
{
    [Fact]
    public async Task Health_endpoint_returns_healthy_report_from_sqlite()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/health");
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
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/health");
        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);

        Assert.Equal("Healthy", document.RootElement.GetProperty("status").GetString());
        Assert.Equal("Ok", document.RootElement.GetProperty("databaseStatus").GetString());
        Assert.True(document.RootElement.TryGetProperty("timestamp", out _));
    }

    [Fact]
    public async Task Startup_applies_migrations_and_creates_the_sqlite_file()
    {
        Assert.True(File.Exists(factory.DatabasePath), "expected the local SQLite database file to exist after startup");
    }

    [Fact]
    public async Task Broken_database_reports_unhealthy_with_service_unavailable()
    {
        using var brokenFactory = new BrokenDatabaseFactory();
        var client = brokenFactory.CreateClient();

        var response = await client.GetAsync("/api/health");
        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("Unhealthy", document.RootElement.GetProperty("status").GetString());
        Assert.Equal("Unavailable", document.RootElement.GetProperty("databaseStatus").GetString());
    }

    /// <summary>
    /// Boots the API against an SQLite path inside a directory that cannot exist, proving the
    /// app stays up and reports degraded health instead of crashing.
    /// </summary>
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

    /// <summary>Mirrors the API contract without coupling tests to EF entities.</summary>
    private sealed record AppHealthReportDto(string Status, string Version, string DatabaseStatus, DateTimeOffset Timestamp);
}
