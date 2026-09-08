using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TagLib;

namespace oniDash.Movies.Probing;

/// <summary>
/// Embedded-artwork extraction via TagLib#. Only the MP4 family (mp4/m4v) carries a
/// cover atom that TagLib# can read; other containers (mkv/avi) are unsupported and
/// yield null — the UI falls back to a generated placeholder. Read-only (rule 11).
/// </summary>
public sealed class TagLibVideoArtworkReader : IVideoArtworkReader
{
    public Task<VideoArtwork?> ReadAsync(string absolutePath, CancellationToken cancellationToken = default)
    {
        try
        {
            using var file = TagLib.File.Create(absolutePath);
            var picture = file.Tag.Pictures?
                .Where(p => p.Data is { Count: > 0 })
                .OrderBy(p => p.Type == PictureType.FrontCover ? 0 : 1)
                .FirstOrDefault();

            return Task.FromResult<VideoArtwork?>(picture is null
                ? null
                : new VideoArtwork(
                    picture.Data.Data,
                    string.IsNullOrWhiteSpace(picture.MimeType) ? "image/jpeg" : picture.MimeType));
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not OutOfMemoryException)
        {
            // Unsupported containers, corrupt atoms, transient IO locks: no artwork.
            return Task.FromResult<VideoArtwork?>(null);
        }
    }
}
