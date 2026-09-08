using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using oniDash.Application.Libraries;
using oniDash.Core.Domain;

namespace oniDash.Movies.Cataloging;

public sealed record VideoFileLocation(string AbsolutePath, string ContentType);

/// <summary>Resolves a movie's file row to an absolute path plus a video content type.</summary>
public interface IVideoFileLocator
{
    Task<VideoFileLocation?> LocateAsync(Guid fileId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Resolves a media file row to its absolute path (source root + scanner-relative path,
/// re-validated to stay inside the root) plus a video content type.
/// </summary>
public sealed class VideoFileLocator(
    IMediaFileRepository fileRepository,
    ILibrarySourceRepository sourceRepository) : IVideoFileLocator
{
    private static readonly Dictionary<string, string> ContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".mp4"] = "video/mp4",
        [".m4v"] = "video/mp4",
        [".mkv"] = "video/x-matroska",
        [".avi"] = "video/x-msvideo",
        [".mov"] = "video/quicktime",
        [".wmv"] = "video/x-ms-wmv",
        [".webm"] = "video/webm",
        [".mpg"] = "video/mpeg",
        [".mpeg"] = "video/mpeg",
        [".ts"] = "video/mp2t",
        [".flv"] = "video/x-flv",
    };

    public async Task<VideoFileLocation?> LocateAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        var file = await fileRepository.GetByIdAsync(fileId, cancellationToken).ConfigureAwait(false);
        if (file is null || file.MissingSinceUtc is not null)
        {
            return null;
        }

        var source = await sourceRepository
            .GetByIdAsync(file.LibrarySourceId, cancellationToken)
            .ConfigureAwait(false);
        if (source is null)
        {
            return null;
        }

        var root = Path.GetFullPath(source.RootPath);
        var absolutePath = Path.GetFullPath(Path.Combine(
            root, file.RelativePath.Replace('/', Path.DirectorySeparatorChar)));
        if (!absolutePath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var extension = Path.GetExtension(absolutePath);
        return new VideoFileLocation(
            absolutePath,
            ContentTypes.TryGetValue(extension, out var contentType) ? contentType : "application/octet-stream");
    }
}
