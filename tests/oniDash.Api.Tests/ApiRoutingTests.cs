using System.Net;
using Xunit;

namespace oniDash.Api.Tests;

public sealed class ApiRoutingTests(OniDashApiFactory factory) : IClassFixture<OniDashApiFactory>
{
    [Fact]
    public async Task Unmatched_api_route_does_not_return_spa_html()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/route-that-does-not-exist");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotEqual("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.DoesNotContain("<html", body, StringComparison.OrdinalIgnoreCase);
    }
}
