using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using oniDash.Books.Reader;
using Xunit;

namespace oniDash.Books.Tests;

public class BookReaderServiceTests
{
    [Fact]
    public async Task GetManifest_And_GetPage_WorksForZipCbz()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_book_{Guid.NewGuid():N}.cbz");
        try
        {
            using (var zip = ZipFile.Open(tempFile, ZipArchiveMode.Create))
            {
                var entry1 = zip.CreateEntry("page_01.jpg");
                using (var s = entry1.Open())
                {
                    s.Write(new byte[] { 0xFF, 0xD8, 0xFF });
                }

                var entry2 = zip.CreateEntry("page_02.png");
                using (var s = entry2.Open())
                {
                    s.Write(new byte[] { 0x89, 0x50, 0x4E, 0x47 });
                }
            }

            var service = new LocalBookReaderService();
            var manifest = await service.GetManifestAsync(tempFile);

            Assert.Equal("CBZ", manifest.Format);
            Assert.Equal(2, manifest.TotalPages);
            Assert.Equal(2, manifest.TableOfContents.Count);

            var page1 = await service.GetPageAsync(tempFile, 1);
            Assert.NotNull(page1);
            Assert.Equal("image/jpeg", page1!.ContentType);

            var page2 = await service.GetPageAsync(tempFile, 2);
            Assert.NotNull(page2);
            Assert.Equal("image/png", page2!.ContentType);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }
}
