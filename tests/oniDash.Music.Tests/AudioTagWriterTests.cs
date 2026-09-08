using System;
using System.IO;
using System.Threading.Tasks;
using oniDash.Music.Tagging;
using Xunit;

namespace oniDash.Music.Tests;

public sealed class AudioTagWriterTests
{
    [Fact]
    public async Task Writes_supplied_fields_and_preserves_unsupplied_fields()
    {
        var path = Path.Combine(Path.GetTempPath(), $"onidash-writer-{Guid.NewGuid():N}.mp3");
        try
        {
            await Mp3FixtureTests.WriteTaggedMp3Async(path, "Original title", "Original artist", album: "Original album", track: 2, year: 2001);
            var ok = new TagLibAudioTagWriter().Write(path, new AudioMetadataUpdate(" Updated title ", null, null, null, null, null, null, " Rock "), out var error);
            Assert.True(ok, error);
            var tags = new TagLibAudioTagReader().Read(path);
            Assert.NotNull(tags); Assert.Equal("Updated title", tags!.Title); Assert.Equal("Original artist", tags.TrackArtist); Assert.Equal("Original album", tags.Album); Assert.Equal(2, tags.TrackNumber); Assert.Equal(2001, tags.Year); Assert.Equal("Rock", tags.Genre);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task Failed_write_does_not_replace_original_file()
    {
        var path = Path.Combine(Path.GetTempPath(), $"onidash-writer-{Guid.NewGuid():N}.mp3");
        try
        {
            await File.WriteAllTextAsync(path, "not audio"); var before = await File.ReadAllBytesAsync(path);
            var ok = new TagLibAudioTagWriter().Write(path, new AudioMetadataUpdate("Title", null, null, null, null, null, null, null), out var error);
            Assert.False(ok); Assert.False(string.IsNullOrWhiteSpace(error)); Assert.Equal(before, await File.ReadAllBytesAsync(path));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Missing_file_returns_a_recoverable_error()
    {
        var ok = new TagLibAudioTagWriter().Write(Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.mp3"), new AudioMetadataUpdate("Title", null, null, null, null, null, null, null), out var error);
        Assert.False(ok); Assert.False(string.IsNullOrWhiteSpace(error));
    }
}
