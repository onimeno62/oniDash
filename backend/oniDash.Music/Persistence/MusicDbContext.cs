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
    public DbSet<MusicPlaylist> Playlists => Set<MusicPlaylist>();
    public DbSet<MusicPlaylistItem> PlaylistItems => Set<MusicPlaylistItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MediaItemAnchor>().ToTable("MediaItems").HasKey(x => x.Id);
        modelBuilder.Entity<MusicArtist>(entity => { entity.ToTable("Artists"); entity.Property(a => a.Name).HasMaxLength(512).IsRequired(); entity.Property(a => a.NormalizedName).HasMaxLength(512).IsRequired(); entity.HasIndex(a => new { a.LibraryId, a.NormalizedName }).IsUnique(); });
        modelBuilder.Entity<MusicAlbum>(entity => { entity.ToTable("Albums"); entity.Property(a => a.Title).HasMaxLength(512).IsRequired(); entity.Property(a => a.NormalizedTitle).HasMaxLength(512).IsRequired(); entity.Property(a => a.AlbumKey).HasMaxLength(1100).IsRequired(); entity.Property(a => a.ArtistName).HasMaxLength(512); entity.Property(a => a.CoverContentType).HasMaxLength(128); entity.HasIndex(a => new { a.LibraryId, a.AlbumKey }).IsUnique(); });
        modelBuilder.Entity<MusicTrack>(entity => { entity.ToTable("Tracks"); entity.Property(t => t.Title).HasMaxLength(512).IsRequired(); entity.Property(t => t.ArtistName).HasMaxLength(512); entity.Property(t => t.Genre).HasMaxLength(256); entity.Property(t => t.Rating).HasDefaultValue(0); entity.Property(t => t.ReplayGainTrackDb).HasPrecision(6, 3); entity.Property(t => t.ReplayGainAlbumDb).HasPrecision(6, 3); entity.Property(t => t.R128TrackGainDb).HasPrecision(6, 3); entity.Property(t => t.R128AlbumGainDb).HasPrecision(6, 3); entity.HasIndex(t => t.MediaItemId).IsUnique(); entity.HasIndex(t => new { t.AlbumId, t.DiscNumber, t.TrackNumber, t.Title }); entity.HasOne<MediaItemAnchor>().WithMany().HasForeignKey(t => t.MediaItemId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<MusicFavorite>(entity => { entity.ToTable("MusicFavorites"); entity.Property(f => f.EntityType).HasMaxLength(32).IsRequired(); entity.HasIndex(f => new { f.EntityType, f.EntityId }).IsUnique(); });
        modelBuilder.Entity<MusicPlayHistory>(entity => { entity.ToTable("MusicPlayHistory"); entity.Property(h => h.Source).HasMaxLength(64).IsRequired(); entity.HasIndex(h => new { h.TrackId, h.StartedAtUtc }); });
        modelBuilder.Entity<MusicPlaylist>(entity => { entity.ToTable("MusicPlaylists"); entity.Property(p => p.Name).HasMaxLength(256).IsRequired(); entity.Property(p => p.Description).HasMaxLength(2000); entity.Property(p => p.SmartQuery).HasMaxLength(4000); entity.HasIndex(p => p.Name).IsUnique(); });
        modelBuilder.Entity<MusicPlaylistItem>(entity => { entity.ToTable("MusicPlaylistItems"); entity.HasIndex(i => new { i.PlaylistId, i.Position }).IsUnique(); entity.HasIndex(i => new { i.PlaylistId, i.TrackId }).IsUnique(); entity.HasOne(i => i.Playlist).WithMany().HasForeignKey(i => i.PlaylistId).OnDelete(DeleteBehavior.Cascade); entity.HasOne(i => i.Track).WithMany().HasForeignKey(i => i.TrackId).OnDelete(DeleteBehavior.Cascade); });
        base.OnModelCreating(modelBuilder);
    }
}
