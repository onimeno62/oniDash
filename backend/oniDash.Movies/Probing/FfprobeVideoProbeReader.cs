using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace oniDash.Movies.Probing;

/// <summary>
/// ffprobe-based implementation. Runs the local ffprobe executable read-only against
/// the indexed file (rule 11: never modifies user media). When ffprobe is not on PATH
/// the reader degrades gracefully — probes return null and cataloguing falls back to
/// filename metadata only (rule 7: core functions work without extra tooling).
/// </summary>
public sealed partial class FfprobeVideoProbeReader(ILogger<FfprobeVideoProbeReader> logger) : IVideoProbeReader
{
    public async Task<VideoProbe?> ProbeAsync(string absolutePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "ffprobe",
                ArgumentList =
                {
                    "-v", "error",
                    "-print_format", "json",
                    "-show_format",
                    "-show_streams",
                    "--", absolutePath,
                },
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            using var process = Process.Start(psi);
            if (process is null)
            {
                return null;
            }

            var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            await process.StandardError.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            if (process.ExitCode != 0)
            {
                logger.LogDebug("ffprobe exited {ExitCode} for {Path}", process.ExitCode, absolutePath);
                return null;
            }

            var json = await stdoutTask.ConfigureAwait(false);
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            double? duration = null;
            if (root.TryGetProperty("format", out var format)
                && format.TryGetProperty("duration", out var formatDuration)
                && double.TryParse(formatDuration.GetString(), out var seconds))
            {
                duration = seconds;
            }

            int? width = null;
            int? height = null;
            if (root.TryGetProperty("streams", out var streams))
            {
                foreach (var stream in streams.EnumerateArray())
                {
                    if (stream.TryGetProperty("codec_type", out var type)
                        && type.GetString() == "video"
                        && stream.TryGetProperty("width", out var w)
                        && stream.TryGetProperty("height", out var h))
                    {
                        width = w.GetInt32();
                        height = h.GetInt32();
                        break;
                    }
                }
            }

            return duration is null && width is null
                ? null
                : new VideoProbe(duration, width, height);
        }
        catch (Exception ex) when (
            ex is not OperationCanceledException and not OutOfMemoryException)
        {
            // Missing executable (Win32Exception), unreadable file, bad JSON: degrade.
            logger.LogDebug(ex, "Probe failed for {Path}; metadata falls back to the filename", absolutePath);
            return null;
        }
    }
}
