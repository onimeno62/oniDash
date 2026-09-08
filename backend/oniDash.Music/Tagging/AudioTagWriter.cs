using System;
using System.IO;

namespace oniDash.Music.Tagging;

public sealed record AudioMetadataUpdate(string? Title, string? TrackArtist, string? AlbumArtist, string? Album, int? TrackNumber, int? DiscNumber, int? Year, string? Genre);
public interface IAudioTagWriter { bool Write(string absolutePath, AudioMetadataUpdate update, out string? error); }

/// <summary>Stages TagLib writes beside the source and replaces only after a successful save.</summary>
public sealed class TagLibAudioTagWriter : IAudioTagWriter
{
    public bool Write(string absolutePath, AudioMetadataUpdate update, out string? error)
    {
        error = null;
        var tempPath = $"{absolutePath}.{Guid.NewGuid():N}.onidash.tmp";
        try
        {
            File.Copy(absolutePath, tempPath, overwrite: false);
            using (var file = TagLib.File.Create(tempPath))
            {
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
            }
            try { File.Replace(tempPath, absolutePath, null, ignoreMetadataErrors: true); }
            catch (PlatformNotSupportedException) { File.Move(tempPath, absolutePath, overwrite: true); }
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not OutOfMemoryException)
        {
            error = ex.Message;
            return false;
        }
        finally
        {
            try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { }
        }
    }
}
