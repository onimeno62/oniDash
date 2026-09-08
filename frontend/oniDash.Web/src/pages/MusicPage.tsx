import { useEffect, useState } from 'react';
import { fetchLibraries, type Library } from '../api/libraries';
import {
  albumCoverUrl,
  fetchMusicAlbums,
  fetchMusicArtists,
  fetchMusicTracks,
  formatDuration,
  type AlbumSummary,
  type ArtistSummary,
  type TrackSummary,
} from '../api/music';
import { usePlayer } from '../hooks/usePlayer';
import { ErrorState } from '../components/states/ErrorState';
import { EmptyState } from '../components/states/EmptyState';
import { LoadingState } from '../components/states/LoadingState';
import { MusicIcon, PlayIcon } from '../components/icons';

/**
 * Music catalogue browser: pick a library, browse artists/albums, play tracks.
 * Tracks stream from the local API with range support; covers come from embedded art.
 */
export function MusicPage() {
  const [libraries, setLibraries] = useState<Library[] | null>(null);
  const [libraryId, setLibraryId] = useState<string>('');
  const [selectedArtist, setSelectedArtist] = useState<ArtistSummary | null>(null);
  const [librariesError, setLibrariesError] = useState<string | null>(null);

  useEffect(() => {
    const controller = new AbortController();
    fetchLibraries(controller.signal)
      .then((all) => {
        setLibraries(all);
        setLibraryId((current) => current || all[0]?.id || '');
      })
      .catch((error: unknown) => {
        if (!controller.signal.aborted) {
          setLibrariesError(error instanceof Error ? error.message : 'Could not load libraries.');
        }
      });
    return () => controller.abort();
  }, []);

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h2 className="text-2xl font-bold tracking-tight">Music</h2>
          <p className="mt-1 text-sm text-secondary">
            Artists and albums from embedded tags in your indexed audio files.
          </p>
        </div>
        <LibraryPicker
          libraries={libraries}
          value={libraryId}
          onChange={(id) => {
            setLibraryId(id);
            setSelectedArtist(null);
          }}
        />
      </div>

      {librariesError && <ErrorState title="Could not load libraries" message={librariesError} />}

      {!librariesError && libraryId && (
        <CatalogBrowser
          libraryId={libraryId}
          selectedArtist={selectedArtist}
          onSelectArtist={setSelectedArtist}
        />
      )}

      {!librariesError && !libraryId && libraries !== null && (
        <EmptyState
          icon={<MusicIcon className="size-6" />}
          title="No libraries yet"
          description="Create a library and scan a music folder to fill this page."
        />
      )}

      <PlayerBar />
    </div>
  );
}

function LibraryPicker({
  libraries,
  value,
  onChange,
}: {
  libraries: Library[] | null;
  value: string;
  onChange: (id: string) => void;
}) {
  return (
    <label className="flex items-center gap-2 text-sm text-secondary">
      Library
      <select
        aria-label="Music library"
        value={value}
        onChange={(event) => onChange(event.target.value)}
        className="rounded-lg border border-border bg-surface px-3 py-2 text-sm shadow-card focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent"
      >
        <option value="" disabled>
          {libraries === null ? 'Loading…' : 'Choose a library'}
        </option>
        {(libraries ?? []).map((library) => (
          <option key={library.id} value={library.id}>
            {library.name}
          </option>
        ))}
      </select>
    </label>
  );
}

