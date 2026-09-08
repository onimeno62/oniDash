using System;
using System.Threading;
using System.Threading.Tasks;
using oniDash.Core.Domain;

namespace oniDash.Application.Libraries;

/// <summary>Persistence abstraction for libraries. Implemented by Infrastructure.</summary>
public interface ILibraryRepository
{
    Task<IReadOnlyList<Library>> ListAsync(CancellationToken cancellationToken = default);

    Task<Library?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default);

    Task AddAsync(Library library, CancellationToken cancellationToken = default);

    Task UpdateAsync(Library library, CancellationToken cancellationToken = default);

    Task DeleteAsync(Library library, CancellationToken cancellationToken = default);
}

/// <summary>Persistence abstraction for configured folder sources.</summary>
public interface ILibrarySourceRepository
{
    Task<IReadOnlyList<LibrarySource>> ListByLibraryAsync(Guid libraryId, CancellationToken cancellationToken = default);

    Task<LibrarySource?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(LibrarySource source, CancellationToken cancellationToken = default);

    Task DeleteAsync(LibrarySource source, CancellationToken cancellationToken = default);

    /// <summary>Stamps the source as successfully scanned (used by the scan pass).</summary>
    Task UpdateLastScannedAsync(Guid sourceId, DateTimeOffset scannedAtUtc, CancellationToken cancellationToken = default);
}

/// <summary>
/// Index rows for physical files. The scanner upserts rows and marks missing files;
/// rows are never deleted by scans (user tags/collections must survive re-scans).
/// </summary>
public interface IMediaFileRepository
{
    Task<IReadOnlyList<MediaFile>> ListBySourceAsync(Guid sourceId, CancellationToken cancellationToken = default);

    Task<MediaFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(MediaFile file, CancellationToken cancellationToken = default);

    Task UpdateAsync(MediaFile file, CancellationToken cancellationToken = default);

    /// <summary>Marks the given file rows as missing since the given instant.</summary>
    Task MarkMissingAsync(IReadOnlyCollection<Guid> fileIds, DateTimeOffset missingSinceUtc, CancellationToken cancellationToken = default);
}

/// <summary>Read access to media items (items are created by the Phase 3 scanner).</summary>
public interface IMediaItemRepository
{
    Task<(IReadOnlyList<MediaItem> Items, int TotalCount)> ListByLibraryAsync(
        Guid libraryId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<MediaItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a bare item that newly discovered files attach to. Catalogue plugins rename
    /// and re-group these placeholders in later milestones.
    /// </summary>
    Task<MediaItem> AddPlaceholderAsync(Guid libraryId, string displayName, CancellationToken cancellationToken = default);
}

/// <summary>Persistence abstraction for tags and their assignment to media items.</summary>
public interface ITagRepository
{
    Task<IReadOnlyList<Tag>> ListByLibraryAsync(Guid libraryId, CancellationToken cancellationToken = default);

    Task<Tag?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsByNameAsync(Guid libraryId, string name, CancellationToken cancellationToken = default);

    Task<bool> IsAssignedToItemAsync(Guid mediaItemId, Guid tagId, CancellationToken cancellationToken = default);

    Task AddAsync(Tag tag, CancellationToken cancellationToken = default);

    Task AssignToItemAsync(Guid mediaItemId, Guid tagId, CancellationToken cancellationToken = default);

    Task UnassignFromItemAsync(Guid mediaItemId, Guid tagId, CancellationToken cancellationToken = default);

    Task DeleteAsync(Tag tag, CancellationToken cancellationToken = default);
}

/// <summary>Persistence abstraction for collections and their contents.</summary>
public interface ICollectionRepository
{
    Task<IReadOnlyList<Collection>> ListByLibraryAsync(Guid libraryId, CancellationToken cancellationToken = default);

    Task<Collection?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsByNameAsync(Guid libraryId, string name, CancellationToken cancellationToken = default);

    Task<bool> ContainsItemAsync(Guid collectionId, Guid mediaItemId, CancellationToken cancellationToken = default);

    Task AddAsync(Collection collection, CancellationToken cancellationToken = default);

    Task AddItemAsync(Guid collectionId, Guid mediaItemId, CancellationToken cancellationToken = default);

    Task RemoveItemAsync(Guid collectionId, Guid mediaItemId, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<MediaItem> Items, int TotalCount)> ListItemsAsync(
        Guid collectionId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(Collection collection, CancellationToken cancellationToken = default);
}
