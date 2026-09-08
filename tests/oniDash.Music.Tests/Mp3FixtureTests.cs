using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using oniDash.Music.Tagging;
using Xunit;
using Xunit.Abstractions;

namespace oniDash.Music.Tests;

/// <summary>
/// Real audio-file tests: builds silent MP3 frames and round-trips them through
/// TagLib# to verify our reader against actual files (not just scripted tags).
/// The generator doubles as the E2E fixture factory.
/// </summary>
public sealed class Mp3FixtureTests(ITestOutputHelper output)
{
    // Minimal CBR MPEG-1 Layer III frame: 44.1kHz, 128kbps, stereo => 417 bytes, mostly padding.
    internal static byte[] SilentFrame()
    {
        var frame = new byte[417];
        frame[0] = 0xFF;
        frame[1] = 0xFB; // MPEG-1, Layer III, no CRC
        frame[2] = 0x90; // 128 kbps, 44.1 kHz
        frame[3] = 0xC4; // joint stereo, no copyright/original
        return frame;
    }

    internal static async Task<string> WriteTaggedMp3Async(
        string path,
        string? title,
        string? artist = null,
        string? albumArtist = null,
        string? album = null,
        uint track = 0,
        uint? year = null,
        byte[]? cover = null,
        int frames = 78)
    {
        var directory = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(directory);
        await using (var stream = File.Create(path))
        {
            for (var i = 0; i < frames; i++)
            {
                await stream.WriteAsync(SilentFrame());
            }
        }

        using (var file = TagLib.File.Create(path))
        {
            if (title is not null)
            {
                file.Tag.Title = title;
            }

            if (artist is not null)
            {
                file.Tag.Performers = [artist];
            }

            if (albumArtist is not null)
            {
                file.Tag.AlbumArtists = [albumArtist];
            }

            if (album is not null)
            {
                file.Tag.Album = album;
            }

            if (track > 0)
            {
                file.Tag.Track = track;
            }

            if (year is > 0)
            {
                file.Tag.Year = year.Value;
            }

            if (cover is not null)
            {
                file.Tag.Pictures =
                [
                    new TagLib.Id3v2.AttachmentFrame
                    {
                        Type = TagLib.PictureType.FrontCover,
                        MimeType = "image/jpeg",
                        Data = new TagLib.ByteVector(cover),
                    },
                ];
            }

            file.Save();
        }

        return path;
    }

    [Fact]
    public async Task Reader_extracts_all_text_tags_from_real_file()
    {
        var path = Path.Combine(Path.GetTempPath(), $"onidash-mp3-{Guid.NewGuid():N}.mp3");
        try
        {
            await WriteTaggedMp3Async(path, "Night Drive", "Kavinsky", "Kavinsky", "OutRun", 3, 2013);

            var tags = new TagLibAudioTagReader().Read(path);

            Assert.NotNull(tags);
            output.WriteLine($"duration={tags!.DurationSeconds}");
            Assert.Equal("Night Drive", tags.Title);
            Assert.Equal("Kavinsky", tags.TrackArtist);
            Assert.Equal("Kavinsky", tags.AlbumArtist);
            Assert.Equal("OutRun", tags.Album);
            Assert.Equal(3, tags.TrackNumber);
            Assert.Equal(2013, tags.Year);
            Assert.True(tags.DurationSeconds > 1.5);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task Reader_extracts_embedded_cover_art()
    {
        var jpeg = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 1, 2, 3, 0xFF, 0xD9 };
        var path = Path.Combine(Path.GetTempPath(), $"onidash-mp3-{Guid.NewGuid():N}.mp3");
        try
        {
            await WriteTaggedMp3Async(path, "Covered", album: "Covers", cover: jpeg);

            var tags = new TagLibAudioTagReader().Read(path);

            Assert.NotNull(tags);
            Assert.Equal(jpeg, tags!.CoverBytes);
            Assert.Equal("image/jpeg", tags.CoverContentType);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task Reader_returns_null_for_garbage_file()
    {
        var path = Path.Combine(Path.GetTempPath(), $"onidash-mp3-{Guid.NewGuid():N}.mp3");
        try
        {
            await File.WriteAllTextAsync(path, "this is not audio");

            Assert.Null(new TagLibAudioTagReader().Read(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task Reader_returns_null_for_missing_file()
    {
        Assert.Null(new TagLibAudioTagReader().Read(
            Path.Combine(Path.GetTempPath(), $"no-such-{Guid.NewGuid():N}.mp3")));
    }

    [Fact]
    public async Task Reader_prefers_front_cover_over_other_pictures()
    {
        var icon = new byte[] { 1, 1, 1 };
        var front = new byte[] { 2, 2, 2 };
        var path = Path.Combine(Path.GetTempPath(), $"onidash-mp3-{Guid.NewGuid():N}.mp3");
        try
        {
            await WriteTaggedMp3Async(path, "Pics", album: "Pics");
            using (var file = TagLib.File.Create(path))
            {
                file.Tag.Pictures =
                [
                    new TagLib.Id3v2.AttachmentFrame { Type = TagLib.PictureType.Other, MimeType = "image/png", Data = new TagLib.ByteVector(icon) },
                    new TagLib.Id3v2.AttachmentFrame { Type = TagLib.PictureType.FrontCover, MimeType = "image/jpeg", Data = new TagLib.ByteVector(front) },
                ];
                file.Save();
            }

            var tags = new TagLibAudioTagReader().Read(path);

            Assert.NotNull(tags);
            Assert.Equal(front, tags!.CoverBytes);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
