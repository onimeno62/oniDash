using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace oniDash.Application.Artwork;

public enum ArtworkKind
{
    Cover = 1,
    Thumbnail = 2,
    Banner = 3,
    Backdrop = 4,
    Artist = 5,
}

public sealed record ArtworkRequest(Guid MediaItemId, ArtworkKind Kind, string SourcePath, string? MimeType = null);
public sealed record ArtworkResource(Guid MediaItemId, ArtworkKind Kind, string CacheKey, string MimeType, long SizeBytes);

/// <summary>Artwork storage is derived/cache data and may be safely rebuilt from source media.</summary>
public interface IArtworkCache
{
    Task<ArtworkResource?> GetAsync(Guid mediaItemId, ArtworkKind kind, CancellationToken cancellationToken = default);
    Task<ArtworkResource> PutAsync(ArtworkRequest request, Stream content, CancellationToken cancellationToken = default);
    Task RemoveAsync(Guid mediaItemId, ArtworkKind kind, CancellationToken cancellationToken = default);
}
