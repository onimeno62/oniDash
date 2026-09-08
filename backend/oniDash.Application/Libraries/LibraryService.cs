using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using oniDash.Application.Common;
using oniDash.Core.Domain;

namespace oniDash.Application.Libraries;

public interface ILibraryService
{
    Task<IReadOnlyList<LibraryDto>> ListAsync(CancellationToken cancellationToken = default);

    Task<LibraryDto> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<LibraryDto> CreateAsync(CreateLibraryRequest request, CancellationToken cancellationToken = default);

    Task<LibraryDto> RenameAsync(Guid id, UpdateLibraryRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>
/// Library management. Deleting a library removes index data only — user files are never
/// touched (AGENTS.md rules 11, 12).
/// </summary>
public sealed class LibraryService(ILibraryRepository repository) : ILibraryService
{
    private const int MaxNameLength = 200;

    public async Task<IReadOnlyList<LibraryDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var libraries = await repository.ListAsync(cancellationToken).ConfigureAwait(false);
        return libraries.Select(ToDto).ToList();
    }

    public async Task<LibraryDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var library = await RequireAsync(id, cancellationToken).ConfigureAwait(false);
        return ToDto(library);
    }

    public async Task<LibraryDto> CreateAsync(CreateLibraryRequest request, CancellationToken cancellationToken = default)
    {
        var name = NormalizeName(request?.Name, "Library name");

        if (await repository.ExistsByNameAsync(name, cancellationToken: cancellationToken).ConfigureAwait(false))
        {
            throw new ConflictException($"A library named '{name}' already exists.");
        }

        var library = new Library { Name = name };
        await repository.AddAsync(library, cancellationToken).ConfigureAwait(false);
        return ToDto(library);
    }

    public async Task<LibraryDto> RenameAsync(Guid id, UpdateLibraryRequest request, CancellationToken cancellationToken = default)
    {
        var library = await RequireAsync(id, cancellationToken).ConfigureAwait(false);
        var name = NormalizeName(request?.Name, "Library name");

        if (await repository.ExistsByNameAsync(name, excludeId: id, cancellationToken: cancellationToken).ConfigureAwait(false))
        {
            throw new ConflictException($"A library named '{name}' already exists.");
        }

        library.Name = name;
        await repository.UpdateAsync(library, cancellationToken).ConfigureAwait(false);
        return ToDto(library);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var library = await RequireAsync(id, cancellationToken).ConfigureAwait(false);
        await repository.DeleteAsync(library, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Library> RequireAsync(Guid id, CancellationToken cancellationToken) =>
        await repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false)
        ?? throw new NotFoundException("Library", id);

    private static string NormalizeName(string? name, string field)
    {
        var trimmed = name?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            throw new ValidationException($"{field} is required.");
        }

        if (trimmed.Length > MaxNameLength)
        {
            throw new ValidationException($"{field} must be at most {MaxNameLength} characters.");
        }

        return trimmed;
    }

    private static LibraryDto ToDto(Library library) =>
        new(library.Id, library.Name, library.CreatedAtUtc);
}
