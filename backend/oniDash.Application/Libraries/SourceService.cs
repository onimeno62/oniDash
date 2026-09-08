using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using oniDash.Application.Common;
using oniDash.Core.Domain;

namespace oniDash.Application.Libraries;

public interface ISourceService
{
    Task<IReadOnlyList<SourceDto>> ListAsync(Guid libraryId, CancellationToken cancellationToken = default);

    Task<SourceDto> AddAsync(Guid libraryId, CreateSourceRequest request, CancellationToken cancellationToken = default);

    Task RemoveAsync(Guid sourceId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Configures folder sources. Sources point at existing directories and are read-only:
/// registering or removing a source never touches the filesystem contents.
/// </summary>
public sealed class SourceService(
    ILibraryRepository libraryRepository,
    ILibrarySourceRepository sourceRepository,
    IFileSystemProbe fileSystemProbe) : ISourceService
{
    private const int MaxNameLength = 200;
    private const int MaxPathLength = 1024;

    public async Task<IReadOnlyList<SourceDto>> ListAsync(Guid libraryId, CancellationToken cancellationToken = default)
    {
        _ = await RequireLibraryAsync(libraryId, cancellationToken).ConfigureAwait(false);
        var sources = await sourceRepository.ListByLibraryAsync(libraryId, cancellationToken).ConfigureAwait(false);
        return sources.Select(ToDto).ToList();
    }

    public async Task<SourceDto> AddAsync(Guid libraryId, CreateSourceRequest request, CancellationToken cancellationToken = default)
    {
        _ = await RequireLibraryAsync(libraryId, cancellationToken).ConfigureAwait(false);

        var name = NormalizeName(request?.Name);
        var rootPath = NormalizePath(request?.RootPath);

        var existing = await sourceRepository.ListByLibraryAsync(libraryId, cancellationToken).ConfigureAwait(false);
        if (existing.Any(source => string.Equals(source.RootPath, rootPath, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ConflictException($"This folder is already configured as a source of the library.");
        }

        var source = new LibrarySource
        {
            LibraryId = libraryId,
            Name = name,
            RootPath = rootPath,
        };
        await sourceRepository.AddAsync(source, cancellationToken).ConfigureAwait(false);
        return ToDto(source);
    }

    public async Task RemoveAsync(Guid sourceId, CancellationToken cancellationToken = default)
    {
        var source = await sourceRepository.GetByIdAsync(sourceId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("Library source", sourceId);

        await sourceRepository.DeleteAsync(source, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Library> RequireLibraryAsync(Guid libraryId, CancellationToken cancellationToken) =>
        await libraryRepository.GetByIdAsync(libraryId, cancellationToken).ConfigureAwait(false)
        ?? throw new NotFoundException("Library", libraryId);

    private static string NormalizeName(string? name)
    {
        var trimmed = name?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            throw new ValidationException("Source name is required.");
        }

        if (trimmed.Length > MaxNameLength)
        {
            throw new ValidationException($"Source name must be at most {MaxNameLength} characters.");
        }

        return trimmed;
    }

    private string NormalizePath(string? path)
    {
        var trimmed = path?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            throw new ValidationException("Source path is required.");
        }

        string fullPath;
        try
        {
            if (!Path.IsPathRooted(trimmed))
            {
                throw new ValidationException("Source path must be absolute (for example 'D:\\Media\\Music').");
            }

            fullPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(trimmed));
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (Exception)
        {
            throw new ValidationException("Source path is not a valid Windows path.");
        }

        if (fullPath.Length > MaxPathLength)
        {
            throw new ValidationException($"Source path must be at most {MaxPathLength} characters.");
        }

        if (!fileSystemProbe.IsExistingDirectory(fullPath))
        {
            throw new ValidationException($"Folder '{fullPath}' does not exist. Choose an existing directory.");
        }

        return fullPath;
    }

    private static SourceDto ToDto(LibrarySource source) =>
        new(source.Id, source.LibraryId, source.Name, source.RootPath, source.CreatedAtUtc);
}
