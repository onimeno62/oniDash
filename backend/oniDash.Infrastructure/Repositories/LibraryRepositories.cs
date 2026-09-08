using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using oniDash.Application.Libraries;
using oniDash.Core.Domain;
using oniDash.Infrastructure.Persistence;

namespace oniDash.Infrastructure.Repositories;

public sealed class LibraryRepository(OniDashDbContext dbContext) : ILibraryRepository
{
    public async Task<IReadOnlyList<Library>> ListAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Libraries
            .AsNoTracking()
            .OrderBy(l => l.CreatedAtUtc)
            .ThenBy(l => l.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public Task<Library?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Libraries.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

    public Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default) =>
        dbContext.Libraries.AnyAsync(
            l => l.Name.ToLower() == name.ToLower() && (excludeId == null || l.Id != excludeId),
            cancellationToken);

    public async Task AddAsync(Library library, CancellationToken cancellationToken = default)
    {
        await dbContext.Libraries.AddAsync(library, cancellationToken).ConfigureAwait(false);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(Library library, CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Library library, CancellationToken cancellationToken = default)
    {
        // Bulk delete by key: no entity attach required; database FKs cascade children.
        await dbContext.Libraries
            .Where(l => l.Id == library.Id)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}

public sealed class LibrarySourceRepository(OniDashDbContext dbContext) : ILibrarySourceRepository
{
    public async Task<IReadOnlyList<LibrarySource>> ListByLibraryAsync(Guid libraryId, CancellationToken cancellationToken = default) =>
        await dbContext.Sources
            .AsNoTracking()
            .Where(s => s.LibraryId == libraryId)
            .OrderBy(s => s.CreatedAtUtc)
            .ThenBy(s => s.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public Task<LibrarySource?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Sources.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task AddAsync(LibrarySource source, CancellationToken cancellationToken = default)
    {
        await dbContext.Sources.AddAsync(source, cancellationToken).ConfigureAwait(false);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(LibrarySource source, CancellationToken cancellationToken = default)
    {
        await dbContext.Sources
            .Where(s => s.Id == source.Id)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task UpdateLastScannedAsync(
        Guid sourceId,
        DateTimeOffset scannedAtUtc,
        CancellationToken cancellationToken = default)
    {
        await dbContext.Sources
            .Where(s => s.Id == sourceId)
            .ExecuteUpdateAsync(
                updates => updates.SetProperty(s => s.LastScannedAtUtc, scannedAtUtc),
                cancellationToken)
            .ConfigureAwait(false);
    }
}

public sealed class MediaItemRepository(OniDashDbContext dbContext) : IMediaItemRepository
{
    public async Task<(IReadOnlyList<MediaItem> Items, int TotalCount)> ListByLibraryAsync(
        Guid libraryId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.MediaItems.AsNoTracking().Where(i => i.LibraryId == libraryId);
        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var items = await query
            .OrderByDescending(i => i.CreatedAtUtc)
            .ThenByDescending(i => i.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (items, totalCount);
    }

    public Task<MediaItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.MediaItems.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public async Task<MediaItem> AddPlaceholderAsync(
        Guid libraryId,
        string displayName,
        CancellationToken cancellationToken = default)
    {
        var item = new MediaItem { LibraryId = libraryId, DisplayName = displayName };
        await dbContext.MediaItems.AddAsync(item, cancellationToken).ConfigureAwait(false);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return item;
    }
}

public sealed class TagRepository(OniDashDbContext dbContext) : ITagRepository
{
    public async Task<IReadOnlyList<Tag>> ListByLibraryAsync(Guid libraryId, CancellationToken cancellationToken = default) =>
        await dbContext.Tags
            .AsNoTracking()
            .Where(t => t.LibraryId == libraryId)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public Task<Tag?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Tags.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<bool> ExistsByNameAsync(Guid libraryId, string name, CancellationToken cancellationToken = default) =>
        dbContext.Tags.AnyAsync(t => t.LibraryId == libraryId && t.Name.ToLower() == name.ToLower(), cancellationToken);

    public Task<bool> IsAssignedToItemAsync(Guid mediaItemId, Guid tagId, CancellationToken cancellationToken = default) =>
        dbContext.MediaItemTags.AnyAsync(t => t.MediaItemId == mediaItemId && t.TagId == tagId, cancellationToken);

    public async Task AddAsync(Tag tag, CancellationToken cancellationToken = default)
    {
        await dbContext.Tags.AddAsync(tag, cancellationToken).ConfigureAwait(false);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task AssignToItemAsync(Guid mediaItemId, Guid tagId, CancellationToken cancellationToken = default)
    {
        await dbContext.MediaItemTags.AddAsync(
            new MediaItemTag { MediaItemId = mediaItemId, TagId = tagId },
            cancellationToken).ConfigureAwait(false);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UnassignFromItemAsync(Guid mediaItemId, Guid tagId, CancellationToken cancellationToken = default)
    {
        await dbContext.MediaItemTags
            .Where(t => t.MediaItemId == mediaItemId && t.TagId == tagId)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task DeleteAsync(Tag tag, CancellationToken cancellationToken = default)
    {
        await dbContext.Tags
            .Where(t => t.Id == tag.Id)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}

public sealed class CollectionRepository(OniDashDbContext dbContext) : ICollectionRepository
{
    public async Task<IReadOnlyList<Collection>> ListByLibraryAsync(Guid libraryId, CancellationToken cancellationToken = default) =>
        await dbContext.Collections
            .AsNoTracking()
            .Where(c => c.LibraryId == libraryId)
            .OrderBy(c => c.CreatedAtUtc)
            .ThenBy(c => c.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public Task<Collection?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Collections.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<bool> ExistsByNameAsync(Guid libraryId, string name, CancellationToken cancellationToken = default) =>
        dbContext.Collections.AnyAsync(c => c.LibraryId == libraryId && c.Name.ToLower() == name.ToLower(), cancellationToken);

    public Task<bool> ContainsItemAsync(Guid collectionId, Guid mediaItemId, CancellationToken cancellationToken = default) =>
        dbContext.CollectionItems.AnyAsync(ci => ci.CollectionId == collectionId && ci.MediaItemId == mediaItemId, cancellationToken);

    public async Task AddAsync(Collection collection, CancellationToken cancellationToken = default)
    {
        await dbContext.Collections.AddAsync(collection, cancellationToken).ConfigureAwait(false);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task AddItemAsync(Guid collectionId, Guid mediaItemId, CancellationToken cancellationToken = default)
    {
        await dbContext.CollectionItems.AddAsync(
            new CollectionItem { CollectionId = collectionId, MediaItemId = mediaItemId },
            cancellationToken).ConfigureAwait(false);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task RemoveItemAsync(Guid collectionId, Guid mediaItemId, CancellationToken cancellationToken = default)
    {
        await dbContext.CollectionItems
            .Where(ci => ci.CollectionId == collectionId && ci.MediaItemId == mediaItemId)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<(IReadOnlyList<MediaItem> Items, int TotalCount)> ListItemsAsync(
        Guid collectionId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.CollectionItems
            .AsNoTracking()
            .Where(ci => ci.CollectionId == collectionId)
            .Select(ci => ci.MediaItem!);
        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var items = await query
            .OrderByDescending(i => i.CreatedAtUtc)
            .ThenByDescending(i => i.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (items.ToList(), totalCount);
    }

    public async Task DeleteAsync(Collection collection, CancellationToken cancellationToken = default)
    {
        await dbContext.Collections
            .Where(c => c.Id == collection.Id)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
