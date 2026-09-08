using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using oniDash.Core.Domain;

namespace oniDash.Infrastructure.Persistence;

/// <summary>
/// Root EF Core context for the core library database. Catalogue-specific models must
/// never be added here — plugins own their own persistence (Architecture rules 1, 2).
/// </summary>
public sealed class OniDashDbContext(DbContextOptions<OniDashDbContext> options) : DbContext(options)
{
    public DbSet<Library> Libraries => Set<Library>();
    public DbSet<LibrarySource> Sources => Set<LibrarySource>();
    public DbSet<MediaItem> MediaItems => Set<MediaItem>();
    public DbSet<MediaFile> Files => Set<MediaFile>();
    public DbSet<Artwork> Artwork => Set<Artwork>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<MediaItemTag> MediaItemTags => Set<MediaItemTag>();
    public DbSet<Collection> Collections => Set<Collection>();
    public DbSet<CollectionItem> CollectionItems => Set<CollectionItem>();

    /// <summary>
    /// SQLite cannot order by TEXT-based DateTimeOffset columns, so every DateTimeOffset is
    /// stored as a sortable binary integer instead (EF-recommended workaround).
    /// </summary>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<DateTimeOffsetToBinaryConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Library>(entity =>
        {
            entity.Property(l => l.Name).HasMaxLength(200).IsRequired().UseCollation("NOCASE");
        });

        modelBuilder.Entity<LibrarySource>(entity =>
        {
            entity.Property(s => s.Name).HasMaxLength(200).IsRequired();
            entity.Property(s => s.RootPath).HasMaxLength(1024).IsRequired().UseCollation("NOCASE");
            // Backstop for the service-level case-insensitive duplicate check.
            entity.HasIndex(s => new { s.LibraryId, s.RootPath }).IsUnique();
            entity.HasOne(s => s.Library)
                .WithMany(l => l.Sources)
                .HasForeignKey(s => s.LibraryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MediaItem>(entity =>
        {
            entity.Property(i => i.DisplayName).HasMaxLength(400).IsRequired();
            entity.HasIndex(i => new { i.LibraryId, i.CreatedAtUtc });
            entity.HasOne(i => i.Library)
                .WithMany()
                .HasForeignKey(i => i.LibraryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MediaFile>(entity =>
        {
            entity.Property(f => f.RelativePath).HasMaxLength(1024).IsRequired();
            entity.Property(f => f.IdentityKey).HasMaxLength(1100).IsRequired();
            entity.Property(f => f.Extension).HasMaxLength(32);
            entity.HasIndex(f => new { f.LibrarySourceId, f.RelativePath }).IsUnique();
            // Dedup guard: one index row per (source, path), enforced by the database.
            entity.HasIndex(f => f.IdentityKey).IsUnique();
            entity.HasOne(f => f.MediaItem)
                .WithMany(i => i.Files)
                .HasForeignKey(f => f.MediaItemId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(f => f.LibrarySource)
                .WithMany(s => s.Files)
                .HasForeignKey(f => f.LibrarySourceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Artwork>(entity =>
        {
            entity.Property(a => a.SourcePath).HasMaxLength(1024).IsRequired();
            entity.Property(a => a.Kind).HasConversion<int>();
            entity.HasOne(a => a.MediaItem)
                .WithMany(i => i.Artwork)
                .HasForeignKey(a => a.MediaItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Tag>(entity =>
        {
            entity.Property(t => t.Name).HasMaxLength(100).IsRequired().UseCollation("NOCASE");
            entity.HasIndex(t => new { t.LibraryId, t.Name }).IsUnique();
            entity.HasOne(t => t.Library)
                .WithMany()
                .HasForeignKey(t => t.LibraryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MediaItemTag>(entity =>
        {
            entity.HasKey(t => new { t.MediaItemId, t.TagId });
            entity.HasOne(t => t.MediaItem)
                .WithMany(i => i.Tags)
                .HasForeignKey(t => t.MediaItemId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(t => t.Tag)
                .WithMany(t => t.Items)
                .HasForeignKey(t => t.TagId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Collection>(entity =>
        {
            entity.Property(c => c.Name).HasMaxLength(200).IsRequired().UseCollation("NOCASE");
            entity.HasIndex(c => new { c.LibraryId, c.Name }).IsUnique();
            entity.HasOne(c => c.Library)
                .WithMany()
                .HasForeignKey(c => c.LibraryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CollectionItem>(entity =>
        {
            entity.HasKey(ci => new { ci.CollectionId, ci.MediaItemId });
            entity.HasOne(ci => ci.Collection)
                .WithMany(c => c.Items)
                .HasForeignKey(ci => ci.CollectionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(ci => ci.MediaItem)
                .WithMany(i => i.Collections)
                .HasForeignKey(ci => ci.MediaItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
