using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using oniDash.Movies.Domain;
using Xunit;

namespace oniDash.Movies.Tests;

public sealed class TvCatalogPersistenceTests
{
    [Fact]
    public async Task Persists_series_season_episode_and_watch_progress()
    {
        using var database = new MovieTestDatabase();
        var libraryId = Guid.NewGuid();
        var mediaItemId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        await using (var db = database.CreateContext())
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO Libraries (Id, Name, CreatedAtUtc) VALUES ({libraryId}, {'T' + "V"}, {now})");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO Sources (Id, LibraryId, Name, RootPath, CreatedAtUtc) VALUES ({sourceId}, {libraryId}, {'T' + "V"}, {'/' + "tv"}, {now})");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO MediaItems (Id, LibraryId, DisplayName, CreatedAtUtc) VALUES ({mediaItemId}, {libraryId}, {'P' + "ilot"}, {now})");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO Files (Id, MediaItemId, LibrarySourceId, RelativePath, IdentityKey, Extension, SizeBytes, LastWriteTimeUtc, CreatedAtUtc) VALUES ({fileId}, {mediaItemId}, {sourceId}, {'s' + "01e01.mkv"}, {'i' + "d"}, {'.' + "mkv"}, {100L}, {now}, {now})");

            var series = new TvSeries { Id = Guid.NewGuid(), LibraryId = libraryId, Title = "Example", NormalizedTitle = "example", UpdatedAtUtc = now };
            var season = new TvSeason { Id = Guid.NewGuid(), SeriesId = series.Id, Number = 1 };
            var episode = new TvEpisode { Id = Guid.NewGuid(), SeasonId = season.Id, MediaItemId = mediaItemId, FileId = fileId, Number = 1, Title = "Pilot", DurationSeconds = 1200, WatchProgressSeconds = 300, UpdatedAtUtc = now };
            db.AddRange(series, season, episode);
            await db.SaveChangesAsync();
        }

        await using (var verify = database.CreateContext())
        {
            var episode = await verify.TvEpisodes.SingleAsync();
            Assert.Equal("Pilot", episode.Title);
            Assert.Equal(300, episode.WatchProgressSeconds);
            Assert.False(episode.Watched);
            Assert.Equal(1, await verify.TvSeries.CountAsync());
            Assert.Equal(1, await verify.TvSeasons.CountAsync());
        }
    }
}
