using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace oniDash.Manga;

public sealed class SuwayomiClient
{
    private readonly HttpClient _http;
    private readonly SuwayomiOptions _options;
    public SuwayomiClient(HttpClient http, IOptions<SuwayomiOptions> options)
    {
        _http = http; _options = options.Value;
        _http.BaseAddress = new Uri(_options.BaseUrl.TrimEnd('/') + "/");
        _http.Timeout = TimeSpan.FromSeconds(Math.Clamp(_options.TimeoutSeconds, 5, 180));
    }
    public async Task<JsonElement> QueryAsync(string query, object? variables = null, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/graphql");
        request.Content = new StringContent(JsonSerializer.Serialize(new { query, variables }), Encoding.UTF8, "application/json");
        if (!string.IsNullOrWhiteSpace(_options.AccessToken)) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.AccessToken);
        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException($"Suwayomi returned {(int)response.StatusCode}: {body}");
        using var document = JsonDocument.Parse(body);
        if (document.RootElement.TryGetProperty("errors", out var errors) && errors.GetArrayLength() > 0) throw new InvalidOperationException(errors[0].GetProperty("message").GetString() ?? "Suwayomi GraphQL error");
        return document.RootElement.TryGetProperty("data", out var data) ? data.Clone() : document.RootElement.Clone();
    }
    public Task<JsonElement> LibraryAsync(CancellationToken ct) => QueryAsync("""
query($first:Int){ mangas(condition:{inLibrary:true}, first:$first){ nodes { id title author artist description genre inLibrary status thumbnailUrl sourceId unreadCount downloadCount url firstUnreadChapter { id name chapterNumber isRead lastPageRead } lastReadChapter { id name chapterNumber isRead lastPageRead } latestUploadedChapter { id name chapterNumber uploadDate isRead } chapters(first:1){ totalCount } source { id name lang iconUrl } } } }
""", new { first = 500 }, ct);
    public Task<JsonElement> MangaAsync(int id, CancellationToken ct) => QueryAsync("""
query($id:Int!){ manga(id:$id){ id title author artist description genre inLibrary status thumbnailUrl sourceId unreadCount downloadCount url firstUnreadChapter { id name chapterNumber isRead lastPageRead } latestUploadedChapter { id name chapterNumber uploadDate isRead } chapters(first:500){ nodes { id name chapterNumber isRead isDownloaded lastPageRead uploadDate } } source { id name lang iconUrl } } }
""", new { id }, ct);
    public Task<JsonElement> SourcesAsync(CancellationToken ct) => QueryAsync("query{ sources(first:500){ nodes { id name lang iconUrl contentWarning extension { pkgName name versionName isInstalled hasUpdate iconUrl } } } }");
    public Task<JsonElement> CategoriesAsync(CancellationToken ct) => QueryAsync("query{ categories(first:200){ nodes { id name order default mangas(first:1){ totalCount } } } }");
    public Task<JsonElement> ExtensionsAsync(CancellationToken ct) => QueryAsync("query{ extensions(first:500){ nodes { pkgName name lang versionName versionCodeLong isInstalled hasUpdate isObsolete iconUrl storeIndexUrl } } extensionStores(first:100){ nodes { indexUrl name isLegacy contactWebsite } } }");
    public Task<JsonElement> UpdatesAsync(CancellationToken ct) => QueryAsync("query{ libraryUpdateStatus { mangaUpdates { status manga { id title thumbnailUrl sourceId unreadCount latestUploadedChapter { id name chapterNumber uploadDate isRead } source { id name lang } } } } }");
    public Task<JsonElement> BrowseSourceAsync(string sourceId, string type, string? query, CancellationToken ct) => QueryAsync("""
mutation($input:FetchSourceMangaInput!){ fetchSourceManga(input:$input){ hasNextPage mangas { id title author artist description genre thumbnailUrl sourceId url source { id name lang iconUrl } } } }
""", new { input = new { source = sourceId, page = 1, query, type = type.ToUpperInvariant() } }, ct);
    public Task<JsonElement> ChaptersAsync(int mangaId, CancellationToken ct) => QueryAsync("query($id:Int!){ manga(id:$id){ chapters(first:500){ nodes { id name chapterNumber isRead isDownloaded lastPageRead uploadDate } } } }", new { id = mangaId }, ct);
    public Task<JsonElement> FetchPagesAsync(int chapterId, CancellationToken ct) => QueryAsync("mutation($input:FetchChapterPagesInput!){ fetchChapterPages(input:$input){ chapter { id name isRead lastPageRead } pages } }", new { input = new { chapterId } }, ct);
    public Task<JsonElement> SetFavoriteAsync(int id, bool favorite, CancellationToken ct) => QueryAsync("mutation($input:UpdateMangaInput!){ updateManga(input:$input){ manga { id inLibrary } } }", new { input = new { id, patch = new { inLibrary = favorite } } }, ct);
    public Task<JsonElement> SetChapterStateAsync(int chapterId, bool read, int? lastPageRead, CancellationToken ct) => QueryAsync("mutation($input:UpdateChapterInput!){ updateChapter(input:$input){ chapter { id isRead lastPageRead } } }", new { input = new { id = chapterId, patch = new { isRead = read, lastPageRead } } }, ct);
    public Task<JsonElement> DownloadAsync(IEnumerable<int> chapterIds, CancellationToken ct) => QueryAsync("mutation($input:EnqueueChapterDownloadsInput!){ enqueueChapterDownloads(input:$input){ downloadStatus { state queue { position progress state chapter { id name } } } } }", new { input = new { ids = chapterIds.ToArray() } }, ct);
    public Task<JsonElement> UpdateLibraryAsync(CancellationToken ct) => QueryAsync("mutation($input:UpdateLibraryInput!){ updateLibrary(input:$input){ updateStatus { jobsInfo { isRunning totalJobs finishedJobs } } } }", new { input = new { } }, ct);
    public Task<JsonElement> InstallExtensionAsync(string id, CancellationToken ct) => QueryAsync("mutation($input:UpdateExtensionInput!){ updateExtension(input:$input){ extension { pkgName name isInstalled versionName } } }", new { input = new { id, patch = new { install = true } } }, ct);
}
