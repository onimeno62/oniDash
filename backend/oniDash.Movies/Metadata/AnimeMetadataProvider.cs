using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using oniDash.Application.Catalogue;

namespace oniDash.Movies.Metadata;

/// <summary>
/// Optional anime enrichment boundary. Local catalogue data remains authoritative;
/// a configured resolver only produces a preview suggestion.
/// </summary>
public interface IAnimeSuggestionResolver
{
    Task<MetadataSuggestion?> SuggestAsync(Guid mediaItemId, CancellationToken cancellationToken = default);
}

public sealed class NullAnimeSuggestionResolver : IAnimeSuggestionResolver
{
    public Task<MetadataSuggestion?> SuggestAsync(Guid mediaItemId, CancellationToken cancellationToken = default) =>
        Task.FromResult<MetadataSuggestion?>(null);
}

public sealed class OptionalAnimeMetadataProvider(IAnimeSuggestionResolver resolver) : IAnimeMetadataProvider
{
    public string Id => "anime.optional";

    public Task<MetadataSuggestion?> SuggestAsync(Guid mediaItemId, CancellationToken cancellationToken = default) =>
        resolver.SuggestAsync(mediaItemId, cancellationToken);
}
