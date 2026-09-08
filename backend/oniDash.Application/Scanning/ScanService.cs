using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using oniDash.Application.Common;
using oniDash.Application.Libraries;
using oniDash.Core.Domain;

namespace oniDash.Application.Scanning;

/// <summary>
/// Assigns a media item to a newly discovered file (identity/dedup groundwork). The
/// default implementation creates one placeholder item per new file row; catalogue
/// plugins rename and re-group placeholders in later milestones (capability-based,
/// Architecture rule 6).
/// </summary>
public interface IMediaItemResolver
{
    Task<Guid> ResolveForNewFileAsync(LibrarySource source, DiscoveredFile file, CancellationToken cancellationToken = default);
}

/// <summary>Default resolver: one placeholder media item per new file, named after the file.</summary>
public sealed class PlaceholderMediaItemResolver(IMediaItemRepository items) : IMediaItemResolver
{
    public async Task<Guid> ResolveForNewFileAsync(
        LibrarySource source,
        DiscoveredFile file,
        CancellationToken cancellationToken = default)
    {
        var fileName = file.RelativePath.Split('/').LastOrDefault() ?? file.RelativePath;
        var stem = fileName.Contains('.') ? fileName[..fileName.LastIndexOf('.')] : fileName;
        var item = await items.AddPlaceholderAsync(source.LibraryId, stem, cancellationToken).ConfigureAwait(false);
        return item.Id;
    }
}

/// <summary>How the current scan pass classified one discovered file.</summary>
public enum IndexedMediaChangeKind
{
    /// <summary>The file was not in the index before; a new placeholder item was created.</summary>
    New,

    /// <summary>The file existed but changed (size/timestamp) or had been missing.</summary>
    Updated,

    /// <summary>The file existed with identical size and timestamp.</summary>
    Unchanged,
}

/// <summary>
/// Everything a catalogue plugin needs to enrich one indexed file. Strictly read-only
/// with respect to the filesystem (Architecture rule 11).
/// </summary>
public sealed record IndexedMediaContext(
    LibrarySource Source,
    DiscoveredFile File,
    Guid FileRowId,
    Guid MediaItemId,
    IndexedMediaChangeKind ChangeKind)
{
    public string AbsolutePath => Path.Combine(Source.RootPath, File.RelativePath.Replace('/', Path.DirectorySeparatorChar));
}

