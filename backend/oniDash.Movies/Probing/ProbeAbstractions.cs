using System;
using System.Threading;
using System.Threading.Tasks;

namespace oniDash.Movies.Probing;

/// <summary>Probe-derived metadata for one video file. Nulls mean "unknown".</summary>
public sealed record VideoProbe(double? DurationSeconds, int? Width, int? Height);

/// <summary>Artwork bytes found in or derived from one video file, if any.</summary>
public sealed record VideoArtwork(byte[] Bytes, string ContentType);

/// <summary>Reads duration/geometry from a video file without touching its bytes' content.</summary>
public interface IVideoProbeReader
{
    Task<VideoProbe?> ProbeAsync(string absolutePath, CancellationToken cancellationToken = default);
}

/// <summary>Extracts (or refuses) embedded artwork for a video file.</summary>
public interface IVideoArtworkReader
{
    Task<VideoArtwork?> ReadAsync(string absolutePath, CancellationToken cancellationToken = default);
}
