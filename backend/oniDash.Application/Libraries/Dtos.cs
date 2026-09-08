using System;
using System.Collections.Generic;
using oniDash.Core.Domain;

namespace oniDash.Application.Libraries;

public sealed record LibraryDto(Guid Id, string Name, DateTimeOffset CreatedAtUtc);

public sealed record CreateLibraryRequest(string Name);

public sealed record UpdateLibraryRequest(string Name);

public sealed record SourceDto(Guid Id, Guid LibraryId, string Name, string RootPath, DateTimeOffset CreatedAtUtc);

public sealed record CreateSourceRequest(string Name, string RootPath);

public sealed record MediaItemDto(Guid Id, string DisplayName, DateTimeOffset CreatedAtUtc);

public sealed record TagDto(Guid Id, Guid LibraryId, string Name, DateTimeOffset CreatedAtUtc);

public sealed record CreateTagRequest(string Name);

public sealed record AssignTagRequest(Guid TagId);

public sealed record CollectionDto(Guid Id, Guid LibraryId, string Name, DateTimeOffset CreatedAtUtc);

public sealed record CreateCollectionRequest(string Name);

public sealed record AddCollectionItemRequest(Guid MediaItemId);
