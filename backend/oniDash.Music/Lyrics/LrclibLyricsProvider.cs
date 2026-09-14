using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace oniDash.Music.Lyrics;

public sealed class LrclibLyricsProvider(HttpClient httpClient) : IMusicLyricsProvider
{
    public string ProviderId => "lrclib";

    public async Task<IReadOnlyList<MusicLyricsCandidate>> SearchAsync(string track, string? artist, string? album, CancellationToken cancellationToken = default)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(track)) query.Add($"track_name={Uri.EscapeDataString(track)}");
        if (!string.IsNullOrWhiteSpace(artist)) query.Add($"artist_name={Uri.EscapeDataString(artist)}");
        if (!string.IsNullOrWhiteSpace(album)) query.Add($"album_name={Uri.EscapeDataString(album)}");
        if (query.Count == 0) return Array.Empty<MusicLyricsCandidate>();
        using var response = await httpClient.GetAsync($"/api/search?{string.Join("&", query)}", cancellationToken);
        if (!response.IsSuccessStatusCode) return Array.Empty<MusicLyricsCandidate>();
        var items = await response.Content.ReadFromJsonAsync<List<LrclibResult>>(cancellationToken: cancellationToken) ?? [];
        return items.Where(x => !string.IsNullOrWhiteSpace(x.TrackName)).Take(10).Select(x => new MusicLyricsCandidate(
            x.Id?.ToString() ?? $"{x.TrackName}:{x.ArtistName}", x.TrackName ?? track, x.ArtistName ?? artist ?? "", x.AlbumName ?? album ?? "",
            !string.IsNullOrWhiteSpace(x.SyncedLyrics), x.PlainLyrics ?? "", x.SyncedLyrics, ProviderId)).ToArray();
    }

    private sealed record LrclibResult(
        [property: JsonPropertyName("id")] long? Id,
        [property: JsonPropertyName("trackName")] string? TrackName,
        [property: JsonPropertyName("artistName")] string? ArtistName,
        [property: JsonPropertyName("albumName")] string? AlbumName,
        [property: JsonPropertyName("plainLyrics")] string? PlainLyrics,
        [property: JsonPropertyName("syncedLyrics")] string? SyncedLyrics);
}
