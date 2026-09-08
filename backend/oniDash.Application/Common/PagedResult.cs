using System;
using System.Collections.Generic;

namespace oniDash.Application.Common;

/// <summary>
/// A standard paged result. Page is 1-based; TotalCount is the number of items across
/// all pages for the same query.
/// </summary>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public const int MaxPageSize = 200;
}
