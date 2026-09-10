using oniDash.Core.Domain;
using oniDash.Infrastructure.Scanning;
using Xunit;

namespace oniDash.Api.Tests;

public sealed class MediaTypeDetectorTests
{
    private readonly MediaTypeDetector _detector = new();

    [Theory]
    [InlineData("track.MP3", MediaType.Audio)]
    [InlineData("movie.mkv", MediaType.Video)]
    [InlineData("novel.epub", MediaType.Book)]
    [InlineData("scan.CBZ", MediaType.Comic)]
    [InlineData("file.unknown", MediaType.Unknown)]
    public void Detect_is_case_insensitive_and_catalogue_neutral(string path, MediaType expected)
    {
        Assert.Equal(expected, _detector.Detect(path));
    }
}
