using System;
using System.Linq;

namespace oniDash.Music.Tagging;

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
    double? ReplayGainTrackGainDb = null,
    double? ReplayGainTrackPeak = null,
    double? ReplayGainAlbumGainDb = null,
    double? ReplayGainAlbumPeak = null);

public interface IAudioTagReader
{
    AudioTags? Read(string absolutePath);
}

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
                FiniteOrNull(tag.ReplayGainTrackGain), FinitePositiveOrNull(tag.ReplayGainTrackPeak),
                FiniteOrNull(tag.ReplayGainAlbumGain), FinitePositiveOrNull(tag.ReplayGainAlbumPeak));
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not OutOfMemoryException)
        {
            return null;
        }
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static double? FiniteOrNull(double value) => double.IsFinite(value) ? value : null;
    private static double? FinitePositiveOrNull(double value) => double.IsFinite(value) && value > 0 ? value : null;
}
