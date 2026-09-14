using oniDash.Music.Domain;
using oniDash.Music.Persistence;

namespace oniDash.Music.Tests;

public sealed class MusicPersistenceModelTests
{
    [Fact]
    public void Model_contains_phase_one_persistence_entities()
    {
        using var fixture = new MusicTestDatabase();
        using var db = fixture.CreateContext();
        var entityTypes = db.Model.GetEntityTypes().Select(e => e.ClrType).ToHashSet();

        Assert.Contains(typeof(MusicTrack), entityTypes);
        Assert.Contains(typeof(MusicPlaybackState), entityTypes);
        Assert.Contains(typeof(MusicLyrics), entityTypes);
        Assert.Contains(typeof(MusicArtwork), entityTypes);
    }

    [Fact]
    public void Track_has_indexes_for_missing_and_added_queries()
    {
        using var fixture = new MusicTestDatabase();
        using var db = fixture.CreateContext();
        var track = db.Model.FindEntityType(typeof(MusicTrack))!;
        var indexes = track.GetIndexes().ToList();

        Assert.Contains(indexes, index => index.Properties.Select(p => p.Name).SequenceEqual(["LibraryId", "IsMissing"]));
        Assert.Contains(indexes, index => index.Properties.Select(p => p.Name).SequenceEqual(["LibraryId", "AddedAtUtc"]));
    }
}
