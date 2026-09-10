using System.IO.Compression;
using System.Text;
using System.Xml.Linq;

namespace oniDash.Books.Metadata;

public sealed record BookDocumentMetadata(string? Title, IReadOnlyDictionary<string, string> Fields, int? PageCount);
public interface IBookDocumentMetadataReader
{
    Task<BookDocumentMetadata> ReadAsync(string absolutePath, string extension, CancellationToken cancellationToken = default);
}

/// <summary>Reads local document metadata without network services or file mutation.</summary>
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
        using var stream = container.Open();
        var containerDoc = XDocument.Load(stream);
        var rootFile = containerDoc.Descendants().FirstOrDefault(e => e.Name.LocalName == "rootfile")?.Attribute("full-path")?.Value;
        var opf = rootFile is null ? null : archive.GetEntry(rootFile.Replace('\\', '/'));
        if (opf is null) return Fallback(path, ".epub");
        using var opfStream = opf.Open();
        var doc = XDocument.Load(opfStream);
        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        Add(fields, "author", MetadataValue(doc, "creator"));
        Add(fields, "series", MetadataValue(doc, "belongs-to-collection"));
        Add(fields, "language", MetadataValue(doc, "language"));
        Add(fields, "publisher", MetadataValue(doc, "publisher"));
        Add(fields, "format", "epub");
        return new BookDocumentMetadata(MetadataValue(doc, "title") ?? Path.GetFileNameWithoutExtension(path), fields, null);
    }

    private static BookDocumentMetadata ReadPdf(string path, CancellationToken cancellationToken)
    {
        using var stream = File.OpenRead(path);
        var length = (int)Math.Min(stream.Length, 1_048_576);
        var bytes = new byte[length];
        _ = stream.Read(bytes, 0, bytes.Length);
        cancellationToken.ThrowIfCancellationRequested();
        var text = Encoding.Latin1.GetString(bytes);
        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["format"] = "pdf" };
        Add(fields, "author", PdfValue(text, "/Author"));
        return new BookDocumentMetadata(PdfValue(text, "/Title") ?? Path.GetFileNameWithoutExtension(path), fields, null);
    }

    private static BookDocumentMetadata ReadComicArchive(string path, string extension, CancellationToken cancellationToken)
    {
        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["format"] = extension.TrimStart('.') };
        int? pageCount = null;
        if (extension == ".cbz")
        {
            using var archive = ZipFile.OpenRead(path);
            pageCount = archive.Entries.Count(entry => !entry.FullName.EndsWith("/", StringComparison.Ordinal) && IsImage(entry.FullName));
        }
        cancellationToken.ThrowIfCancellationRequested();
        return new BookDocumentMetadata(Path.GetFileNameWithoutExtension(path), fields, pageCount);
    }

    private static BookDocumentMetadata Fallback(string path, string extension) => new(Path.GetFileNameWithoutExtension(path), new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["format"] = extension.TrimStart('.') }, null);
    private static string? MetadataValue(XDocument doc, string name) => doc.Descendants().FirstOrDefault(e => e.Name.LocalName.Equals(name, StringComparison.OrdinalIgnoreCase))?.Value.Trim();
    private static string? PdfValue(string text, string key)
    {
        var start = text.IndexOf(key, StringComparison.Ordinal);
        start = start < 0 ? -1 : text.IndexOf('(', start + key.Length);
        var end = start < 0 ? -1 : text.IndexOf(')', start + 1);
        return start >= 0 && end > start ? text[(start + 1)..end].Trim() : null;
    }
    private static bool IsImage(string name) => new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" }.Contains(Path.GetExtension(name), StringComparer.OrdinalIgnoreCase);
    private static void Add(IDictionary<string, string> target, string key, string? value) { if (!string.IsNullOrWhiteSpace(value)) target[key] = value.Trim(); }
    private static string NormalizeExtension(string extension) => string.IsNullOrWhiteSpace(extension) ? string.Empty : extension.StartsWith('.') ? extension.ToLowerInvariant() : "." + extension.ToLowerInvariant();
}
