using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace oniDash.Music.Metadata;

/// <summary>Read-only MusicBrainz enrichment. Results are proposals and never overwrite local tags.</summary>
public sealed class MusicBrainzMetadataProvider(HttpClient client) : IMusicMetadataProvider
{
    public string Id => "musicbrainz";

    public Task<MusicProviderResult?> SearchArtistAsync(string query, CancellationToken cancellationToken = default) => SearchAsync("artist", query, cancellationToken);
    public Task<MusicProviderResult?> SearchAlbumAsync(string query, CancellationToken cancellationToken = default) => SearchAsync("release-group", query, cancellationToken);
    public Task<MusicProviderResult?> SearchTrackAsync(string query, CancellationToken cancellationToken = default) => SearchAsync("recording", query, cancellationToken);

    private async Task<MusicProviderResult?> SearchAsync(string entity, string query, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query)) return null;
        var encoded = Uri.EscapeDataString(query.Trim());
        using var response = await client.GetAsync($"https://musicbrainz.org/ws/2/{entity}/?query={encoded}&fmt=json&limit=5", ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) return null;
        await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
        if (!json.RootElement.TryGetProperty($"{entity}s", out var results) || results.GetArrayLength() == 0) return null;
        var item = results[0];
        if (!item.TryGetProperty("id", out var id) || !item.TryGetProperty("score", out var score) || score.GetInt32() < 70) return null;
        var name = item.TryGetProperty("title", out var title) ? title.GetString() : item.TryGetProperty("name", out var n) ? n.GetString() : null;
        if (string.IsNullOrWhiteSpace(name)) return null;
        return new MusicProviderResult(Id, entity, id.GetString()!, name!, null, null);
    }
}
