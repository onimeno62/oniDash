using System;

namespace oniDash.Application.Common;

/// <summary>Canonical offset pagination used by all list/search APIs.</summary>
public readonly record struct PageRequest(int Limit = 50, int Offset = 0)
{
    public const int DefaultLimit = 50;
    public const int MaxLimit = 200;

    public PageRequest Normalize() => new(Math.Clamp(Limit <= 0 ? DefaultLimit : Limit, 1, MaxLimit), Math.Max(0, Offset));
}

public sealed record PageResult<T>(IReadOnlyList<T> Items, int TotalCount, PageRequest Page)
{
    public bool HasNext => Page.Offset + Items.Count < TotalCount;
}
