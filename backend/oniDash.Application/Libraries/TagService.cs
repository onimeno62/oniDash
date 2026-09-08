using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using oniDash.Application.Common;
using oniDash.Core.Domain;

namespace oniDash.Application.Libraries;

public interface ITagService
{
    Task<IReadOnlyList<TagDto>> ListAsync(Guid libraryId, CancellationToken cancellationToken = default);

    Task<TagDto> CreateAsync(Guid libraryId, CreateTagRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid tagId, CancellationToken cancellationToken = default);

    Task AssignToItemAsync(Guid mediaItemId, AssignTagRequest request, CancellationToken cancellationToken = default);

    Task UnassignFromItemAsync(Guid mediaItemId, Guid tagId, CancellationToken cancellationToken = default);
}

public sealed class TagService(
    ILibraryRepository libraryRepository,
    IMediaItemRepository mediaItemRepository,
    ITagRepository tagRepository) : ITagService
{
    private const int MaxNameLength = 100;

    public async Task<IReadOnlyList<TagDto>> ListAsync(Guid libraryId, CancellationToken cancellationToken = default)
    {
        _ = await RequireLibraryAsync(libraryId, cancellationToken).ConfigureAwait(false);
        var tags = await tagRepository.ListByLibraryAsync(libraryId, cancellationToken).ConfigureAwait(false);
        return tags.Select(ToDto).ToList();
    }

    public async Task<TagDto> CreateAsync(Guid libraryId, CreateTagRequest request, CancellationToken cancellationToken = default)
    {
        _ = await RequireLibraryAsync(libraryId, cancellationToken).ConfigureAwait(false);

        var name = NormalizeName(request?.Name);
        if (await tagRepository.ExistsByNameAsync(libraryId, name, cancellationToken).ConfigureAwait(false))
        {
            throw new ConflictException($"A tag named '{name}' already exists in this library.");
        }

        var tag = new Tag { LibraryId = libraryId, Name = name };
        await tagRepository.AddAsync(tag, cancellationToken).ConfigureAwait(false);
        return ToDto(tag);
    }

    public async Task DeleteAsync(Guid tagId, CancellationToken cancellationToken = default)
    {
        var tag = await tagRepository.GetByIdAsync(tagId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("Tag", tagId);

        await tagRepository.DeleteAsync(tag, cancellationToken).ConfigureAwait(false);
    }

    public async Task AssignToItemAsync(Guid mediaItemId, AssignTagRequest request, CancellationToken cancellationToken = default)
    {
        var item = await mediaItemRepository.GetByIdAsync(mediaItemId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("Media item", mediaItemId);

        var tag = await tagRepository.GetByIdAsync(RequireTagId(request), cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("Tag", request!.TagId);

        if (tag.LibraryId != item.LibraryId)
        {
            throw new ValidationException("The tag belongs to a different library than the media item.");
        }

        if (await tagRepository.IsAssignedToItemAsync(mediaItemId, tag.Id, cancellationToken).ConfigureAwait(false))
        {
            throw new ConflictException("The tag is already applied to this media item.");
        }

        await tagRepository.AssignToItemAsync(mediaItemId, tag.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task UnassignFromItemAsync(Guid mediaItemId, Guid tagId, CancellationToken cancellationToken = default)
    {
        var tag = await tagRepository.GetByIdAsync(tagId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("Tag", tagId);

        await tagRepository.UnassignFromItemAsync(mediaItemId, tag.Id, cancellationToken).ConfigureAwait(false);
    }

    private static Guid RequireTagId(AssignTagRequest? request) =>
        request is null || request.TagId == Guid.Empty
            ? throw new ValidationException("TagId is required.")
            : request.TagId;

    private async Task<Library> RequireLibraryAsync(Guid libraryId, CancellationToken cancellationToken) =>
        await libraryRepository.GetByIdAsync(libraryId, cancellationToken).ConfigureAwait(false)
        ?? throw new NotFoundException("Library", libraryId);

    private static string NormalizeName(string? name)
    {
        var trimmed = name?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            throw new ValidationException("Tag name is required.");
        }

        if (trimmed.Length > MaxNameLength)
        {
            throw new ValidationException($"Tag name must be at most {MaxNameLength} characters.");
        }

        return trimmed;
    }

    private static TagDto ToDto(Tag tag) =>
        new(tag.Id, tag.LibraryId, tag.Name, tag.CreatedAtUtc);
}
