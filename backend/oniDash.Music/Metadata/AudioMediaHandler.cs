using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using oniDash.Application.Media;
using oniDash.Core.Domain;
using oniDash.Music.Tagging;

namespace oniDash.Music.Metadata;

/// <summary>Adapts embedded audio tags to the platform-neutral media inspection contract.</summary>
public sealed class AudioMediaHandler(IAudioTagReader reader) : IMediaHandler
{
    private static readonly IReadOnlySet<MediaType> Types = new HashSet<MediaType> { MediaType.Audio };
    private static readonly IReadOnlySet<string> Extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".mp3", ".flac", ".m4a", ".aac", ".ogg", ".opus", ".wav", ".wma", ".aiff", ".ape"
    };

    public string Id => "music.audio";
    public IReadOnlySet<MediaType> SupportedTypes => Types;

    public bool CanHandle(MediaDescriptor descriptor) =>
        descriptor.MediaType == MediaType.Audio && Extensions.Contains(NormalizeExtension(descriptor.Extension));

    public Task<MediaInspectionResult> InspectAsync(MediaDescriptor descriptor, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!CanHandle(descriptor))
            throw new InvalidOperationException($"Handler '{Id}' cannot handle '{descriptor.Extension}' as {descriptor.MediaType}.");

        var tags = reader.Read(descriptor.AbsolutePath);
        if (tags is null)
        {
            return Task.FromResult(new MediaInspectionResult(
                null, null, null, null,
                new Dictionary<string, string>(),
                Array.Empty<ArtworkCandidate>(),
                MetadataProvenance.Local("embedded-audio-tags", 0d)));
        }

        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        Add(metadata, "artist", tags.TrackArtist);
        Add(metadata, "albumArtist", tags.AlbumArtist);
        Add(metadata, "album", tags.Album);
        Add(metadata, "trackNumber", tags.TrackNumber);
        Add(metadata, "discNumber", tags.DiscNumber);
        Add(metadata, "year", tags.Year);
        Add(metadata, "genre", tags.Genre);

        var artwork = tags.CoverBytes is { Length: > 0 } && !string.IsNullOrWhiteSpace(tags.CoverContentType)
            ? new[] { new ArtworkCandidate("front-cover", "embedded", tags.CoverContentType!, tags.CoverBytes.LongLength) }
            : Array.Empty<ArtworkCandidate>();

        return Task.FromResult(new MediaInspectionResult(
            tags.Title,
            tags.DurationSeconds is { } seconds && seconds >= 0 ? TimeSpan.FromSeconds(seconds) : null,
            null,
            null,
            metadata,
            artwork,
            MetadataProvenance.Local("embedded-audio-tags")));
    }

    private static string NormalizeExtension(string extension) =>
        string.IsNullOrWhiteSpace(extension) ? string.Empty : extension.StartsWith('.') ? extension : "." + extension;

    private static void Add(IDictionary<string, string> target, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value)) target[key] = value.Trim();
    }

    private static void Add(IDictionary<string, string> target, string key, int? value)
    {
        if (value is { } number && number > 0) target[key] = number.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}