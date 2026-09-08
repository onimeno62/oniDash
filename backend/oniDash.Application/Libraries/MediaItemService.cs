using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using oniDash.Application.Common;
using oniDash.Core.Domain;

namespace oniDash.Application.Libraries;

public interface IMediaItemService
{
    Task<PagedResult<MediaItemDto>> ListByLibraryAsync(
        Guid libraryId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}

/// <summary>Read-side queries over media items (populated by the Phase 3 scanner).</summary>
public sealed class MediaItemService(ILibraryRepository libraryRepository, IMediaItemRepository repository) : IMediaItemService
{
    public async Task<PagedResult<MediaItemDto>> ListByLibraryAsync(
        Guid libraryId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        _ = await libraryRepository.GetByIdAsync(libraryId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("Library", libraryId);

        var (skip, take) = Paging.Validate(page, pageSize);
        var (items, totalCount) = await repository
            .ListByLibraryAsync(libraryId, skip, take, cancellationToken)
            .ConfigureAwait(false);

        return new PagedResult<MediaItemDto>(
            Items: items.Select(ToDto).ToList(),
            Page: page,
            PageSize: pageSize,
            TotalCount: totalCount);
    }

    private static MediaItemDto ToDto(MediaItem item) =>
        new(item.Id, item.DisplayName, item.CreatedAtUtc);
}
