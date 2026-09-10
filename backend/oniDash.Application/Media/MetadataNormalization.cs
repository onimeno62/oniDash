using System.Globalization;
using oniDash.Core.Domain;

namespace oniDash.Application.Media;

/// <summary>Normalizes handler output without replacing local truth or external provenance.</summary>
public interface IMediaMetadataNormalizer
{
    MediaInspectionResult Normalize(MediaInspectionResult inspection);
}

public sealed class MediaMetadataNormalizer : IMediaMetadataNormalizer
{
    public MediaInspectionResult Normalize(MediaInspectionResult inspection)
    {
        ArgumentNullException.ThrowIfNull(inspection);
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in inspection.Metadata)
        {
            var key = NormalizeKey(pair.Key);
            var value = NormalizeValue(pair.Value);
            if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value)) metadata[key] = value;
        }

        var title = NormalizeValue(inspection.Title);
        var artwork = inspection.Artwork
            .Where(candidate => !string.IsNullOrWhiteSpace(candidate.Source)
                && !string.IsNullOrWhiteSpace(candidate.MimeType))
            .Select(candidate => candidate with { Kind = NormalizeValue(candidate.Kind) ?? "thumbnail", Source = NormalizeValue(candidate.Source)!, MimeType = candidate.MimeType.Trim().ToLowerInvariant() })
            .ToArray();

        return inspection with
        {
            Title = title,
            Metadata = metadata,
            Artwork = artwork,
            Duration = inspection.Duration is { } duration && duration >= TimeSpan.Zero ? duration : null,
            Width = inspection.Width is > 0 ? inspection.Width : null,
            Height = inspection.Height is > 0 ? inspection.Height : null,
            Provenance = inspection.Provenance with { Confidence = Math.Clamp(inspection.Provenance.Confidence, 0d, 1d) }
        };
    }

    private static string NormalizeKey(string value) =>
        string.Concat((value ?? string.Empty).Trim().Select((character, index) =>
            char.IsLetterOrDigit(character) ? (index == 0 ? char.ToLowerInvariant(character).ToString() : character.ToString()) : ""));

    private static string? NormalizeValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : string.Join(' ', value.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
