using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace oniDash.Movies.Cataloging;

/// <summary>Filename-derived metadata for one video file. Nulls mean "not detected".</summary>
public sealed record MovieNameInfo(string Title, int? Year);

/// <summary>
/// Parses release-style video filenames ("Movie (Year)", scene-style dotted names,
/// quality/edition noise, multi-part markers). Strictly a string transform — no
/// filesystem access, unit-testable, offline-first (rule 7).
/// </summary>
public static partial class MovieNameParser
{
    /// <summary>Quality/source/edition tokens stripped from titles before display.</summary>
    private static readonly string[] NoiseTokens =
    [
        "1080p", "720p", "2160p", "480p", "4k", "8k",
        "bluray", "blu-ray", "bdrip", "brrip", "dvdrip", "dvdscr", "webrip", "web-dl",
        "webdl", "web", "hdtv", "hdrip", "cam", "ts", "tc", "screener", "remux",
        "x264", "x265", "h264", "h265", "xvid", "avc", "hevc", "aac", "aac2", "ac3",
        "eac3", "dts", "dtshd", "truehd", "atmos", "ddp", "dd5", "5ch", "7ch", "2ch",
        "10bit", "8bit", "hdr", "sdr", "dv", "proper", "repack", "extended",
        "unrated", "remastered", "imax", "multi", "dual", "dubbed",
        "subbed", "subs", "internal", "reencoded", "hq", "mq", "lq",
    ];

    [GeneratedRegex(@"\((19\d{2}|20\d{2})\)")]
    private static partial Regex ParenYear();

    [GeneratedRegex(@"\b(19\d{2}|20\d{2})\b")]
    private static partial Regex BareYear();

    [GeneratedRegex(@"\s+(?:cd|part|pt|disk|disc|dvd)\s*\d+$", RegexOptions.IgnoreCase)]
    private static partial Regex MultiPart();

    // Codec tokens anchor the release-group tail ("...x264-GRP", "...h264.GRP"): cut it.
    [GeneratedRegex(@"\b(x264|x265|h264|h265|hevc|xvid|avc)\b.*$", RegexOptions.IgnoreCase)]
    private static partial Regex CodecTail();

    public static MovieNameInfo Parse(string fileName)
    {
        var stem = Path.GetFileNameWithoutExtension(fileName)?.Trim() ?? string.Empty;
        if (stem.Length == 0)
        {
            return new MovieNameInfo(string.Empty, null);
        }

        // 1. Year: an explicit (Year) wins, else the last 19xx/20xx group — the last
        //    one is the release year in release-style names ("2001.A.Space.Odyssey.
        //    1968.1080p"). Extracted before any cutting so "Blade Runner (2049)" keeps
        //    its year.
        int? year = null;
        var parenMatches = ParenYear().Matches(stem);
        var bareMatches = parenMatches.Count > 0 ? null : BareYear().Matches(stem);
        var match = parenMatches.Count > 0 ? parenMatches[^1] : bareMatches is { Count: > 0 } ? bareMatches[^1] : null;

        if (match is not null && match.Success)
        {
            year = int.Parse(match.Groups[1].Value);
            stem = stem.Remove(match.Index, match.Length);
        }

        // 2. Drop scene-style suffixes after the first separator.
        var cutMarkers = new[] { " - ", " [", " (" };
        var cutIndex = cutMarkers
            .Select(marker => stem.IndexOf(marker, StringComparison.Ordinal))
            .Where(index => index > 0)
            .DefaultIfEmpty(-1)
            .Min();
        var head = cutIndex > 0 ? stem[..cutIndex] : stem;

        // 3. Multi-part markers keep titles equal across parts ("... CD1/CD2").
        head = MultiPart().Replace(head, string.Empty);

        // 4. Cut the release-group tail anchored by a codec token.
        head = CodecTail().Replace(head, string.Empty);

        var title = Clean(head);
        if (title.Length == 0)
        {
            title = Clean(Path.GetFileNameWithoutExtension(fileName) ?? string.Empty);
        }

        return new MovieNameInfo(title, year);
    }

    private static string Clean(string value)
    {
        var words = value
            .Replace('.', ' ')
            .Replace('_', ' ')
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(word => !NoiseTokens.Contains(word, StringComparer.OrdinalIgnoreCase));

        return string.Join(' ', words).Trim(" -_[]()".ToCharArray());
    }
}
