using Microsoft.EntityFrameworkCore;
using oniDash.Manga.Domain;

namespace oniDash.Manga.Persistence;

public sealed class MangaDbContext(DbContextOptions<MangaDbContext> options) : DbContext(options)
{
    public DbSet<MangaTitle> Titles => Set<MangaTitle>();
    public DbSet<MangaChapter> Chapters => Set<MangaChapter>();
    public DbSet<MangaBookmarkEntity> Bookmarks => Set<MangaBookmarkEntity>();
    public DbSet<MangaPlugin> Plugins => Set<MangaPlugin>();
    public DbSet<MangaDownload> Downloads => Set<MangaDownload>();
    public DbSet<MangaNotification> Notifications => Set<MangaNotification>();
    public DbSet<MangaTrackingSync> TrackingSyncs => Set<MangaTrackingSync>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MangaTitle>(e => { e.ToTable("MangaTitles"); e.Property(x => x.Title).HasMaxLength(512).IsRequired(); e.Property(x => x.NormalizedTitle).HasMaxLength(512).IsRequired(); e.Property(x => x.SourceId).HasMaxLength(128).IsRequired(); e.Property(x => x.ExternalId).HasMaxLength(512).IsRequired(); e.HasIndex(x => new { x.SourceId, x.ExternalId }).IsUnique(); e.HasIndex(x => new { x.LibraryId, x.NormalizedTitle }); });
        modelBuilder.Entity<MangaChapter>(e => { e.ToTable("MangaChapters"); e.Property(x => x.SourceChapterId).HasMaxLength(512).IsRequired(); e.Property(x => x.Title).HasMaxLength(512).IsRequired(); e.HasIndex(x => new { x.MangaId, x.SourceChapterId }).IsUnique(); e.HasOne<MangaTitle>().WithMany().HasForeignKey(x => x.MangaId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<MangaBookmarkEntity>(e => { e.ToTable("MangaBookmarks"); e.HasIndex(x => new { x.ChapterId, x.Page }).IsUnique(); e.HasOne<MangaChapter>().WithMany().HasForeignKey(x => x.ChapterId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<MangaPlugin>(e => { e.ToTable("MangaPlugins"); e.HasKey(x => x.Id); e.Property(x => x.Id).HasMaxLength(128); e.Property(x => x.Name).HasMaxLength(256).IsRequired(); });
        modelBuilder.Entity<MangaDownload>(e => { e.ToTable("MangaDownloads"); e.HasIndex(x => new { x.ChapterId, x.State }); e.HasOne<MangaChapter>().WithMany().HasForeignKey(x => x.ChapterId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<MangaNotification>(e => { e.ToTable("MangaNotifications"); e.HasKey(x => x.Id); e.HasIndex(x => x.MangaId); e.HasIndex(x => x.Read); });
        modelBuilder.Entity<MangaTrackingSync>(e => { e.ToTable("MangaTrackingSyncs"); e.HasKey(x => x.Id); e.HasIndex(x => new { x.MangaId, x.TrackerName }).IsUnique(); });
    }
}

public sealed class MangaDatabaseInitializer(MangaDbContext db)
{
    public Task InitializeAsync(CancellationToken cancellationToken = default) => db.Database.MigrateAsync(cancellationToken);
}
