using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using oniDash.Application.Media;
using oniDash.Core.Domain;

namespace oniDash.Infrastructure.Scanning;

/// <summary>Resolves a media type before catalogue-specific inspection. Extension matching is case-insensitive.</summary>
public interface IMediaTypeDetector
{
    MediaType Detect(string path);
}

public sealed class MediaTypeDetector : IMediaTypeDetector
{
    private static readonly IReadOnlyDictionary<string, MediaType> Extensions =
        new Dictionary<string, MediaType>(StringComparer.OrdinalIgnoreCase)
        {
            [".mp3"] = MediaType.Audio, [".flac"] = MediaType.Audio, [".m4a"] = MediaType.Audio,
            [".aac"] = MediaType.Audio, [".ogg"] = MediaType.Audio, [".wav"] = MediaType.Audio,
            [".mp4"] = MediaType.Video, [".mkv"] = MediaType.Video, [".avi"] = MediaType.Video,
            [".mov"] = MediaType.Video, [".webm"] = MediaType.Video,
            [".epub"] = MediaType.Book, [".pdf"] = MediaType.Document, [".mobi"] = MediaType.Book,
            [".azw"] = MediaType.Book, [".azw3"] = MediaType.Book, [".fb2"] = MediaType.Book,
            [".cbz"] = MediaType.Comic, [".cbr"] = MediaType.Comic, [".cb7"] = MediaType.Comic,
        };

    public MediaType Detect(string path) =>
        Extensions.TryGetValue(Path.GetExtension(path), out var type) ? type : MediaType.Unknown;
}

/// <summary>Ordered capability registry. More specific handlers can be registered ahead of generic ones.</summary>
public sealed class MediaHandlerRegistry(IEnumerable<IMediaHandler> handlers)
{
    private readonly IReadOnlyList<IMediaHandler> _handlers = handlers.ToArray();

    public IReadOnlyList<IMediaHandler> Resolve(MediaDescriptor descriptor) =>
        _handlers.Where(handler => handler.SupportedTypes.Contains(descriptor.MediaType) && handler.CanHandle(descriptor)).ToArray();
}
