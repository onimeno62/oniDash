namespace oniDash.Music.Lyrics;

public sealed record MusicLyricsCandidate(string Id, string Title, string Artist, string Album, bool Synced, string PlainText, string? SyncedText, string Source);

public interface IMusicLyricsProvider
{
    string ProviderId { get; }
    Task<IReadOnlyList<MusicLyricsCandidate>> SearchAsync(string track, string? artist, string? album, CancellationToken cancellationToken = default);
}
