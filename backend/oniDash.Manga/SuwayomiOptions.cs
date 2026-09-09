namespace oniDash.Manga;

public sealed class SuwayomiOptions
{
    public const string SectionName = "Manga:Suwayomi";
    public string BaseUrl { get; set; } = "http://127.0.0.1:4567";
    public string? AccessToken { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
}
