using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace oniDash.Api.Tests;

public class BooksHealthTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public BooksHealthTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetBooksHealth_ReturnsSuccessAndReport()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/books/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("totalBooks", content);
        Assert.Contains("missingCovers", content);
        Assert.Contains("missingFiles", content);
        Assert.Contains("missingMetadata", content);
    }
}
