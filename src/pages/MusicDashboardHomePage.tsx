import { useEffect, useState } from 'react';
import { fetchLibraries, type Library } from '../api/libraries';
import {
  fetchMusicAlbums,
  fetchMusicArtists,
  fetchMusicHomeCollections,
  fetchMusicOverview,
  fetchMusicPlaylists,
  fetchMusicTrack,
  type AlbumSummary,
  type ArtistSummary,
  type MusicHomeCollections,
  type MusicOverview,
  type PlaylistSummary,
  type TrackSummary,
} from '../api/music';
import { MusicIcon } from '../components/icons';
import { ContinueHero } from '../components/music/ContinueHero';
import { MusicDiscovery } from '../components/music/MusicDiscovery';
import { MusicStatsOverview } from '../components/music/MusicStatsOverview';
import { TrackCarousel } from '../components/music/TrackCarousel';
import { MusicPlayerBar } from '../components/MusicPlayerBar';
import { EmptyState } from '../components/states/EmptyState';
import { ErrorState } from '../components/states/ErrorState';
import { LoadingState } from '../components/states/LoadingState';
import { usePlayer } from '../hooks/usePlayer';

type Layout = 'standard' | 'bento';

export function MusicDashboardHomePage() {
  const player = usePlayer();
  const [libraries, setLibraries] = useState<Library[] | null>(null);
  const [libraryId, setLibraryId] = useState('');
  const [home, setHome] = useState<MusicHomeCollections | null>(null);
  const [albums, setAlbums] = useState<AlbumSummary[]>([]);
  const [artists, setArtists] = useState<ArtistSummary[]>([]);
  const [playlists, setPlaylists] = useState<PlaylistSummary[]>([]);
  const [overview, setOverview] = useState<MusicOverview | null>(null);
  const [layout, setLayout] = useState<Layout>(() =>
    localStorage.getItem('onidash.music.dashboard.layout') === 'bento' ? 'bento' : 'standard'
  );
  const [error, setError] = useState<string | null>(null);
  const [continuedTrack, setContinuedTrack] = useState<TrackSummary | null>(null);

  const load = () => {
    if (!libraryId) return;
    const controller = new AbortController();
    Promise.all([
      fetchMusicHomeCollections(libraryId, 12, controller.signal),
      fetchMusicAlbums(libraryId, { limit: 12, signal: controller.signal }),
      fetchMusicArtists(libraryId, { limit: 12, signal: controller.signal }),
      fetchMusicPlaylists(controller.signal),
      fetchMusicOverview(controller.signal),
    ])
      .then(([h, a, ar, p, o]) => {
        setHome(h);
        setAlbums(a);
        setArtists(ar);
        setPlaylists(p);
        setOverview(o);
        setError(null);
      })
      .catch((e) => {
        if (!controller.signal.aborted) {
          setError(e instanceof Error ? e.message : 'Could not load Music Home.');
        }
      });
    return () => controller.abort();
  };

  useEffect(() => {
    const controller = new AbortController();
    fetchLibraries(controller.signal)
      .then((items) => {
        setLibraries(items);
        if (items[0]) setLibraryId((v) => v || items[0].id);
      })
      .catch((e) => {
        if (!controller.signal.aborted) {
          setError(e instanceof Error ? e.message : 'Could not load libraries.');
        }
      });
    return () => controller.abort();
  }, []);

  useEffect(() => {
    const cancel = load();
    return () => cancel?.();
  }, [libraryId]);

  useEffect(() => {
    localStorage.setItem('onidash.music.dashboard.layout', layout);
  }, [layout]);

  const continueItem = home?.continueListening[0];
  useEffect(() => {
    let active = true;
    if (!continueItem) {
      setContinuedTrack(null);
      return;
    }
    if (player.current?.id === continueItem.trackId) {
      setContinuedTrack(player.current);
      return;
    }
    const controller = new AbortController();
    fetchMusicTrack(continueItem.trackId, controller.signal)
      .then((track) => {
        if (active) setContinuedTrack(track);
      })
      .catch(() => {
        if (active) setContinuedTrack(null);
      });
    return () => {
      active = false;
      controller.abort();
    };
  }, [continueItem?.trackId, player.current?.id]);

  const continueTrack = player.current ?? continuedTrack;
  const continuePosition =
    continueTrack?.id === player.current?.id ? player.position : continueItem?.positionSeconds ?? 0;
  const play = (track: TrackSummary) => player.toggle(track);

  if (error && !home) {
    return <ErrorState title="Music Home unavailable" message={error} onRetry={load} />;
  }
  if (!libraries) {
    return <LoadingState label="Loading Music Library…" />;
  }
  if (!libraryId) {
    return (
      <EmptyState
        icon={<MusicIcon className="size-6" />}
        title="No music library"
        description="Create a music library and scan your audio folder to get started."
      />
    );
  }
  if (!home) {
    return <LoadingState label="Scanning your music collection…" />;
  }

  // Bento layout grouping vs Standard Carousel layout
  return (
    <div className="mx-auto max-w-[1600px] space-y-10 pb-28">
      {/* Editorial Header */}
      <header className="flex flex-col gap-5 border-b border-border/70 pb-6 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <div className="flex items-center gap-2">
            <span className="size-2 rounded-full bg-accent" />
            <p className="text-xs font-bold uppercase tracking-[0.25em] text-accent">
              oniDash Audio
            </p>
          </div>
          <h1 className="mt-2 text-3xl font-extrabold tracking-tight text-primary sm:text-4xl">
            Music Dashboard
          </h1>
          <p className="mt-1 text-sm text-secondary">
            Your personal, local-first audiophile collection with synchronized lyrics and lossless playback.
          </p>
        </div>

        {/* Library Switcher & Layout Toggle */}
        <div className="flex flex-wrap items-center gap-3">
          <div className="flex items-center gap-2">
            <label htmlFor="library-select" className="text-xs font-semibold text-tertiary">
              Library:
            </label>
            <select
              id="library-select"
              aria-label="Music library"
              value={libraryId}
              onChange={(e) => setLibraryId(e.target.value)}
              className="rounded-xl border border-border/80 bg-surface/90 px-3 py-2 text-xs font-medium text-primary shadow-sm focus:border-accent focus:outline-none focus:ring-1 focus:ring-accent"
            >
              {libraries.map((l) => (
                <option key={l.id} value={l.id}>
                  {l.name}
                </option>
              ))}
            </select>
          </div>

          <div className="flex rounded-xl border border-border/80 bg-surface/90 p-1 shadow-sm">
            <button
              type="button"
              id="layout-standard-btn"
              onClick={() => setLayout('standard')}
              className={`rounded-lg px-3 py-1.5 text-xs font-medium transition-all ${
                layout === 'standard'
                  ? 'bg-accent text-white shadow-sm'
                  : 'text-secondary hover:text-primary'
              }`}
            >
              Standard
            </button>
            <button
              type="button"
              id="layout-bento-btn"
              onClick={() => setLayout('bento')}
              className={`rounded-lg px-3 py-1.5 text-xs font-medium transition-all ${
                layout === 'bento'
                  ? 'bg-accent text-white shadow-sm'
                  : 'text-secondary hover:text-primary'
              }`}
            >
              Bento Grid
            </button>
          </div>
        </div>
      </header>

      {/* Hero Continue Section */}
      <ContinueHero
        track={continueTrack}
        progress={continuePosition}
        isPlaying={player.playing && player.current?.id === continueTrack?.id}
        onPlay={() => continueTrack && play(continueTrack)}
      />

      {/* Main Content Layout */}
      {layout === 'standard' ? (
        <div className="space-y-12">
          {/* Recently Played */}
          <TrackCarousel
            title="Recently Played"
            subtitle="Pick up where you left off in your latest listening sessions"
            tracks={home.recentlyPlayed}
            currentPlayingId={player.current?.id}
            isPlaying={player.playing}
            onPlay={play}
          />

          {/* Favorites */}
          <TrackCarousel
            title="Your Favorites"
            subtitle="Tracks marked with top appreciation in your library"
            tracks={home.favorites}
            currentPlayingId={player.current?.id}
            isPlaying={player.playing}
            onPlay={play}
          />

          {/* Most Played */}
          <TrackCarousel
            title="Heavy Rotation"
            subtitle="Most played tracks across your local catalogue"
            tracks={home.mostPlayed}
            currentPlayingId={player.current?.id}
            isPlaying={player.playing}
            onPlay={play}
          />

          {/* Recently Added */}
          <TrackCarousel
            title="Recently Added"
            subtitle="Fresh audio scans and latest imports"
            tracks={home.recentlyAdded}
            currentPlayingId={player.current?.id}
            isPlaying={player.playing}
            onPlay={play}
          />

          {/* Top Rated */}
          <TrackCarousel
            title="Top Rated"
            subtitle="Audiophile 5-star picks"
            tracks={home.topRated}
            currentPlayingId={player.current?.id}
            isPlaying={player.playing}
            onPlay={play}
          />

          {/* Discovery: Albums, Artists, Playlists */}
          <MusicDiscovery albums={albums} artists={artists} playlists={playlists} />

          {/* Server-calculated SQLite Telemetry */}
          <MusicStatsOverview overview={overview} />
        </div>
      ) : (
        /* Bento Grid Layout */
        <div className="grid gap-8 lg:grid-cols-12">
          {/* Heavy Rotation & Recently Played (8 cols) */}
          <div className="space-y-8 lg:col-span-8">
            <TrackCarousel
              title="Heavy Rotation"
              subtitle="Your most spun records and files"
              tracks={home.mostPlayed}
              currentPlayingId={player.current?.id}
              isPlaying={player.playing}
              onPlay={play}
            />

            <TrackCarousel
              title="Recently Added"
              subtitle="New additions to this audio vault"
              tracks={home.recentlyAdded}
              currentPlayingId={player.current?.id}
              isPlaying={player.playing}
              onPlay={play}
            />

            <MusicDiscovery albums={albums} artists={artists} playlists={playlists} />
          </div>

          {/* Quick Favorites Sidebar & Stats (4 cols) */}
          <div className="space-y-8 lg:col-span-4">
            <section className="rounded-3xl border border-border/80 bg-surface/80 p-5 shadow-sm">
              <div className="mb-4 flex items-center justify-between">
                <h3 className="text-base font-bold text-primary">Starred Favorites</h3>
                <span className="text-xs text-tertiary">{home.favorites.length} saved</span>
              </div>
              <div className="space-y-2">
                {home.favorites.slice(0, 6).map((track) => (
                  <div
                    key={track.id}
                    onClick={() => play(track)}
                    className="group flex cursor-pointer items-center justify-between rounded-xl border border-border/50 bg-surface/50 p-2.5 transition-colors hover:border-accent/40 hover:bg-surface-elevated"
                  >
                    <div className="min-w-0 pr-2">
                      <p className="truncate text-xs font-semibold text-primary group-hover:text-accent">
                        {track.title}
                      </p>
                      <p className="truncate text-[11px] text-secondary">
                        {track.artistName ?? 'Unknown'}
                      </p>
                    </div>
                    <button
                      type="button"
                      className="flex size-7 shrink-0 items-center justify-center rounded-full bg-accent/15 text-accent group-hover:bg-accent group-hover:text-white"
                    >
                      ▶
                    </button>
                  </div>
                ))}
              </div>
            </section>

            <MusicStatsOverview overview={overview} />
          </div>
        </div>
      )}

      {/* Persistent Audio Player Bar */}
      <MusicPlayerBar />
    </div>
  );
}
