using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using oniDash.Core.Domain;

namespace oniDash.Application.Enrichment;

public sealed record MetadataField(string Name, string? Value, double Confidence, MetadataSource Source);

public enum MetadataSource
{
    Embedded = 1,
    Filename = 2,
    Folder = 3,
    Sidecar = 4,
    Provider = 5,
    User = 6,
}

public sealed record MetadataCandidate(
    Guid MediaItemId,
    MediaType MediaType,
    IReadOnlyList<MetadataField> Fields,
    string ProviderId,
    DateTimeOffset RetrievedAtUtc);

/// <summary>External metadata is opt-in enrichment; it is never the local catalogue source of truth.</summary>
public interface IMediaMetadataProvider
{
    string Id { get; }
    IReadOnlySet<MediaType> SupportedTypes { get; }
    Task<IReadOnlyList<MetadataCandidate>> SearchAsync(
        string query,
        MediaType mediaType,
        CancellationToken cancellationToken = default);
}

public sealed record EnrichmentPreview(
    Guid MediaItemId,
    IReadOnlyList<MetadataField> ProposedChanges,
    string ProviderId);

public interface IEnrichmentService
{
    Task<IReadOnlyList<EnrichmentPreview>> PreviewAsync(Guid mediaItemId, string providerId, CancellationToken cancellationToken = default);
    Task ApplyAsync(Guid mediaItemId, IReadOnlyList<MetadataField> changes, CancellationToken cancellationToken = default);
}
