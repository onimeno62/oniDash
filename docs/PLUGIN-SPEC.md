# oniDash Plugin Specification

Plugins add catalogue-specific capabilities without coupling them to Core.

## May provide
Domain models, database configuration/migrations, file detectors, metadata readers/providers, application services, approved API endpoints, UI routes, navigation entries, commands, background jobs.

## Must not
Modify Core merely to support itself; access another plugin's tables directly; bypass application services from the frontend; perform destructive filesystem operations by default.

## Conceptual contract
```csharp
public interface IMediaPlugin
{
    string Id { get; }
    string Name { get; }
    Version Version { get; }
    void RegisterServices(IServiceCollection services);
    void RegisterRoutes(IEndpointRouteBuilder endpoints);
}
```

The exact contract may evolve before implementation; avoid premature complexity.

## Detection
Plugins expose supported extensions, confidence, identification logic, and optional folder heuristics.

## Metadata
External identifiers are stored as ExternalReference. Providers are adapters.

## UI
Plugins can register navigation entries, catalogue/detail routes, and context actions, while using the shared oniDash design system.

First-party modules: Music, Movies, Anime, Manga, Books.
