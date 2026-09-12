using System;
using System.Collections.Generic;

namespace oniDash.Books.Domain;

public sealed class BookAuthor
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Biography { get; set; }
    public List<BookItem> Books { get; set; } = new();
}

public sealed class BookSeries
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<BookItem> Books { get; set; } = new();
}

public sealed class BookItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MediaItemId { get; set; }
    public Guid LibraryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public Guid? AuthorId { get; set; }
    public BookAuthor? Author { get; set; }
    public Guid? SeriesId { get; set; }
    public BookSeries? Series { get; set; }
    public int? SeriesIndex { get; set; }
    public int? Year { get; set; }
    public int? PageCount { get; set; }
    public string? Format { get; set; }
    public string? FilePath { get; set; }
    public bool Read { get; set; }
    public int? ProgressPages { get; set; }
    public double? ProgressPercent { get; set; }
    public int? Rating { get; set; }
    public bool Favorite { get; set; }
    public DateTimeOffset? LastReadAtUtc { get; set; }
    public List<BookBookmark> Bookmarks { get; set; } = new();
}

public sealed class BookBookmark
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BookId { get; set; }
    public BookItem? Book { get; set; }
    public int PageNumber { get; set; }
    public string? Title { get; set; }
    public string? Note { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
