using oniDash.Application.Media;
using oniDash.Core.Domain;
using Xunit;

namespace oniDash.Application.Tests;

public sealed class MetadataNormalizationTests
{
    [Fact]
    public void Normalize_trims_values_normalizes_keys_and_preserves_provenance()
    {
        var input = new MediaInspectionResult(
            "  A   Title  ",
            TimeSpan.FromSeconds(12),
            1920,
            1080,
            new Dictionary<string, string> { [" Track Number "] = "  01  ", ["empty"] = " " },
            new[] { new ArtworkCandidate(" front-cover ", " embedded ", "IMAGE/JPEG", 42) },
            new MetadataProvenance("local", 4d, DateTimeOffset.UtcNow, false));

        var result = new MediaMetadataNormalizer().Normalize(input);

        Assert.Equal("A Title", result.Title);
        Assert.Equal("01", result.Metadata["tracknumber"]);
        Assert.DoesNotContain("empty", result.Metadata.Keys);
        Assert.Equal("front-cover", result.Artwork[0].Kind);
        Assert.Equal("embedded", result.Artwork[0].Source);
        Assert.Equal("image/jpeg", result.Artwork[0].MimeType);
        Assert.Equal(1d, result.Provenance.Confidence);
    }

    [Fact]
    public void Normalize_drops_invalid_dimensions_duration_and_artwork()
    {
        var input = new MediaInspectionResult("title", TimeSpan.FromSeconds(-1), 0, -1,
            new Dictionary<string, string>(),
            new[] { new ArtworkCandidate("poster", "", "image/jpeg", null) },
            MetadataProvenance.Local("local"));

        var result = new MediaMetadataNormalizer().Normalize(input);

        Assert.Null(result.Duration);
        Assert.Null(result.Width);
        Assert.Null(result.Height);
        Assert.Empty(result.Artwork);
    }
}
