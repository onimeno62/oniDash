using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using oniDash.Application.Common;
using oniDash.Application.Libraries;
using oniDash.Core.Domain;

namespace oniDash.Music.Cataloging;

public sealed record AudioFileLocation(string AbsolutePath, string ContentType);

/// <summary>
/// Resolves a media file row to its absolute path (source root + scanner-relative path,
/// re-validated to stay inside the root) plus an audio content type.
/// </summary>
public sealed class AudioFileLocator(
    IMediaFileRepository fileRepository,
    ILibrarySourceRepository sourceRepository) : IMediaFileLocator
{
    private static readonly Dictionary<string, string> ContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".mp3"] = "audio/mpeg",
        [".m4a"] = "audio/mp4",
        [".mp4"] = "audio/mp4",
        [".aac"] = "audio/aac",
        [".flac"] = "audio/flac",
        [".ogg"] = "audio/ogg",
        [".opus"] = "audio/ogg",
        [".wav"] = "audio/wav",
        [".wma"] = "audio/x-ms-wma",
    };

    public async Task<AudioFileLocation?> LocateAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        var file = await fileRepository.GetByIdAsync(fileId, cancellationToken).ConfigureAwait(false);
        if (file is null || file.MissingSinceUtc is not null)
        {
            return null;
        }

        var source = await sourceRepository
            .GetByIdAsync(file.LibrarySourceId, cancellationToken)
            .ConfigureAwait(false);
        if (source is null || !PathSafety.TryResolveChildPath(source.RootPath, file.RelativePath, out var absolutePath))
        {
            return null;
        }

        var extension = Path.GetExtension(absolutePath);
        return new AudioFileLocation(
            absolutePath,
            ContentTypes.TryGetValue(extension, out var contentType) ? contentType : "application/octet-stream");
    }
}
