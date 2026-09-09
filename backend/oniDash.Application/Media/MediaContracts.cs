using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using oniDash.Core.Domain;

namespace oniDash.Application.Media;

/// <summary>Capability-based handler contract. A handler may opt into one or more media types.</summary>
public interface IMediaHandler
{
    string Id { get; }
    IReadOnlySet<MediaType> SupportedTypes { get; }
    bool CanHandle(MediaDescriptor descriptor);
    Task<MediaInspectionResult> InspectAsync(MediaDescriptor descriptor, CancellationToken cancellationToken = default);
}

public sealed record MediaDescriptor(
    Guid FileId,
    Guid MediaItemId,
    MediaType MediaType,
    string AbsolutePath,
    string Extension,
    long SizeBytes,
    DateTimeOffset LastWriteTimeUtc);

public sealed record MediaInspectionResult(
    string? Title,
    TimeSpan? Duration,
    int? Width,
    int? Height,
    IReadOnlyDictionary<string, string> Metadata,
    IReadOnlyList<ArtworkCandidate> Artwork,
    MetadataProvenance Provenance);

public sealed record ArtworkCandidate(string Kind, string Source, string MimeType, long? SizeBytes);

public sealed record MetadataProvenance(
    string Source,
    double Confidence,
    DateTimeOffset ObservedAtUtc,
    bool IsExternal)
{
    public static MetadataProvenance Local(string source, double confidence = 1d) =>
        new(source, Math.Clamp(confidence, 0d, 1d), DateTimeOffset.UtcNow, false);
}
