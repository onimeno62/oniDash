using System;
using System.Threading.Tasks;
using oniDash.Core.Domain;
using oniDash.Music.Metadata;
using oniDash.Music.Tagging;
using Xunit;

namespace oniDash.Music.Tests;

public sealed class ReplayGainMetadataTests
{
    [Fact]
    public async Task Publishes_supported_replay_gain_fields()
    {
        var tags = new AudioTags("Track", null, null, null, null, null, null, 60, null, null, null, -7.25, 0.91, -6.5, 0.98);
        var handler = new AudioMediaHandler(new Stub(tags));
        var result = await handler.InspectAsync(new Application.Media.MediaDescriptor("/music/a.flac", ".flac", MediaType.Audio, 1, DateTimeOffset.UtcNow));
        Assert.Equal("-7.25", result.Metadata["replayGainTrackGainDb"]);
        Assert.Equal("0.98", result.Metadata["replayGainAlbumPeak"]);
    }

    private sealed class Stub(AudioTags tags) : IAudioTagReader { public AudioTags? Read(string absolutePath) => tags; }
}
