using System;
using System.Threading;
using System.Threading.Tasks;

namespace oniDash.Music.Cataloging;

/// <summary>Where an indexed file's bytes live and how to describe them to a player.</summary>
public sealed record AudioFileLocation(string AbsolutePath, string ContentType);

/// <summary>
/// Resolves an indexed file row to a streamable location. Implemented inside the music
/// plugin against the Application repository abstractions, so the plugin never touches
/// the filesystem directly nor the core's persistence internals (rules 3/6).
/// </summary>
public interface IMediaFileLocator
{
    Task<AudioFileLocation?> LocateAsync(Guid fileId, CancellationToken cancellationToken = default);
}
