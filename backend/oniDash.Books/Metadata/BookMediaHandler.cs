using oniDash.Application.Media;
using oniDash.Books.Metadata;
using oniDash.Core.Domain;

namespace oniDash.Books;

/// <summary>Adapts EPUB, PDF and comic archive metadata to the canonical media contract.</summary>
public sealed class BookMediaHandler(IBookDocumentMetadataReader reader) : IMediaHandler
{
    private static readonly IReadOnlySet<MediaType> Types = new HashSet<MediaType> { MediaType.Book, MediaType.Document, MediaType.Comic };
    private static readonly IReadOnlySet<string> Extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".epub", ".pdf", ".mobi", ".azw", ".azw3", ".fb2", ".cbz", ".cbr", ".cb7" };
    public string Id => "books.document";
    public IReadOnlySet<MediaType> SupportedTypes => Types;
    public bool CanHandle(MediaDescriptor descriptor) => Types.Contains(descriptor.MediaType) && Extensions.Contains(NormalizeExtension(descriptor.Extension));
    public async Task<MediaInspectionResult> InspectAsync(MediaDescriptor descriptor, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); if (!CanHandle(descriptor)) throw new InvalidOperationException($"Handler '{Id}' cannot handle '{descriptor.Extension}' as {descriptor.MediaType}.");
        var metadata = await reader.ReadAsync(descriptor.AbsolutePath, descriptor.Extension, cancellationToken).ConfigureAwait(false); var fields = new Dictionary<string, string>(metadata.Fields, StringComparer.OrdinalIgnoreCase); if (metadata.PageCount is int pages && pages > 0) fields["pageCount"] = pages.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return new MediaInspectionResult(string.IsNullOrWhiteSpace(metadata.Title) ? null : metadata.Title.Trim(), null, null, null, fields, metadata.Artwork ?? Array.Empty<ArtworkCandidate>(), MetadataProvenance.Local("local-document-metadata", metadata.Fields.Count == 0 ? 0.25d : 1d));
    }
    private static string NormalizeExtension(string extension) => string.IsNullOrWhiteSpace(extension) ? string.Empty : extension.StartsWith('.') ? extension : "." + extension;
}
