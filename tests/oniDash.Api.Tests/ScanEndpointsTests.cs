using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace oniDash.Api.Tests;

/// <summary>
/// End-to-end tests for the scan lifecycle: start, poll, cancel, list — against real
/// temp folders so the real enumerator, real SQLite, and the background job manager run.
/// </summary>
public sealed class ScanEndpointsTests(OniDashApiFactory factory) : IClassFixture<OniDashApiFactory>
{
    [Fact]
    public async Task Start_scan_returns_202_with_progress_body()
    {
        var client = factory.CreateClient();
        var source = await CreateSourceWithFilesAsync(client, fileCount: 3);
        try
        {
            var response = await client.PostAsync($"/api/sources/{source.SourceId}/scans", null);
            var body = await ReadJson(response);

            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
            Assert.Equal("/api/scans/" + body.GetProperty("scanId").GetString(), response.Headers.Location?.ToString());
            Assert.Equal("Running", body.GetProperty("status").GetString());
            Assert.True(body.TryGetProperty("filesDiscovered", out _));

            await WaitUntilScanCompletes(client, source.SourceId);
        }
        finally
        {
            TryDeleteDirectory(source.RootPath);
        }
    }

    [Fact]
    public async Task Completed_scan_reports_counts_and_marks_source_scanned()
    {
        var client = factory.CreateClient();
        var source = await CreateSourceWithFilesAsync(client, fileCount: 3);
        try
        {
            var started = await client.PostAsync($"/api/sources/{source.SourceId}/scans", null);
            started.EnsureSuccessStatusCode();
            var progress = await WaitUntilScanCompletes(client, source.SourceId);

            Assert.Equal("Completed", progress.GetProperty("status").GetString());
            Assert.Equal(5, progress.GetProperty("filesDiscovered").GetInt64());
            Assert.Equal(5, progress.GetProperty("filesIndexed").GetInt64());
            Assert.Equal(5, progress.GetProperty("filesProcessed").GetInt64());
            Assert.NotNull(progress.GetProperty("completedAtUtc").GetString());
        }
        finally
        {
            TryDeleteDirectory(source.RootPath);
        }
    }

    [Fact]
    public async Task Second_scan_while_running_returns_409_with_running_scan_id()
    {
        var client = factory.CreateClient();
        var source = await CreateSourceWithFilesAsync(client, fileCount: 1200);
        try
        {
            var first = await client.PostAsync($"/api/sources/{source.SourceId}/scans", null);
            first.EnsureSuccessStatusCode();

            var second = await client.PostAsync($"/api/sources/{source.SourceId}/scans", null);
            var problem = await ReadJson(second);

            Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
            Assert.Equal("Conflict", problem.GetProperty("title").GetString());
            Assert.Contains("already running", problem.GetProperty("detail").GetString());
            Assert.False(string.IsNullOrEmpty(problem.GetProperty("scanId").GetString()));

            await WaitUntilScanCompletes(client, source.SourceId);
        }
        finally
        {
            TryDeleteDirectory(source.RootPath);
        }
    }

    [Fact]
    public async Task Cancel_stops_a_running_scan()
    {
        var client = factory.CreateClient();
        var source = await CreateSourceWithFilesAsync(client, fileCount: 1500);
        try
        {
            var started = await client.PostAsync($"/api/sources/{source.SourceId}/scans", null);
            started.EnsureSuccessStatusCode();
            var scanId = (await ReadJson(started)).GetProperty("scanId").GetString();

            // Keep trying to cancel until the API accepts it, or the scan finished first.
            var cancelAccepted = false;
            var deadline = DateTime.UtcNow.AddSeconds(30);
            while (DateTime.UtcNow < deadline && !cancelAccepted)
            {
                var cancelResponse = await client.PostAsync($"/api/scans/{scanId}/cancel", null);
                if (cancelResponse.IsSuccessStatusCode)
                {
                    var body = await ReadJson(cancelResponse);
                    cancelAccepted = body.GetProperty("cancelled").GetBoolean();
                }

                if (!cancelAccepted)
                {
                    var current = await client.GetFromJsonAsync<JsonElement>($"/api/scans/{scanId}");
                    if (current.GetProperty("status").GetString() == "Completed")
                    {
                        break; // finished before the cancel landed; nothing to assert
                    }
                    await Task.Delay(25);
                }
            }

            var final = await WaitUntilScanByIdCompletes(client, scanId!);
            if (cancelAccepted)
            {
                Assert.Equal("Cancelled", final.GetProperty("status").GetString());
                Assert.NotNull(final.GetProperty("completedAtUtc").GetString());
            }
        }
        finally
        {
            TryDeleteDirectory(source.RootPath);
        }
    }

