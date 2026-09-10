using oniDash.Application.Health;
using Xunit;

namespace oniDash.Application.Tests;

public sealed class LibraryHealthServiceTests
{
    [Fact]
    public async Task Returns_probe_report_without_mutating_it()
    {
        var expected = new LibraryHealthReport(1, 2, 3, 4, 1, 2, DateTimeOffset.UtcNow);
        var service = new LibraryHealthService(new StubProbe(expected));
        var actual = await service.GetReportAsync();
        Assert.Equal(expected, actual);
    }

    private sealed class StubProbe(LibraryHealthReport report) : ILibraryHealthProbe
    {
        public Task<LibraryHealthReport> GetReportAsync(CancellationToken cancellationToken = default) => Task.FromResult(report);
    }
}
