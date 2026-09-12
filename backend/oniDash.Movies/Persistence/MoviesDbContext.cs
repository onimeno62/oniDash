using System;
using Microsoft.EntityFrameworkCore;
using oniDash.Movies.Domain;

namespace oniDash.Movies.Persistence;

public sealed class MediaItemAnchor
{
    public Guid Id { get; set; }
}

public sealed class MoviesDbContext(DbContextOptions<MoviesDbContext> options) : DbContext(options)
{
    public DbSet<Movie> Movies => Set<Movie>();
    public DbSet<TvSeries> TvSeries => Set<TvSeries>();
    public DbSet<TvSeason> TvSeasons => Set<TvSeason>();
    public DbSet<TvEpisode> TvEpisodes => Set<TvEpisode>();

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
            entity.HasIndex(m => m.MediaItemId).IsUnique();
            entity.HasIndex(m => new { m.LibraryId, m.NormalizedTitle });
            entity.HasOne<MediaItemAnchor>().WithMany().HasForeignKey(m => m.MediaItemId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TvSeries>(entity =>
        {
            entity.ToTable("TvSeries");
            entity.Property(x => x.Title).HasMaxLength(512).IsRequired();
            entity.Property(x => x.NormalizedTitle).HasMaxLength(512).IsRequired();
            entity.HasIndex(x => new { x.LibraryId, x.NormalizedTitle, x.Year }).IsUnique();
        });

        modelBuilder.Entity<TvSeason>(entity =>
        {
            entity.ToTable("TvSeasons");
            entity.Property(x => x.Title).HasMaxLength(512);
            entity.HasIndex(x => new { x.SeriesId, x.Number }).IsUnique();
            entity.HasOne<TvSeries>().WithMany().HasForeignKey(x => x.SeriesId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TvEpisode>(entity =>
        {
            entity.ToTable("TvEpisodes");
            entity.Property(x => x.Title).HasMaxLength(512).IsRequired();
            entity.HasIndex(x => x.MediaItemId).IsUnique();
            entity.HasIndex(x => new { x.SeasonId, x.Number }).IsUnique();
            entity.HasOne<TvSeason>().WithMany().HasForeignKey(x => x.SeasonId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<MediaItemAnchor>().WithMany().HasForeignKey(x => x.MediaItemId).OnDelete(DeleteBehavior.Cascade);
        });

        base.OnModelCreating(modelBuilder);
    }
}
