using System;
using Microsoft.EntityFrameworkCore;
using oniDash.Music.Domain;

namespace oniDash.Music.Persistence;

public sealed class MediaItemAnchor { public Guid Id { get; set; } }

public sealed class MusicDbContext(DbContextOptions<MusicDbContext> options) : DbContext(options)
{
    public DbSet<MusicArtist> Artists => Set<MusicArtist>();
    public DbSet<MusicAlbum> Albums => Set<MusicAlbum>();
    public DbSet<MusicTrack> Tracks => Set<MusicTrack>();
    public DbSet<MusicFavorite> Favorites => Set<MusicFavorite>();
    public DbSet<MusicPlayHistory> PlayHistory => Set<MusicPlayHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MediaItemAnchor>().ToTable("MediaItems").HasKey(x => x.Id);
        modelBuilder.Entity<MusicArtist>(entity => { entity.ToTable("Artists"); entity.Property(a => a.Name).HasMaxLength(512).IsRequired(); entity.Property(a => a.NormalizedName).HasMaxLength(512).IsRequired(); entity.HasIndex(a => new { a.LibraryId, a.NormalizedName }).IsUnique(); });
        modelBuilder.Entity<MusicAlbum>(entity => { entity.ToTable("Albums"); entity.Property(a => a.Title).HasMaxLength(512).IsRequired(); entity.Property(a => a.NormalizedTitle).HasMaxLength(512).IsRequired(); entity.Property(a => a.AlbumKey).HasMaxLength(1100).IsRequired(); entity.Property(a => a.ArtistName).HasMaxLength(512); entity.Property(a => a.CoverContentType).HasMaxLength(128); entity.HasIndex(a => new { a.LibraryId, a.AlbumKey }).IsUnique(); entity.HasOne(a => a.Artist).WithMany().HasForeignKey(a => a.ArtistId).OnDelete(DeleteBehavior.SetNull); });
        modelBuilder.Entity<MusicTrack>(entity => { entity.ToTable("Tracks"); entity.Property(t => t.Title).HasMaxLength(512).IsRequired(); entity.Property(t => t.ArtistName).HasMaxLength(512); entity.Property(t => t.Genre).HasMaxLength(256); entity.HasIndex(t => t.MediaItemId).IsUnique(); entity.HasIndex(t => new { t.AlbumId, t.DiscNumber, t.TrackNumber, t.Title }); entity.HasOne<MediaItemAnchor>().WithMany().HasForeignKey(t => t.MediaItemId).OnDelete(DeleteBehavior.Cascade); entity.HasOne(t => t.Album).WithMany().HasForeignKey(t => t.AlbumId).OnDelete(DeleteBehavior.SetNull); entity.HasOne(t => t.Artist).WithMany().HasForeignKey(t => t.ArtistId).OnDelete(DeleteBehavior.SetNull); });
        modelBuilder.Entity<MusicFavorite>(entity => { entity.ToTable("MusicFavorites"); entity.Property(f => f.EntityType).HasMaxLength(32).IsRequired(); entity.HasIndex(f => new { f.EntityType, f.EntityId }).IsUnique(); });
        modelBuilder.Entity<MusicPlayHistory>(entity => { entity.ToTable("MusicPlayHistory"); entity.Property(h => h.Source).HasMaxLength(64).IsRequired(); entity.HasIndex(h => new { h.TrackId, h.StartedAtUtc }); });
        base.OnModelCreating(modelBuilder);
    }
}
