namespace oniDash.Core.Domain;

/// <summary>
/// Catalogue-neutral media taxonomy shared by discovery, indexing, search and plugins.
/// Plugins may refine these values in their own catalogue models but must not replace the
/// platform taxonomy with media-specific conditionals.
/// </summary>
public enum MediaType
{
    Unknown = 0,
    Audio = 1,
    Movie = 2,
    Episode = 3,
    Anime = 4,
    Manga = 5,
    Book = 6,
    Image = 7,
    Video = 8,
    Comic = 9,
    Document = 10,
}
