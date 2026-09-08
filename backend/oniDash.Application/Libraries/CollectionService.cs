using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using oniDash.Application.Common;
using oniDash.Core.Domain;

namespace oniDash.Application.Libraries;

public interface ICollectionService
{
    Task<IReadOnlyList<CollectionDto>> ListAsync(Guid libraryId, CancellationToken cancellationToken = default);

    Task<CollectionDto> CreateAsync(Guid libraryId, CreateCollectionRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid collectionId, CancellationToken cancellationToken = default);

    Task AddItemAsync(Guid collectionId, AddCollectionItemRequest request, CancellationToken cancellationToken = default);

    Task RemoveItemAsync(Guid collectionId, Guid mediaItemId, CancellationToken cancellationToken = default);

    Task<PagedResult<MediaItemDto>> ListItemsAsync(
        Guid collectionId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}

public sealed class CollectionService(
    ILibraryRepository libraryRepository,
    IMediaItemRepository mediaItemRepository,
    ICollectionRepository collectionRepository) : ICollectionService
{
    private const int MaxNameLength = 200;

    public async Task<IReadOnlyList<CollectionDto>> ListAsync(Guid libraryId, CancellationToken cancellationToken = default)
    {
        _ = await RequireLibraryAsync(libraryId, cancellationToken).ConfigureAwait(false);
        var collections = await collectionRepository.ListByLibraryAsync(libraryId, cancellationToken).ConfigureAwait(false);
        return collections.Select(ToDto).ToList();
    }

    public async Task<CollectionDto> CreateAsync(Guid libraryId, CreateCollectionRequest request, CancellationToken cancellationToken = default)
    {
        _ = await RequireLibraryAsync(libraryId, cancellationToken).ConfigureAwait(false);

        var name = NormalizeName(request?.Name);
        if (await collectionRepository.ExistsByNameAsync(libraryId, name, cancellationToken).ConfigureAwait(false))
        {
            throw new ConflictException($"A collection named '{name}' already exists in this library.");
        }

        var collection = new Core.Domain.Collection { LibraryId = libraryId, Name = name };
        await collectionRepository.AddAsync(collection, cancellationToken).ConfigureAwait(false);
        return ToDto(collection);
    }

    public async Task DeleteAsync(Guid collectionId, CancellationToken cancellationToken = default)
    {
        var collection = await collectionRepository.GetByIdAsync(collectionId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("Collection", collectionId);

        await collectionRepository.DeleteAsync(collection, cancellationToken).ConfigureAwait(false);
    }

    public async Task AddItemAsync(Guid collectionId, AddCollectionItemRequest request, CancellationToken cancellationToken = default)
    {
        var collection = await collectionRepository.GetByIdAsync(collectionId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("Collection", collectionId);

        var item = await mediaItemRepository
            .GetByIdAsync(RequireItemId(request), cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException("Media item", request!.MediaItemId);

        if (item.LibraryId != collection.LibraryId)
        {
            throw new ValidationException("The media item belongs to a different library than the collection.");
        }

        if (await collectionRepository.ContainsItemAsync(collectionId, item.Id, cancellationToken).ConfigureAwait(false))
        {
            throw new ConflictException("The media item is already in this collection.");
        }

        await collectionRepository.AddItemAsync(collectionId, item.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task RemoveItemAsync(Guid collectionId, Guid mediaItemId, CancellationToken cancellationToken = default)
    {
        var collection = await collectionRepository.GetByIdAsync(collectionId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("Collection", collectionId);

        await collectionRepository.RemoveItemAsync(collection.Id, mediaItemId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<PagedResult<MediaItemDto>> ListItemsAsync(
        Guid collectionId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var collection = await collectionRepository.GetByIdAsync(collectionId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("Collection", collectionId);

        var (skip, take) = Paging.Validate(page, pageSize);
        var (items, totalCount) = await collectionRepository
            .ListItemsAsync(collection.Id, skip, take, cancellationToken)
            .ConfigureAwait(false);

        return new PagedResult<MediaItemDto>(
            Items: items.Select(item => new MediaItemDto(item.Id, item.DisplayName, item.CreatedAtUtc)).ToList(),
            Page: page,
            PageSize: pageSize,
            TotalCount: totalCount);
    }

    private static Guid RequireItemId(AddCollectionItemRequest? request) =>
        request is null || request.MediaItemId == Guid.Empty
            ? throw new ValidationException("MediaItemId is required.")
            : request.MediaItemId;

    private async Task<Library> RequireLibraryAsync(Guid libraryId, CancellationToken cancellationToken) =>
        await libraryRepository.GetByIdAsync(libraryId, cancellationToken).ConfigureAwait(false)
        ?? throw new NotFoundException("Library", libraryId);

    private static string NormalizeName(string? name)
    {
        var trimmed = name?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            throw new ValidationException("Collection name is required.");
        }

        if (trimmed.Length > MaxNameLength)
        {
            throw new ValidationException($"Collection name must be at most {MaxNameLength} characters.");
        }

        return trimmed;
    }

    private static CollectionDto ToDto(Core.Domain.Collection collection) =>
        new(collection.Id, collection.LibraryId, collection.Name, collection.CreatedAtUtc);
}
