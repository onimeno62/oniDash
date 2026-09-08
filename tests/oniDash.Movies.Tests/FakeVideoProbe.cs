using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using oniDash.Movies.Probing;

namespace oniDash.Movies.Tests;

/// <summary>Probe/artwork fakes keyed by file path, so no external tooling is needed.</summary>
public sealed class FakeVideoProbeReader : IVideoProbeReader
{
    public Dictionary<string, VideoProbe> ProbesByPath { get; } = new(StringComparer.OrdinalIgnoreCase);

    public Task<VideoProbe?> ProbeAsync(string absolutePath, CancellationToken cancellationToken = default) =>
        Task.FromResult(ProbesByPath.TryGetValue(absolutePath, out var probe) ? probe : null);
}

public sealed class FakeVideoArtworkReader : IVideoArtworkReader
{
    public Dictionary<string, VideoArtwork> ArtworkByPath { get; } = new(StringComparer.OrdinalIgnoreCase);

    public Task<VideoArtwork?> ReadAsync(string absolutePath, CancellationToken cancellationToken = default) =>
        Task.FromResult(ArtworkByPath.TryGetValue(absolutePath, out var artwork) ? artwork : null);
}
