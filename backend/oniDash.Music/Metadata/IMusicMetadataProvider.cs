using System.Threading;
using System.Threading.Tasks;

namespace oniDash.Music.Metadata;

public interface IMusicMetadataProvider
{
    string Id { get; }
    Task<MusicProviderResult?> SearchArtistAsync(string query, CancellationToken cancellationToken = default);
    Task<MusicProviderResult?> SearchAlbumAsync(string query, CancellationToken cancellationToken = default);
    Task<MusicProviderResult?> SearchTrackAsync(string query, CancellationToken cancellationToken = default);
}

public sealed record MusicProviderResult(string ProviderId, string EntityType, string ExternalId, string Name, string? ArtworkUrl, string? Description);
