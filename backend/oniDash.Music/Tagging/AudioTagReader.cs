using System;
using System.Linq;

namespace oniDash.Music.Tagging;

/// <summary>Embedded metadata extracted from one audio file. Nulls mean tag absent.</summary>
public sealed record AudioTags(
    string? Title,
    string? TrackArtist,
    string? AlbumArtist,
    string? Album,
    int? TrackNumber,
    int? DiscNumber,
    int? Year,
    double? DurationSeconds,
    string? Genre,
    byte[]? CoverBytes,
    string? CoverContentType,
    double? ReplayGainTrackDb = null,
    double? ReplayGainAlbumDb = null,
    double? R128TrackGainDb = null,
    double? R128AlbumGainDb = null);

public interface IAudioTagReader
{
    AudioTags? Read(string absolutePath);
}

/// <summary>TagLib#-based read-only audio metadata reader.</summary>
public sealed class TagLibAudioTagReader : IAudioTagReader
{
    public AudioTags? Read(string absolutePath)
    {
        try
        {
            using var file = TagLib.File.Create(absolutePath);
            var tag = file.Tag;
            var properties = file.Properties;
            var cover = tag.Pictures?.Where(p => p.Data is { Count: > 0 }).OrderBy(p => p.Type == TagLib.PictureType.FrontCover ? 0 : 1).FirstOrDefault();
            return new AudioTags(
                NullIfEmpty(tag.Title), NullIfEmpty(tag.FirstPerformer), NullIfEmpty(tag.FirstAlbumArtist), NullIfEmpty(tag.Album),
                tag.Track > 0 ? (int)tag.Track : null, tag.Disc > 0 ? (int)tag.Disc : null, tag.Year > 0 ? (int)tag.Year : null,
                properties is null ? null : properties.Duration.TotalSeconds, NullIfEmpty(tag.FirstGenre), cover?.Data?.Data,
                string.IsNullOrWhiteSpace(cover?.MimeType) ? null : cover!.MimeType,
                ParseGain(tag.ToString(), "REPLAYGAIN_TRACK_GAIN"), ParseGain(tag.ToString(), "REPLAYGAIN_ALBUM_GAIN"),
                ParseGain(tag.ToString(), "R128_TRACK_GAIN"), ParseGain(tag.ToString(), "R128_ALBUM_GAIN"));
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not OutOfMemoryException)
        {
            return null;
        }
    }

    private static double? ParseGain(string text, string key)
    {
        var marker = text.IndexOf(key, StringComparison.OrdinalIgnoreCase);
        if (marker < 0) return null;
        var start = marker + key.Length;
        while (start < text.Length && (char.IsWhiteSpace(text[start]) || text[start] is ':' or '=')) start++;
        var end = start;
        while (end < text.Length && (char.IsDigit(text[end]) || text[end] is '+' or '-' or '.' or ',')) end++;
        return double.TryParse(text[start..end].Replace(',', '.'), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var gain) ? gain : null;
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
