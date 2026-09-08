using System;
using Microsoft.EntityFrameworkCore;
using oniDash.Movies.Domain;

namespace oniDash.Movies.Persistence;

/// <summary>Row anchor used only to declare the cross-catalogue foreign key in the model.</summary>
public sealed class MediaItemAnchor
{
    public Guid Id { get; set; }
}

/// <summary>
/// The movie catalogue's own context. It shares the oniDash SQLite database with the
/// core context but keeps its own migration history table, so the plugin owns its
/// schema end to end (Architecture rule 2) without the core ever depending on it
/// (rule 5). Cross-catalogue integrity is enforced at the database level: movies
/// cascade with their media items.
/// </summary>
public sealed class MoviesDbContext(DbContextOptions<MoviesDbContext> options) : DbContext(options)
{
    public DbSet<Movie> Movies => Set<Movie>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MediaItemAnchor>().ToTable("MediaItems").HasKey(x => x.Id);

        modelBuilder.Entity<Movie>(entity =>
        {
            entity.ToTable("Movies");
            entity.Property(m => m.Title).HasMaxLength(512).IsRequired();
            entity.Property(m => m.NormalizedTitle).HasMaxLength(512).IsRequired();
            entity.Property(m => m.Container).HasMaxLength(32);
            entity.Property(m => m.PosterContentType).HasMaxLength(128);
            // One movie per media item: two files of the same title stay separate rows
            // until duplicate detection arrives (docs/ROADMAP.md Phase 10).
            entity.HasIndex(m => m.MediaItemId).IsUnique();
            entity.HasIndex(m => new { m.LibraryId, m.NormalizedTitle });
            entity.HasOne<MediaItemAnchor>()
                .WithMany()
                .HasForeignKey(m => m.MediaItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        base.OnModelCreating(modelBuilder);
    }
}
