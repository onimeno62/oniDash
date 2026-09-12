using Microsoft.EntityFrameworkCore;
using oniDash.Books.Domain;

namespace oniDash.Books.Persistence;

public sealed class BooksDbContext : DbContext
{
    public BooksDbContext(DbContextOptions<BooksDbContext> options) : base(options) { }

    public DbSet<BookItem> Books => Set<BookItem>();
    public DbSet<BookAuthor> Authors => Set<BookAuthor>();
    public DbSet<BookSeries> Series => Set<BookSeries>();
    public DbSet<BookBookmark> Bookmarks => Set<BookBookmark>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BookItem>(b =>
        {
            b.ToTable("books");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.MediaItemId).IsUnique();
            b.HasIndex(x => x.LibraryId);
            b.HasIndex(x => x.AuthorId);
            b.HasIndex(x => x.SeriesId);

            b.HasOne(x => x.Author)
                .WithMany(x => x.Books)
                .HasForeignKey(x => x.AuthorId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasOne(x => x.Series)
                .WithMany(x => x.Books)
                .HasForeignKey(x => x.SeriesId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<BookAuthor>(b =>
        {
            b.ToTable("book_authors");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.Name);
        });

        modelBuilder.Entity<BookSeries>(b =>
        {
            b.ToTable("book_series");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.Title);
        });

        modelBuilder.Entity<BookBookmark>(b =>
        {
            b.ToTable("book_bookmarks");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.BookId);
            b.HasOne(x => x.Book)
                .WithMany(x => x.Bookmarks)
                .HasForeignKey(x => x.BookId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