function CatalogBrowser({
  libraryId,
  selectedArtist,
  onSelectArtist,
}: {
  libraryId: string;
  selectedArtist: ArtistSummary | null;
  onSelectArtist: (artist: ArtistSummary | null) => void;
}) {
  const [artists, setArtists] = useState<ArtistSummary[] | null>(null);
  const [albums, setAlbums] = useState<AlbumSummary[] | null>(null);
  const [tracks, setTracks] = useState<TrackSummary[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [reload, setReload] = useState(0);

  useEffect(() => {
    const controller = new AbortController();
    setArtists(null);
    setError(null);
    fetchMusicArtists(libraryId, controller.signal)
      .then(setArtists)
      .catch((loadError: unknown) => {
        if (!controller.signal.aborted) {
          setError(loadError instanceof Error ? loadError.message : 'Could not load the music catalogue.');
        }
      });
    return () => controller.abort();
  }, [libraryId, reload]);

  useEffect(() => {
    const controller = new AbortController();
    setAlbums(null);
    setTracks(null);
    fetchMusicAlbums(libraryId, { artistId: selectedArtist?.id, signal: controller.signal })
      .then(setAlbums)
      .catch(() => undefined);
    fetchMusicTracks(libraryId, { artistId: selectedArtist?.id, signal: controller.signal })
      .then(setTracks)
      .catch(() => undefined);
    return () => controller.abort();
  }, [libraryId, selectedArtist?.id, reload]);

  if (error) {
    return <ErrorState title="Music catalogue unavailable" message={error} onRetry={() => setReload((n) => n + 1)} />;
  }

  if (artists === null) {
    return <LoadingState label="Loading music…" />;
  }

  if (artists.length === 0) {
    return (
      <EmptyState
        icon={<MusicIcon className="size-6" />}
        title="No tagged music yet"
        description="Scan a folder containing audio files with artist/album tags, then come back."
        action={
          <button
            type="button"
            onClick={() => setReload((n) => n + 1)}
            className="inline-flex items-center gap-2 rounded-lg border border-border bg-surface-elevated px-4 py-2 text-sm font-medium text-primary transition-colors hover:bg-surface-hover focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent"
          >
            Check again
          </button>
        }
      />
    );
  }

  return (
    <div className="space-y-8">
      <section aria-label="Artists">
        <h3 className="mb-3 text-sm font-semibold uppercase tracking-wide text-muted">Artists</h3>
        <div className="flex flex-wrap gap-2">
          <button
            type="button"
            onClick={() => onSelectArtist(null)}
            className={`rounded-full border px-4 py-1.5 text-sm transition-colors ${
              selectedArtist === null
                ? 'border-accent bg-accent-soft text-accent'
                : 'border-border bg-surface text-secondary hover:bg-surface-hover'
            }`}
          >
            All
          </button>
          {artists.map((artist) => (
            <button
              key={artist.id}
              type="button"
              onClick={() => onSelectArtist(artist)}
              className={`rounded-full border px-4 py-1.5 text-sm transition-colors ${
                selectedArtist?.id === artist.id
                  ? 'border-accent bg-accent-soft text-accent'
                  : 'border-border bg-surface text-secondary hover:bg-surface-hover'
              }`}
            >
              {artist.name}
            </button>
          ))}
        </div>
      </section>

      <section aria-label="Albums">
        <h3 className="mb-3 text-sm font-semibold uppercase tracking-wide text-muted">
          Albums{selectedArtist ? ` — ${selectedArtist.name}` : ''}
        </h3>
        {albums === null ? (
          <LoadingState label="Loading albums…" />
        ) : albums.length === 0 ? (
          <p className="text-sm text-muted">No albums for this filter.</p>
        ) : (
          <ul className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-5">
            {albums.map((album) => (
              <li key={album.id}>
                <AlbumCard album={album} />
              </li>
            ))}
          </ul>
        )}
      </section>

      <section aria-label="Tracks">
        <h3 className="mb-3 text-sm font-semibold uppercase tracking-wide text-muted">Tracks</h3>
        {tracks === null ? (
          <LoadingState label="Loading tracks…" />
        ) : tracks.length === 0 ? (
          <p className="text-sm text-muted">No tracks for this filter.</p>
        ) : (
          <ul className="divide-y divide-border overflow-hidden rounded-2xl border border-border bg-surface shadow-card">
            {tracks.map((track) => (
              <TrackRow key={track.id} track={track} />
            ))}
          </ul>
        )}
      </section>
    </div>
  );
}

function AlbumCard({ album }: { album: AlbumSummary }) {
  return (
    <figure className="overflow-hidden rounded-2xl border border-border bg-surface shadow-card transition-transform hover:-translate-y-0.5">
      {album.hasCover ? (
        <img
          src={albumCoverUrl(album.id)}
          alt={`Cover of ${album.title}`}
          loading="lazy"
          className="aspect-square w-full object-cover"
        />
      ) : (
        <div
          aria-hidden
          className="grid aspect-square w-full place-items-center bg-gradient-to-br from-accent-soft to-surface"
        >
          <MusicIcon className="size-10 text-accent opacity-70" />
        </div>
      )}
      <figcaption className="space-y-0.5 p-3">
        <p className="truncate text-sm font-medium">{album.title}</p>
        <p className="truncate text-xs text-muted">
          {album.artistName ?? 'Unknown artist'}
          {album.year ? ` · ${album.year}` : ''}
        </p>
      </figcaption>
    </figure>
  );
}

function TrackRow({ track }: { track: TrackSummary }) {
  const { current, toggle } = usePlayer();
  const isCurrent = current?.track.id === track.id;

  return (
    <li className="flex items-center gap-3 px-4 py-2.5">
      <button
        type="button"
        aria-label={isCurrent ? `Pause ${track.title}` : `Play ${track.title}`}
        onClick={() => toggle(track)}
        className={`icon-btn shrink-0 ${isCurrent ? 'text-accent' : ''}`}
      >
        <PlayIcon className="size-4" />
      </button>
      <span className="min-w-0 flex-1">
        <span className={`block truncate text-sm ${isCurrent ? 'font-semibold text-accent' : 'font-medium'}`}>
          {track.title}
        </span>
        <span className="block truncate text-xs text-muted">
          {track.artistName ?? 'Unknown artist'} · {track.albumTitle}
          {track.trackNumber ? ` · #${track.trackNumber}` : ''}
        </span>
      </span>
      <span className="shrink-0 text-xs tabular-nums text-muted">{formatDuration(track.durationSeconds)}</span>
    </li>
  );
}

/** Fixed bottom bar with the audio element and current-track info. */
function PlayerBar() {
  const { current, playing, toggle, stop, position, duration, seek } = usePlayer();

  if (!current) {
    return null;
  }

  return (
    <div
      className="fixed inset-x-0 bottom-0 z-40 border-t border-border bg-surface/95 backdrop-blur"
      data-testid="player-bar"
    >
      <div className="mx-auto flex max-w-6xl items-center gap-4 px-4 py-3">
        <button
          type="button"
          aria-label={playing ? 'Pause' : 'Play'}
          onClick={() => toggle(current.track)}
          className="icon-btn text-accent"
        >
          <PlayIcon className="size-5" />
        </button>
        <span className="hidden min-w-0 flex-1 sm:block">
          <span className="block truncate text-sm font-medium">{current.track.title}</span>
          <span className="block truncate text-xs text-muted">
            {current.track.artistName ?? 'Unknown artist'} · {current.track.albumTitle}
          </span>
        </span>
        <span className="text-xs tabular-nums text-muted">
          {formatDuration(position)} / {formatDuration(duration)}
        </span>
        <input
          aria-label="Seek"
          type="range"
          min={0}
          max={Math.max(1, Math.floor(duration))}
          value={Math.floor(position)}
          onChange={(event) => seek(Number(event.target.value))}
          className="hidden w-48 md:block"
        />
        <button type="button" aria-label="Stop" onClick={stop} className="icon-btn">
          ×
        </button>
      </div>
    </div>
  );
}