/// <summary>
/// Capability hook (Architecture rule 6) for catalogue plugins to enrich indexed files —
/// read embedded metadata, group items into artists/albums, extract artwork. Invoked
/// during the scan's index phase after the file row is persisted; handlers receive every
/// file (also non-audio ones) and filter by extension themselves. Handler failures must
/// not fail the scan: implementations decide their own per-file error policy.
/// </summary>
public interface IIndexedMediaHandler
{
    Task HandleAsync(IndexedMediaContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// One full scan pass over a source. Enumerates the filesystem through
/// <see cref="IFileEnumerator"/>, upserts <see cref="MediaFile"/> index rows, marks files
/// that disappeared as missing (never deletes index rows or touches user files), and
/// stamps the source's LastScannedAtUtc on success. Cancelled or failed passes leave the
/// previous index intact: discovery completes before any write.
/// </summary>
public sealed class ScanService(
    ILibrarySourceRepository sourceRepository,
    IMediaFileRepository fileRepository,
    IFileEnumerator fileEnumerator,
    IMediaItemResolver itemResolver,
    IEnumerable<IIndexedMediaHandler> mediaHandlers) : IScanService
{
    private const int DiscoveryProgressBatch = 100;
    private const int IndexingProgressBatch = 50;

    public async Task<ScanOutcome> ScanSourceAsync(
        Guid sourceId,
        Action<ScanProgressUpdate>? onProgress = null,
        CancellationToken cancellationToken = default)
    {
        var source = await sourceRepository.GetByIdAsync(sourceId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("Library source", sourceId);

        var filter = ScanFilterOptions.Default;
        var existing = (await fileRepository.ListBySourceAsync(sourceId, cancellationToken).ConfigureAwait(false))
            .ToDictionary(file => file.IdentityKey, StringComparer.Ordinal);
        var seenKeys = new HashSet<string>(StringComparer.Ordinal);
        var handlers = (mediaHandlers ?? []).ToArray();

        long discovered = 0, indexed = 0, updated = 0, unchanged = 0;
        var discoveredFiles = new List<DiscoveredFile>();

        // Phase 1: discover. Nothing is written until enumeration finishes, so a
        // cancelled or failed pass leaves the previous index untouched.
        cancellationToken.ThrowIfCancellationRequested();
        await foreach (var file in fileEnumerator
            .EnumerateAsync(source.RootPath, filter, cancellationToken)
            .ConfigureAwait(false))
        {
            discoveredFiles.Add(file);
            discovered++;
            if (discovered % DiscoveryProgressBatch == 0)
            {
                onProgress?.Invoke(new ScanProgressUpdate(
                    ScanPhase.Discovering, discovered, 0, 0, 0, 0, 0));
            }
        }

        // Phase 2: index (upsert). New rows are added; changed or previously-missing rows
        // are refreshed; identical rows are left untouched. Existing rows keep their
        // MediaItemId, so user tags/collections survive re-scans. Registered catalogue
        // handlers enrich each row after it is persisted.
        cancellationToken.ThrowIfCancellationRequested();
        var processed = 0L;
        foreach (var file in discoveredFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var identityKey = BuildIdentityKey(sourceId, file.RelativePath);
            seenKeys.Add(identityKey);

            IndexedMediaChangeKind changeKind;
            Guid mediaItemId;
            Guid fileRowId;
            if (!existing.TryGetValue(identityKey, out var row))
            {
                mediaItemId = await itemResolver
                    .ResolveForNewFileAsync(source, file, cancellationToken)
                    .ConfigureAwait(false);
                row = new MediaFile
                {
                    MediaItemId = mediaItemId,
                    LibrarySourceId = sourceId,
                    RelativePath = file.RelativePath,
                    IdentityKey = identityKey,
                    Extension = file.Extension,
                    SizeBytes = file.SizeBytes,
                    LastWriteTimeUtc = file.LastWriteTimeUtc,
                };
                await fileRepository.AddAsync(row, cancellationToken).ConfigureAwait(false);
                indexed++;
                changeKind = IndexedMediaChangeKind.New;
                fileRowId = row.Id;
            }
            else if (row.SizeBytes != file.SizeBytes
                || row.LastWriteTimeUtc != file.LastWriteTimeUtc
                || row.MissingSinceUtc is not null)
            {
                row.SizeBytes = file.SizeBytes;
                row.LastWriteTimeUtc = file.LastWriteTimeUtc;
                row.MissingSinceUtc = null;
                await fileRepository.UpdateAsync(row, cancellationToken).ConfigureAwait(false);
                updated++;
                changeKind = IndexedMediaChangeKind.Updated;
                fileRowId = row.Id;
                mediaItemId = row.MediaItemId;
            }
            else
            {
                unchanged++;
                changeKind = IndexedMediaChangeKind.Unchanged;
                fileRowId = row.Id;
                mediaItemId = row.MediaItemId;
            }

            if (handlers.Length > 0)
            {
                var context = new IndexedMediaContext(source, file, fileRowId, mediaItemId, changeKind);
                foreach (var handler in handlers)
                {
                    await handler.HandleAsync(context, cancellationToken).ConfigureAwait(false);
                }
            }

            processed++;
            if (processed % IndexingProgressBatch == 0)
            {
                onProgress?.Invoke(new ScanProgressUpdate(
                    ScanPhase.Indexing, discovered, processed, indexed, updated, unchanged, 0));
            }
        }

        // Phase 3: reconcile — rows the scan no longer finds are marked missing, keeping
        // their media items (and any user-applied tags/collections) for later phases.
        cancellationToken.ThrowIfCancellationRequested();
        var missingIds = existing.Values
            .Where(row => !seenKeys.Contains(row.IdentityKey) && row.MissingSinceUtc is null)
            .Select(row => row.Id)
            .ToList();
        if (missingIds.Count > 0)
        {
            await fileRepository.MarkMissingAsync(missingIds, DateTimeOffset.UtcNow, cancellationToken)
                .ConfigureAwait(false);
        }

        await sourceRepository.UpdateLastScannedAsync(sourceId, DateTimeOffset.UtcNow, cancellationToken)
            .ConfigureAwait(false);

        onProgress?.Invoke(new ScanProgressUpdate(
            ScanPhase.Done, discovered, discovered, indexed, updated, unchanged, missingIds.Count));

        return new ScanOutcome(
            discovered, indexed, updated, unchanged, missingIds.Count, DateTimeOffset.UtcNow);
    }

    /// <summary>Stable, case-normalized identity: source id + lowercase '/'-separated path.</summary>
    private static string BuildIdentityKey(Guid sourceId, string relativePath) =>
        $"{sourceId:N}/{relativePath.ToLowerInvariant()}";
}
