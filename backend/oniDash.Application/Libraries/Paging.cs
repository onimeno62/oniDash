using System;
using System.Threading;
using System.Threading.Tasks;
using oniDash.Application.Common;

namespace oniDash.Application.Libraries;

/// <summary>Validates and normalizes paging parameters shared by paged queries.</summary>
public static class Paging
{
    public static (int Skip, int Take) Validate(int page, int pageSize)
    {
        if (page < 1)
        {
            throw new ValidationException("Page must be 1 or greater.");
        }

        if (pageSize is < 1 or > PagedResult<object>.MaxPageSize)
        {
            throw new ValidationException($"Page size must be between 1 and {PagedResult<object>.MaxPageSize}.");
        }

        return ((page - 1) * pageSize, pageSize);
    }
}
