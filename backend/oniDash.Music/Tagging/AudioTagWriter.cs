using System;

namespace oniDash.Music.Tagging;

public sealed record AudioMetadataUpdate(
    string? Title,
    string? TrackArtist,
    string? AlbumArtist,
    string? Album,
    int? TrackNumber,
    int? DiscNumber,
    int? Year,
    string? Genre);

public interface IAudioTagWriter
{
    bool Write(string absolutePath, AudioMetadataUpdate update, out string? error);
}

/// <summary>Writes only explicitly supplied fields through TagLib#.</summary>
public sealed class TagLibAudioTagWriter : IAudioTagWriter
{
    public bool Write(string absolutePath, AudioMetadataUpdate update, out string? error)
    {
        error = null;
        try
        {
            using var file = TagLib.File.Create(absolutePath);
            var tag = file.Tag;
            if (update.Title is not null) tag.Title = update.Title.Trim();
            if (update.TrackArtist is not null) tag.Performers = [update.TrackArtist.Trim()];
            if (update.AlbumArtist is not null) tag.AlbumArtists = [update.AlbumArtist.Trim()];
            if (update.Album is not null) tag.Album = update.Album.Trim();
            if (update.TrackNumber is not null) tag.Track = (uint)Math.Max(0, update.TrackNumber.Value);
            if (update.DiscNumber is not null) tag.Disc = (uint)Math.Max(0, update.DiscNumber.Value);
            if (update.Year is not null) tag.Year = (uint)Math.Max(0, update.Year.Value);
            if (update.Genre is not null) tag.Genres = [update.Genre.Trim()];
            file.Save();
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not OutOfMemoryException)
        {
            error = ex.Message;
            return false;
        }
    }
}
