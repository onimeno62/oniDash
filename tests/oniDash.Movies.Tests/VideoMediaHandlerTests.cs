using oniDash.Application.Media;
using oniDash.Core.Domain;
using oniDash.Movies.Metadata;
using oniDash.Movies.Probing;
using Xunit;

namespace oniDash.Movies.Tests;

public sealed class VideoMediaHandlerTests
{
    [Fact]
    public async Task Inspect_maps_local_probe_results_to_canonical_contract()
    {
        var path = Path.Combine(Path.GetTempPath(), "The.Movie.2026.mkv");
        var reader = new FakeVideoProbeReader();
        reader.ProbesByPath[path] = new VideoProbe(7_201.5, 3840, 2160);
        var handler = new VideoMediaHandler(reader);

        var result = await handler.InspectAsync(Descriptor(path, ".MKV"));

        Assert.Equal("The.Movie.2026", result.Title);
        Assert.Equal(TimeSpan.FromSeconds(7_201.5), result.Duration);
        Assert.Equal(3840, result.Width);
        Assert.Equal(2160, result.Height);
        Assert.Empty(result.Metadata);
        Assert.Empty(result.Artwork);
        Assert.Equal("local-video-probe", result.Provenance.Source);
        Assert.Equal(1d, result.Provenance.Confidence);
        Assert.False(result.Provenance.IsExternal);
    }

    [Fact]
    public async Task Inspect_degrades_to_filename_when_probe_is_unavailable()
    {
        var path = Path.Combine(Path.GetTempPath(), "Offline Video.mp4");
        var handler = new VideoMediaHandler(new FakeVideoProbeReader());

        var result = await handler.InspectAsync(Descriptor(path, "mp4"));

        Assert.Equal("Offline Video", result.Title);
        Assert.Null(result.Duration);
        Assert.Null(result.Width);
        Assert.Null(result.Height);
        Assert.Equal(0.25d, result.Provenance.Confidence);
    }

    [Theory]
    [InlineData(MediaType.Audio, ".mp4")]
    [InlineData(MediaType.Video, ".mp3")]
    public async Task Inspect_rejects_unsupported_descriptors(MediaType type, string extension)
    {
        var handler = new VideoMediaHandler(new FakeVideoProbeReader());
        var descriptor = Descriptor("video" + extension, extension) with { MediaType = type };

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.InspectAsync(descriptor));
    }

    [Fact]
    public async Task Inspect_honors_pre_cancelled_requests()
    {
        var handler = new VideoMediaHandler(new FakeVideoProbeReader());
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            handler.InspectAsync(Descriptor("video.webm", ".webm"), cancellation.Token));
    }

    private static MediaDescriptor Descriptor(string path, string extension) => new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        MediaType.Video,
        path,
        extension,
        123,
        DateTimeOffset.UtcNow);
}
