using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace oniDash.Api.Tests;

public class DashboardEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public DashboardEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("/api/dashboard/continue")]
    [InlineData("/api/dashboard/recently-added")]
    [InlineData("/api/dashboard/recently-played")]
    [InlineData("/api/dashboard/favorites")]
    [InlineData("/api/dashboard/activity")]
    [InlineData("/api/dashboard/recommendations")]
    public async Task DashboardEndpoints_ReturnSuccess(string url)
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.NotNull(content);
    }
}
