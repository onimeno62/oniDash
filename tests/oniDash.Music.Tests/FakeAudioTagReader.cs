using System;
using System.Collections.Generic;
using oniDash.Music.Tagging;

namespace oniDash.Music.Tests;

/// <summary>Scripted tag reader: maps absolute paths to canned AudioTags.</summary>
public sealed class FakeAudioTagReader : IAudioTagReader
{
    public Dictionary<string, AudioTags> TagsByPath { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<string> ReadCalls { get; } = [];

    public AudioTags? Read(string absolutePath)
    {
        ReadCalls.Add(absolutePath);
        return TagsByPath.TryGetValue(absolutePath, out var tags) ? tags : null;
    }
}
