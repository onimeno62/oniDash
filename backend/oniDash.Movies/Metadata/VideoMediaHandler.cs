using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using oniDash.Application.Media;
using oniDash.Core.Domain;
using oniDash.Movies.Probing;

namespace oniDash.Movies.Metadata;

/// <summary>Adapts local video probe results to the platform-neutral media inspection contract.</summary>
public sealed class VideoMediaHandler(IVideoProbeReader reader) : IMediaHandler
{
    private static readonly IReadOnlySet<MediaType> Types = new HashSet<MediaType> { MediaType.Video };
    private static readonly IReadOnlySet<string> Extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".mkv", ".avi", ".mov", ".webm"
    };

    public string Id => "movies.video";
    public IReadOnlySet<MediaType> SupportedTypes => Types;

    public bool CanHandle(MediaDescriptor descriptor) =>
        descriptor.MediaType == MediaType.Video && Extensions.Contains(NormalizeExtension(descriptor.Extension));

    public async Task<MediaInspectionResult> InspectAsync(
        MediaDescriptor descriptor,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!CanHandle(descriptor))
            throw new InvalidOperationException($"Handler '{Id}' cannot handle '{descriptor.Extension}' as {descriptor.MediaType}.");

        var probe = await reader.ProbeAsync(descriptor.AbsolutePath, cancellationToken).ConfigureAwait(false);
        var title = Path.GetFileNameWithoutExtension(descriptor.AbsolutePath).Trim();
        var duration = probe?.DurationSeconds is { } seconds && seconds >= 0
            ? TimeSpan.FromSeconds(seconds)
            : null;

        return new MediaInspectionResult(
            string.IsNullOrWhiteSpace(title) ? null : title,
            duration,
            PositiveOrNull(probe?.Width),
            PositiveOrNull(probe?.Height),
            new Dictionary<string, string>(),
            Array.Empty<ArtworkCandidate>(),
            MetadataProvenance.Local("local-video-probe", probe is null ? 0.25d : 1d));
    }

    private static string NormalizeExtension(string extension) =>
        string.IsNullOrWhiteSpace(extension) ? string.Empty : extension.StartsWith('.') ? extension : "." + extension;

    private static int? PositiveOrNull(int? value) => value is > 0 ? value : null;
}
