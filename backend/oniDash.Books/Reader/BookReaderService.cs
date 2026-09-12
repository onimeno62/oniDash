using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace oniDash.Books.Reader;

public sealed record BookTocEntry(string Title, string Target, int? PageNumber);
public sealed record BookManifest(string Format, int TotalPages, string? Title, List<BookTocEntry> TableOfContents);
public sealed record BookPageContent(int PageNumber, string ContentType, byte[] Data, string? Text = null);

public interface IBookReaderService
{
    Task<BookManifest> GetManifestAsync(string filePath, CancellationToken ct = default);
    Task<BookPageContent?> GetPageAsync(string filePath, int pageNumber, CancellationToken ct = default);
    Stream? OpenReadStream(string filePath);
}

public sealed class LocalBookReaderService : IBookReaderService
{
    public Task<BookManifest> GetManifestAsync(string filePath, CancellationToken ct = default)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Book file not found", filePath);

        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        if (ext is ".cbz" or ".zip")
        {
            using var archive = ZipFile.OpenRead(filePath);
            var pageEntries = archive.Entries
                .Where(e => !e.FullName.EndsWith("/") && IsImageFile(e.Name))
                .OrderBy(e => e.FullName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var toc = pageEntries.Select((e, idx) => new BookTocEntry(Path.GetFileNameWithoutExtension(e.Name), e.FullName, idx + 1)).ToList();
            return Task.FromResult(new BookManifest("CBZ", pageEntries.Count, Path.GetFileNameWithoutExtension(filePath), toc));
        }

        if (ext is ".epub" or ".epub3")
        {
            try
            {
                using var archive = ZipFile.OpenRead(filePath);
                var htmlEntries = archive.Entries
                    .Where(e => e.FullName.EndsWith(".xhtml", StringComparison.OrdinalIgnoreCase) ||
                                e.FullName.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(e => e.FullName, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var toc = htmlEntries.Select((e, idx) => new BookTocEntry(Path.GetFileNameWithoutExtension(e.Name), e.FullName, idx + 1)).ToList();
                return Task.FromResult(new BookManifest("EPUB", Math.Max(1, htmlEntries.Count), Path.GetFileNameWithoutExtension(filePath), toc));
            }
            catch
            {
                return Task.FromResult(new BookManifest("EPUB", 1, Path.GetFileNameWithoutExtension(filePath), new()));
            }
        }

        // PDF fallback manifest representation
        return Task.FromResult(new BookManifest(ext.TrimStart('.').ToUpperInvariant(), 1, Path.GetFileNameWithoutExtension(filePath), new()));
    }

    public async Task<BookPageContent?> GetPageAsync(string filePath, int pageNumber, CancellationToken ct = default)
    {
        if (!File.Exists(filePath) || pageNumber < 1) return null;
        var ext = Path.GetExtension(filePath).ToLowerInvariant();

        if (ext is ".cbz" or ".zip")
        {
            using var archive = ZipFile.OpenRead(filePath);
            var images = archive.Entries
                .Where(e => !e.FullName.EndsWith("/") && IsImageFile(e.Name))
                .OrderBy(e => e.FullName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (pageNumber > images.Count) return null;
            var entry = images[pageNumber - 1];
            await using var stream = entry.Open();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms, ct);
            return new BookPageContent(pageNumber, GuessContentType(entry.Name), ms.ToArray());
        }

        if (ext is ".epub" or ".epub3")
        {
            using var archive = ZipFile.OpenRead(filePath);
            var pages = archive.Entries
                .Where(e => e.FullName.EndsWith(".xhtml", StringComparison.OrdinalIgnoreCase) ||
                            e.FullName.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
                .OrderBy(e => e.FullName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (pageNumber > pages.Count) return null;
            var entry = pages[pageNumber - 1];
            await using var stream = entry.Open();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms, ct);
            var text = Encoding.UTF8.GetString(ms.ToArray());
            return new BookPageContent(pageNumber, "application/xhtml+xml", ms.ToArray(), text);
        }

        return null;
    }

    public Stream? OpenReadStream(string filePath)
    {
        return File.Exists(filePath) ? File.OpenRead(filePath) : null;
    }

    private static bool IsImageFile(string filename)
    {
        var ext = Path.GetExtension(filename).ToLowerInvariant();
        return ext is ".jpg" or ".jpeg" or ".png" or ".webp" or ".gif" or ".bmp";
    }

    private static string GuessContentType(string filename)
    {
        return Path.GetExtension(filename).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            _ => "application/octet-stream"
        };
    }
}
