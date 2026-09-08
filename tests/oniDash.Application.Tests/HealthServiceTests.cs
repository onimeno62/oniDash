using oniDash.Application.Abstractions;
using oniDash.Application.Health;
using Xunit;

namespace oniDash.Application.Tests;

public sealed class HealthServiceTests
{
    private static readonly DateTimeOffset KnownTimestamp = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private sealed class StubProbe(DatabaseHealthStatus status) : IDatabaseHealthProbe
    {
        public CancellationToken? ReceivedToken { get; private set; }

        public Task<DatabaseHealthStatus> ProbeAsync(CancellationToken cancellationToken = default)
        {
            ReceivedToken = cancellationToken;
            return Task.FromResult(status);
        }
    }

    private sealed class StubVersion(string version) : IAppVersionProvider
    {
        public string Version { get; } = version;
    }

    [Fact]
    public async Task Healthy_database_reports_healthy_application()
    {
        var probe = new StubProbe(DatabaseHealthStatus.Ok);
        var service = new HealthService(probe, new StubVersion("0.1.0"));

        var report = await service.GetReportAsync();

        Assert.Equal(AppHealthStatus.Healthy, report.Status);
        Assert.Equal(DatabaseHealthStatus.Ok, report.DatabaseStatus);
    }

    [Fact]
    public async Task Unavailable_database_reports_unhealthy_application()
    {
        var probe = new StubProbe(DatabaseHealthStatus.Unavailable);
        var service = new HealthService(probe, new StubVersion("0.1.0"));

        var report = await service.GetReportAsync();

        Assert.Equal(AppHealthStatus.Unhealthy, report.Status);
        Assert.Equal(DatabaseHealthStatus.Unavailable, report.DatabaseStatus);
    }

    [Fact]
    public async Task Report_carries_the_application_version()
    {
        var service = new HealthService(
            new StubProbe(DatabaseHealthStatus.Ok),
            new StubVersion("9.9.9"));

        var report = await service.GetReportAsync();

        Assert.Equal("9.9.9", report.Version);
    }

    [Fact]
    public async Task Probe_receives_the_caller_cancellation_token()
    {
        using var cts = new CancellationTokenSource();
        var probe = new StubProbe(DatabaseHealthStatus.Ok);
        var service = new HealthService(probe, new StubVersion("0.1.0"));

        await service.GetReportAsync(cts.Token);

        Assert.True(cts.Token == probe.ReceivedToken, "expected the probe to receive the caller's cancellation token");
    }
}
