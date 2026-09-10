using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using oniDash.Application.Media;

namespace oniDash.Books.Metadata;

public sealed record BookDocumentMetadata(string? Title, IReadOnlyDictionary<string, string> Fields, int? PageCount, IReadOnlyList<ArtworkCandidate>? Artwork = null);
public interface IBookDocumentMetadataReader
{
    Task<BookDocumentMetadata> ReadAsync(string absolutePath, string extension, CancellationToken cancellationToken = default);
}

/// <summary>Reads local document metadata and identifies embedded cover candidates without mutation.</summary>
public sealed class LocalBookDocumentMetadataReader : IBookDocumentMetadataReader
{
    public Task<BookDocumentMetadata> ReadAsync(string absolutePath, string extension, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalized = NormalizeExtension(extension);
        var result = normalized == ".epub" ? ReadEpub(absolutePath, cancellationToken) : normalized == ".pdf" ? ReadPdf(absolutePath, cancellationToken) : ReadComicArchive(absolutePath, normalized, cancellationToken);
        return Task.FromResult(result);
    }
    private static BookDocumentMetadata ReadEpub(string path, CancellationToken cancellationToken)
    {
        using var archive = ZipFile.OpenRead(path);
        var container = archive.GetEntry("META-INF/container.xml");
        if (container is null) return Fallback(path, ".epub");
        using var stream = container.Open(); var containerDoc = XDocument.Load(stream);
        var rootFile = containerDoc.Descendants().FirstOrDefault(e => e.Name.LocalName == "rootfile")?.Attribute("full-path")?.Value;
        var opf = rootFile is null ? null : archive.GetEntry(rootFile.Replace('\\', '/'));
        if (opf is null) return Fallback(path, ".epub");
        using var opfStream = opf.Open(); var doc = XDocument.Load(opfStream);
        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        Add(fields, "author", MetadataValue(doc, "creator")); Add(fields, "series", MetadataValue(doc, "belongs-to-collection")); Add(fields, "language", MetadataValue(doc, "language")); Add(fields, "publisher", MetadataValue(doc, "publisher")); Add(fields, "format", "epub");
        var coverId = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "meta" && string.Equals((string?)e.Attribute("name"), "cover", StringComparison.OrdinalIgnoreCase))?.Attribute("content")?.Value;
        var coverHref = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "item" && string.Equals((string?)e.Attribute("id"), coverId, StringComparison.Ordinal))?.Attribute("href")?.Value;
        var coverEntry = coverHref is null ? null : archive.GetEntry(ResolveArchivePath(rootFile!, coverHref));
        var artwork = coverEntry is null ? Array.Empty<ArtworkCandidate>() : [new ArtworkCandidate("thumbnail", "embedded:" + coverEntry.FullName, MimeTypeFor(coverEntry.FullName), coverEntry.Length)];
        return new BookDocumentMetadata(MetadataValue(doc, "title") ?? Path.GetFileNameWithoutExtension(path), fields, null, artwork);
    }
    private static BookDocumentMetadata ReadPdf(string path, CancellationToken cancellationToken)
    {
        using var stream = File.OpenRead(path); var length = (int)Math.Min(stream.Length, 1_048_576); var bytes = new byte[length]; _ = stream.Read(bytes, 0, bytes.Length); cancellationToken.ThrowIfCancellationRequested();
        var text = Encoding.Latin1.GetString(bytes); var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["format"] = "pdf" }; Add(fields, "author", PdfValue(text, "/Author"));
        return new BookDocumentMetadata(PdfValue(text, "/Title") ?? Path.GetFileNameWithoutExtension(path), fields, null);
    }
    private static BookDocumentMetadata ReadComicArchive(string path, string extension, CancellationToken cancellationToken)
    {
        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["format"] = extension.TrimStart('.') }; int? pageCount = null; IReadOnlyList<ArtworkCandidate> artwork = Array.Empty<ArtworkCandidate>();
        if (extension == ".cbz")
        {
            using var archive = ZipFile.OpenRead(path); var images = archive.Entries.Where(entry => !entry.FullName.EndsWith("/", StringComparison.Ordinal) && IsImage(entry.FullName)).OrderBy(entry => entry.FullName, StringComparer.OrdinalIgnoreCase).ToArray(); pageCount = images.Length;
            if (images.Length > 0) artwork = [new ArtworkCandidate("thumbnail", "embedded:" + images[0].FullName, MimeTypeFor(images[0].FullName), images[0].Length)];
        }
        cancellationToken.ThrowIfCancellationRequested(); return new BookDocumentMetadata(Path.GetFileNameWithoutExtension(path), fields, pageCount, artwork);
    }
    private static BookDocumentMetadata Fallback(string path, string extension) => new(Path.GetFileNameWithoutExtension(path), new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["format"] = extension.TrimStart('.') }, null);
    private static string ResolveArchivePath(string root, string href) => (Path.GetDirectoryName(root)?.Replace('\\', '/') is { Length: > 0 } dir ? dir + "/" : string.Empty) + href.Replace('\\', '/');
    private static string? MetadataValue(XDocument doc, string name) => doc.Descendants().FirstOrDefault(e => e.Name.LocalName.Equals(name, StringComparison.OrdinalIgnoreCase))?.Value.Trim();
    private static string? PdfValue(string text, string key) { var start = text.IndexOf(key, StringComparison.Ordinal); start = start < 0 ? -1 : text.IndexOf('(', start + key.Length); var end = start < 0 ? -1 : text.IndexOf(')', start + 1); return start >= 0 && end > start ? text[(start + 1)..end].Trim() : null; }
    private static bool IsImage(string name) => new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" }.Contains(Path.GetExtension(name), StringComparer.OrdinalIgnoreCase);
    private static string MimeTypeFor(string name) => Path.GetExtension(name).ToLowerInvariant() switch { ".jpg" or ".jpeg" => "image/jpeg", ".png" => "image/png", ".webp" => "image/webp", ".gif" => "image/gif", _ => "application/octet-stream" };
    private static void Add(IDictionary<string, string> target, string key, string? value) { if (!string.IsNullOrWhiteSpace(value)) target[key] = value.Trim(); }
    private static string NormalizeExtension(string extension) => string.IsNullOrWhiteSpace(extension) ? string.Empty : extension.StartsWith('.') ? extension.ToLowerInvariant() : "." + extension.ToLowerInvariant();
}
