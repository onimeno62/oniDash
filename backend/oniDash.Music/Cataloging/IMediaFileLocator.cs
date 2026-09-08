using System;
using System.Threading;
using System.Threading.Tasks;

namespace oniDash.Music.Cataloging;

// AudioFileLocation lives in AudioFileLocator.cs; this file only adds the locator
// abstraction so the player/endpoints can depend on it instead of the concrete class.

/// <summary>
/// Resolves an indexed file row to a streamable location. Implemented inside the music
/// plugin against the Application repository abstractions, so the plugin never touches
/// the filesystem directly nor the core's persistence internals (rules 3/6).
/// </summary>
public interface IMediaFileLocator
{
    Task<AudioFileLocation?> LocateAsync(Guid fileId, CancellationToken cancellationToken = default);
}
