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

public interface IMediaItemResolver
{
    Task<Guid> ResolveForNewFileAsync(LibrarySource source, DiscoveredFile file, CancellationToken cancellationToken = default);
}

public sealed class PlaceholderMediaItemResolver(IMediaItemRepository items) : IMediaItemResolver
{
    public async Task<Guid> ResolveForNewFileAsync(LibrarySource source, DiscoveredFile file, CancellationToken cancellationToken = default)
    {
        var fileName = file.RelativePath.Split('/').LastOrDefault() ?? file.RelativePath;
        var stem = fileName.Contains('.') ? fileName[..fileName.LastIndexOf('.')]: fileName;
        var item = await items.AddPlaceholderAsync(source.LibraryId, stem, cancellationToken).ConfigureAwait(false);
        return item.Id;
    }
}

public enum IndexedMediaChangeKind { New, Updated, Unchanged }

public sealed record IndexedMediaContext(LibrarySource Source, DiscoveredFile File, Guid FileRowId, Guid MediaItemId, IndexedMediaChangeKind ChangeKind)
{
    public string AbsolutePath => Path.Combine(Source.RootPath, File.RelativePath.Replace('/', Path.DirectorySeparatorChar));
}

public interface IIndexedMediaHandler
{
    Task HandleAsync(IndexedMediaContext context, CancellationToken cancellationToken = default);
}

/// <summary>Discovery is completed before writes; individual enrichment failures are isolated as diagnostics.</summary>
public sealed class ScanService(
    ILibrarySourceRepository sourceRepository,
    IMediaFileRepository fileRepository,
    IFileEnumerator fileEnumerator,
    IMediaItemResolver itemResolver,
    IEnumerable<IIndexedMediaHandler> mediaHandlers) : IScanService
{
    private const int DiscoveryProgressBatch = 100;
    private const int IndexingProgressBatch = 50;

    public async Task<ScanOutcome> ScanSourceAsync(Guid sourceId, Action<ScanProgressUpdate>? onProgress = null, CancellationToken cancellationToken = default)
    {
        var source = await sourceRepository.GetByIdAsync(sourceId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("Library source", sourceId);
        var existing = (await fileRepository.ListBySourceAsync(sourceId, cancellationToken).ConfigureAwait(false))
            .ToDictionary(file => file.IdentityKey, StringComparer.Ordinal);
        var seenKeys = new HashSet<string>(StringComparer.Ordinal);
        var handlers = (mediaHandlers ?? []).ToArray();
        var diagnostics = new List<ScanDiagnostic>();
        long discovered = 0, indexed = 0, updated = 0, unchanged = 0;
        var discoveredFiles = new List<DiscoveredFile>();

        cancellationToken.ThrowIfCancellationRequested();
        await foreach (var file in fileEnumerator.EnumerateAsync(source.RootPath, ScanFilterOptions.Default, cancellationToken).ConfigureAwait(false))
        {
            discoveredFiles.Add(file);
            discovered++;
            if (discovered % DiscoveryProgressBatch == 0)
                onProgress?.Invoke(new ScanProgressUpdate(ScanPhase.Discovering, discovered, 0, 0, 0, 0, 0));
        }

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
                mediaItemId = await itemResolver.ResolveForNewFileAsync(source, file, cancellationToken).ConfigureAwait(false);
                row = new MediaFile { MediaItemId = mediaItemId, LibrarySourceId = sourceId, RelativePath = file.RelativePath,
                    IdentityKey = identityKey, Extension = file.Extension, SizeBytes = file.SizeBytes, LastWriteTimeUtc = file.LastWriteTimeUtc };
                await fileRepository.AddAsync(row, cancellationToken).ConfigureAwait(false);
                indexed++; changeKind = IndexedMediaChangeKind.New; fileRowId = row.Id;
            }
            else if (row.SizeBytes != file.SizeBytes || row.LastWriteTimeUtc != file.LastWriteTimeUtc || row.MissingSinceUtc is not null)
            {
                row.SizeBytes = file.SizeBytes; row.LastWriteTimeUtc = file.LastWriteTimeUtc; row.MissingSinceUtc = null;
                await fileRepository.UpdateAsync(row, cancellationToken).ConfigureAwait(false);
                updated++; changeKind = IndexedMediaChangeKind.Updated; fileRowId = row.Id; mediaItemId = row.MediaItemId;
            }
            else { unchanged++; changeKind = IndexedMediaChangeKind.Unchanged; fileRowId = row.Id; mediaItemId = row.MediaItemId; }

            var context = new IndexedMediaContext(source, file, fileRowId, mediaItemId, changeKind);
            foreach (var handler in handlers)
            {
                try { await handler.HandleAsync(context, cancellationToken).ConfigureAwait(false); }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    diagnostics.Add(new ScanDiagnostic(file.RelativePath, ScanDiagnosticSeverity.Error,
                        "HANDLER_FAILED", $"Media handler '{handler.GetType().Name}' failed: {ex.Message}"));
                }
            }
            processed++;
            if (processed % IndexingProgressBatch == 0)
                onProgress?.Invoke(new ScanProgressUpdate(ScanPhase.Indexing, discovered, processed, indexed, updated, unchanged, 0));
        }

        cancellationToken.ThrowIfCancellationRequested();
        var missingIds = existing.Values.Where(row => !seenKeys.Contains(row.IdentityKey) && row.MissingSinceUtc is null).Select(row => row.Id).ToList();
        if (missingIds.Count > 0) await fileRepository.MarkMissingAsync(missingIds, DateTimeOffset.UtcNow, cancellationToken).ConfigureAwait(false);
        await sourceRepository.UpdateLastScannedAsync(sourceId, DateTimeOffset.UtcNow, cancellationToken).ConfigureAwait(false);
        onProgress?.Invoke(new ScanProgressUpdate(ScanPhase.Done, discovered, discovered, indexed, updated, unchanged, missingIds.Count));
        return new ScanOutcome(discovered, indexed, updated, unchanged, missingIds.Count, DateTimeOffset.UtcNow, diagnostics);
    }

    private static string BuildIdentityKey(Guid sourceId, string relativePath) => $"{sourceId:N}/{relativePath.ToLowerInvariant()}";
}
