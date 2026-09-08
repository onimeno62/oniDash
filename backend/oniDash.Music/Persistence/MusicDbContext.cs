using System;
using Microsoft.EntityFrameworkCore;
using oniDash.Music.Domain;

namespace oniDash.Music.Persistence;

/// <summary>Row anchors used only to declare cross-catalogue foreign keys in the model.</summary>
public sealed class MediaItemAnchor
{
    public Guid Id { get; set; }
}

/// <summary>
/// The music catalogue's own context. It shares the oniDash SQLite database with the
/// core context but keeps its own migration history table, so the plugin owns its
/// schema end to end (Architecture rule 2) without the core ever depending on it
/// (rule 5). Cross-catalogue integrity is enforced at the database level: tracks
/// cascade with their media items, albums/artists cascade with their libraries.
/// </summary>
public sealed class MusicDbContext(DbContextOptions<MusicDbContext> options) : DbContext(options)
{
    public DbSet<MusicArtist> Artists => Set<MusicArtist>();
    public DbSet<MusicAlbum> Albums => Set<MusicAlbum>();
    public DbSet<MusicTrack> Tracks => Set<MusicTrack>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MediaItemAnchor>().ToTable("MediaItems").HasKey(x => x.Id);

        modelBuilder.Entity<MusicArtist>(entity =>
        {
            entity.ToTable("Artists");
            entity.Property(a => a.Id).HasColumnName("Id");
            entity.Property(a => a.Name).HasMaxLength(512).IsRequired();
            entity.Property(a => a.NormalizedName).HasMaxLength(512).IsRequired();
            entity.HasIndex(a => new { a.LibraryId, a.NormalizedName }).IsUnique();
        });

        modelBuilder.Entity<MusicAlbum>(entity =>
        {
            entity.ToTable("Albums");
            entity.Property(a => a.Title).HasMaxLength(512).IsRequired();
            entity.Property(a => a.NormalizedTitle).HasMaxLength(512).IsRequired();
            entity.Property(a => a.AlbumKey).HasMaxLength(1100).IsRequired();
            entity.Property(a => a.ArtistName).HasMaxLength(512);
            entity.Property(a => a.CoverContentType).HasMaxLength(128);
            // Library-scoped uniqueness: two libraries may own the same album title.
            entity.HasIndex(a => new { a.LibraryId, a.AlbumKey }).IsUnique();
            entity.HasOne(a => a.Artist)
                .WithMany()
                .HasForeignKey(a => a.ArtistId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<MusicTrack>(entity =>
        {
            entity.ToTable("Tracks");
            entity.Property(t => t.Title).HasMaxLength(512).IsRequired();
            entity.Property(t => t.ArtistName).HasMaxLength(512);
            entity.Property(t => t.Genre).HasMaxLength(256);
            entity.HasIndex(t => t.MediaItemId).IsUnique();
            entity.HasIndex(t => new { t.AlbumId, t.DiscNumber, t.TrackNumber, t.Title });
            entity.HasOne<MediaItemAnchor>()
                .WithMany()
                .HasForeignKey(t => t.MediaItemId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(t => t.Album)
                .WithMany()
                .HasForeignKey(t => t.AlbumId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(t => t.Artist)
                .WithMany()
                .HasForeignKey(t => t.ArtistId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        base.OnModelCreating(modelBuilder);
    }
}