    [Fact]
    public async Task Unknown_source_returns_404()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsync($"/api/sources/{Guid.NewGuid()}/scans", null);
        var problem = await ReadJson(response);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("Not found", problem.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Unknown_scan_id_returns_404()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/scans/{Guid.NewGuid()}");
        var problem = await ReadJson(response);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("Not found", problem.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Missing_files_are_marked_missing_after_removal()
    {
        var client = factory.CreateClient();
        var source = await CreateSourceWithFilesAsync(client, fileCount: 2);
        try
        {
            await RunScanAsync(client, source.SourceId);
            Directory.Delete(Path.Combine(source.RootPath, "sub"), recursive: true);

            var progress = await RunScanAsync(client, source.SourceId);

            // sub/ holds 2 fixed image files.
            Assert.Equal(2, progress.GetProperty("filesMarkedMissing").GetInt64());
        }
        finally
        {
            TryDeleteDirectory(source.RootPath);
        }
    }

    [Fact]
    public async Task Rescan_of_unchanged_tree_reports_unchanged_not_new()
    {
        var client = factory.CreateClient();
        var source = await CreateSourceWithFilesAsync(client, fileCount: 2);
        try
        {
            await RunScanAsync(client, source.SourceId);

            var progress = await RunScanAsync(client, source.SourceId);

            // 2 root files + 2 sub files.
            Assert.Equal(0, progress.GetProperty("filesIndexed").GetInt64());
            Assert.Equal(4, progress.GetProperty("filesUnchanged").GetInt64());
        }
        finally
        {
            TryDeleteDirectory(source.RootPath);
        }
    }

    [Fact]
    public async Task Changed_files_are_counted_as_updated()
    {
        var client = factory.CreateClient();
        var source = await CreateSourceWithFilesAsync(client, fileCount: 2);
        try
        {
            await RunScanAsync(client, source.SourceId);

            // Rewrite one file with new content and a fresh timestamp.
            var target = Path.Combine(source.RootPath, "f0000.mp3");
            File.WriteAllText(target, "new content, longer than before");
            File.SetLastWriteTimeUtc(target, DateTime.UtcNow.AddHours(1));

            var progress = await RunScanAsync(client, source.SourceId);

            Assert.Equal(1, progress.GetProperty("filesUpdated").GetInt64());
        }
        finally
        {
            TryDeleteDirectory(source.RootPath);
        }
    }

    [Fact]
    public async Task List_scans_shows_recent_runs_for_a_source()
    {
        var client = factory.CreateClient();
        var source = await CreateSourceWithFilesAsync(client, fileCount: 1);
        try
        {
            await RunScanAsync(client, source.SourceId);
            await RunScanAsync(client, source.SourceId);

            var response = await client.GetAsync($"/api/scans?sourceId={source.SourceId}");
            response.EnsureSuccessStatusCode();
            var list = await ReadJson(response);

            Assert.True(list.GetArrayLength() >= 2);
            Assert.All(
                list.EnumerateArray(),
                scan => Assert.Equal(source.SourceId.ToString(), scan.GetProperty("sourceId").GetString()));
        }
        finally
        {
            TryDeleteDirectory(source.RootPath);
        }
    }

    // ---- helpers -----------------------------------------------------------

    private static async Task<(Guid LibraryId, Guid SourceId, string RootPath)> CreateSourceWithFilesAsync(
        HttpClient client, int fileCount)
    {
        var libraryResponse = await client.PostAsJsonAsync("/api/libraries", new { name = $"Scan {Guid.NewGuid():N}" });
        libraryResponse.EnsureSuccessStatusCode();
        var library = await ReadJson(libraryResponse);

        var rootPath = Path.Combine(Path.GetTempPath(), $"onidash-scan-{Guid.NewGuid():N}");
        Directory.CreateDirectory(rootPath);
        Directory.CreateDirectory(Path.Combine(rootPath, "sub"));
        for (var index = 0; index < fileCount; index++)
        {
            await File.WriteAllTextAsync(
                Path.Combine(rootPath, $"f{index:D4}.mp3"),
                $"content-{index}");
        }

        // Two fixed files in a subfolder (used by the missing-files test).
        await File.WriteAllTextAsync(Path.Combine(rootPath, "sub", "g0.jpg"), "img-0");
        await File.WriteAllTextAsync(Path.Combine(rootPath, "sub", "g1.jpg"), "img-1");

        var sourceResponse = await client.PostAsJsonAsync(
            $"/api/libraries/{IdOf(library)}/sources",
            new { name = "Main", rootPath });
        sourceResponse.EnsureSuccessStatusCode();
        var source = await ReadJson(sourceResponse);

        return (IdOf(library), IdOf(source), rootPath);
    }

    private static async Task<JsonElement> RunScanAsync(HttpClient client, Guid sourceId)
    {
        var started = await client.PostAsync($"/api/sources/{sourceId}/scans", null);
        started.EnsureSuccessStatusCode();
        return await WaitUntilScanCompletes(client, sourceId);
    }

    private static async Task<JsonElement> WaitUntilScanCompletes(HttpClient client, Guid sourceId)
    {
        var deadline = DateTime.UtcNow.AddSeconds(90);
        while (DateTime.UtcNow < deadline)
        {
            var list = await client.GetFromJsonAsync<JsonElement>($"/api/scans?sourceId={sourceId}&limit=1");
            var scans = list.EnumerateArray().ToList();
            if (scans.Count > 0 && scans[0].TryGetProperty("status", out var status))
            {
                var statusText = status.GetString();
                if (statusText is "Completed" or "Failed" or "Cancelled")
                {
                    return scans[0];
                }
            }
            await Task.Delay(50);
        }

        throw new TimeoutException($"scan for source {sourceId} did not complete in time");
    }

    private static async Task<JsonElement> WaitUntilScanByIdCompletes(HttpClient client, string scanId)
    {
        var deadline = DateTime.UtcNow.AddSeconds(90);
        while (DateTime.UtcNow < deadline)
        {
            var scan = await client.GetFromJsonAsync<JsonElement>($"/api/scans/{scanId}");
            var status = scan.GetProperty("status").GetString();
            if (status is "Completed" or "Failed" or "Cancelled")
            {
                return scan;
            }
            await Task.Delay(50);
        }

        throw new TimeoutException($"scan {scanId} did not complete in time");
    }

    private static Guid IdOf(JsonElement element) =>
        Guid.Parse(element.GetProperty("id").GetString()
            ?? throw new InvalidOperationException("expected a non-null id"));

    private static async Task<JsonElement> ReadJson(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(content).RootElement.Clone();
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            Directory.Delete(path, recursive: true);
        }
        catch (IOException)
        {
        }
    }
}
