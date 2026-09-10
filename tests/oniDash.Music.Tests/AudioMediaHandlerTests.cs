using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using oniDash.Application.Media;
using oniDash.Core.Domain;
using oniDash.Music.Metadata;
using oniDash.Music.Tagging;
using Xunit;

namespace oniDash.Music.Tests;

public sealed class AudioMediaHandlerTests
{
    [Fact]
    public async Task Inspects_embedded_tags_into_canonical_contract()
    {
        var handler = new AudioMediaHandler(new FakeReader(new AudioTags(
            "Song", "Artist", "Album Artist", "Album", 4, 2, 2026, 123.5, "Rock",
            new byte[] { 1, 2, 3 }, "image/jpeg")));

        var result = await handler.InspectAsync(new MediaDescriptor(
            Guid.NewGuid(), Guid.NewGuid(), MediaType.Audio, "C:/music/song.mp3", ".mp3", 3, DateTimeOffset.UtcNow));

        Assert.Equal("Song", result.Title);
        Assert.Equal(TimeSpan.FromSeconds(123.5), result.Duration);
        Assert.Equal("Artist", result.Metadata["artist"]);
        Assert.Equal("Album Artist", result.Metadata["albumArtist"]);
        Assert.Equal("4", result.Metadata["trackNumber"]);
        Assert.Equal("2", result.Metadata["discNumber"]);
        Assert.Equal("2026", result.Metadata["year"]);
        Assert.Single(result.Artwork);
        Assert.Equal("embedded", result.Artwork[0].Source);
        Assert.Equal(3, result.Artwork[0].SizeBytes);
        Assert.False(result.Provenance.IsExternal);
    }

    [Theory]
    [InlineData("mp3")]
    [InlineData(".FLAC")]
    [InlineData("OpUs")]
    public void Accepts_supported_audio_extensions_without_leading_dot(string extension)
    {
        var handler = new AudioMediaHandler(new FakeReader(null));
        var descriptor = new MediaDescriptor(Guid.NewGuid(), Guid.NewGuid(), MediaType.Audio, "song", extension, 1, DateTimeOffset.UtcNow);
        Assert.True(handler.CanHandle(descriptor));
    }

    [Fact]
    public void Rejects_non_audio_media_types_even_when_extension_matches()
    {
        var handler = new AudioMediaHandler(new FakeReader(null));
        var descriptor = new MediaDescriptor(Guid.NewGuid(), Guid.NewGuid(), MediaType.Movie, "movie.mp3", ".mp3", 1, DateTimeOffset.UtcNow);
        Assert.False(handler.CanHandle(descriptor));
    }

    [Fact]
    public async Task Unreadable_audio_returns_empty_local_inspection_instead_of_throwing()
    {
        var handler = new AudioMediaHandler(new FakeReader(null));
        var result = await handler.InspectAsync(new MediaDescriptor(
            Guid.NewGuid(), Guid.NewGuid(), MediaType.Audio, "bad.mp3", ".mp3", 1, DateTimeOffset.UtcNow));

        Assert.Null(result.Title);
        Assert.Null(result.Duration);
        Assert.Empty(result.Metadata);
        Assert.Empty(result.Artwork);
        Assert.Equal(0d, result.Provenance.Confidence);
    }

    private sealed class FakeReader(AudioTags? tags) : IAudioTagReader
    {
        public AudioTags? Read(string absolutePath) => tags;
    }
}