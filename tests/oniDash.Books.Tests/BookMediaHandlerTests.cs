using oniDash.Application.Media;
using oniDash.Books;
using oniDash.Books.Metadata;
using oniDash.Core.Domain;
using Xunit;

namespace oniDash.Books.Tests;
public sealed class BookMediaHandlerTests
{
    [Fact]
    public async Task Inspect_maps_document_metadata_and_page_count()
    {
        var reader = new FakeReader(new BookDocumentMetadata("A Book", new Dictionary<string, string> { ["author"] = "An Author", ["format"] = "pdf" }, 120));
        var result = await new BookMediaHandler(reader).InspectAsync(Descriptor(MediaType.Document, ".pdf"));
        Assert.Equal("A Book", result.Title); Assert.Equal("An Author", result.Metadata["author"]); Assert.Equal("120", result.Metadata["pageCount"]); Assert.Equal("local-document-metadata", result.Provenance.Source); Assert.False(result.Provenance.IsExternal);
    }
    [Theory]
    [InlineData(MediaType.Book, ".epub")]
    [InlineData(MediaType.Document, ".PDF")]
    [InlineData(MediaType.Comic, ".cbz")]
    public void CanHandle_accepts_supported_catalogue_types(MediaType type, string extension) => Assert.True(new BookMediaHandler(new FakeReader(new BookDocumentMetadata(null, new Dictionary<string, string>(), null))).CanHandle(Descriptor(type, extension)));
    [Fact]
    public async Task Inspect_rejects_unknown_extension() => await Assert.ThrowsAsync<InvalidOperationException>(() => new BookMediaHandler(new FakeReader(new BookDocumentMetadata(null, new Dictionary<string, string>(), null))).InspectAsync(Descriptor(MediaType.Book, ".zip")));
    private static MediaDescriptor Descriptor(MediaType type, string extension) => new(Guid.NewGuid(), Guid.NewGuid(), type, "library/item" + extension, extension, 1, DateTimeOffset.UtcNow);
    private sealed class FakeReader(BookDocumentMetadata result) : IBookDocumentMetadataReader { public Task<BookDocumentMetadata> ReadAsync(string absolutePath, string extension, CancellationToken cancellationToken = default) => Task.FromResult(result); }
}
