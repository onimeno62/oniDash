import { useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { fetchLibraries, type Library } from '../api/libraries';
import {
  albumCoverUrl,
  addMusicFavorite,
  fetchMusicAlbums,
  fetchMusicArtists,
  fetchMusicFavorites,
  fetchMusicHistory,
  fetchMusicOverview,
  fetchMusicPlaylists,
  fetchMusicTopTracks,
  fetchMusicTracks,
  formatDuration,
  reindexMusic,
  removeMusicFavorite,
  type AlbumSummary,
  type ArtistSummary,
  type MusicFavorite,
  type MusicOverview,
  type MusicPlayHistory,
  type PlaylistSummary,
  type TrackSummary,
} from '../api/music';
import { MusicPlayerBar } from '../components/MusicPlayerBar';
import { MusicIcon } from '../components/icons';
import { ErrorState } from '../components/states/ErrorState';
import { LoadingState } from '../components/states/LoadingState';
import { EmptyState } from '../components/states/EmptyState';
import { usePlayer } from '../hooks/usePlayer';

type LayoutMode = 'standard' | 'bento';

function Cover({ albumId, fallback = false, className = '' }: { albumId?: string | null; fallback?: boolean; className?: string }) {
  if (!albumId || fallback) return <div className={`grid place-items-center bg-surface-elevated text-primary/25 ${className}`}><MusicIcon className="size-8" /></div>;
  return <img src={albumCoverUrl(albumId)} alt="" loading="lazy" className={`object-cover ${className}`} />;
}

function SectionHeader({ title, count, onSeeAll }: { title: string; count?: number; onSeeAll?: () => void }) {
  return <div className="mb-4 flex items-center justify-between gap-4">
    <div className="flex min-w-0 items-baseline gap-2"><h2 className="truncate text-lg font-semibold tracking-tight text-primary">{title}</h2>{count !== undefined && <span className="text-xs text-secondary">{count}</span>}</div>
    {onSeeAll && <button type="button" onClick={onSeeAll} className="shrink-0 rounded-lg px-2 py-1 text-xs font-medium text-secondary transition hover:bg-surface hover:text-primary">See all</button>}
  </div>;
}

function TrackCard({ track, active, onPlay, favorite, onFavorite }: { track: TrackSummary; active: boolean; onPlay: () => void; favorite: boolean; onFavorite: () => void }) {
  return <article className={`group relative min-w-[210px] rounded-2xl border p-3 transition ${active ? 'border-accent/40 bg-accent/[.07]' : 'border-border bg-surface/60 hover:-translate-y-0.5 hover:bg-surface-elevated'}`}>
    <button type="button" onClick={onPlay} className="relative block w-full overflow-hidden rounded-xl text-left" aria-label={`Play ${track.title}`}>
      <Cover albumId={track.albumId} fallback={!track.hasCover} className="aspect-square w-full rounded-xl" />
      <span className="absolute bottom-2 right-2 grid size-9 place-items-center rounded-full bg-primary text-background opacity-0 shadow-lg transition group-hover:opacity-100">▶</span>
    </button>
    <div className="mt-3 min-w-0 pr-7"><p className="truncate text-sm font-semibold text-primary">{track.title}</p><p className="mt-0.5 truncate text-xs text-secondary">{track.artistName ?? 'Unknown artist'}</p><p className="mt-1 truncate text-[11px] text-tertiary">{track.albumTitle} · {formatDuration(track.durationSeconds)}</p></div>
    <button type="button" aria-label={favorite ? 'Remove favorite' : 'Add favorite'} onClick={onFavorite} className={`absolute right-3 top-[calc(100%-3.6rem)] grid size-7 place-items-center rounded-full text-sm transition ${favorite ? 'text-accent' : 'text-tertiary hover:bg-surface-elevated hover:text-primary'}`}>{favorite ? '♥' : '♡'}</button>
  </article>;
}

function AlbumCard({ album, onOpen }: { album: AlbumSummary; onOpen: () => void }) {
  return <button type="button" onClick={onOpen} className="group min-w-[180px] text-left">
    <div className="overflow-hidden rounded-2xl border border-border bg-surface transition group-hover:-translate-y-0.5 group-hover:border-border-strong group-hover:bg-surface-elevated"><Cover albumId={album.id} fallback={!album.hasCover} className="aspect-square w-full transition duration-300 group-hover:scale-[1.02]" /></div>
    <p className="mt-3 truncate text-sm font-semibold text-primary">{album.title}</p><p className="mt-0.5 truncate text-xs text-secondary">{album.artistName ?? 'Unknown artist'}{album.year ? ` · ${album.year}` : ''}</p>
  </button>;
}

function ArtistCard({ artist, onOpen }: { artist: ArtistSummary; onOpen: () => void }) {
  return <button type="button" onClick={onOpen} className="group min-w-[150px] text-center">
    <div className="mx-auto grid aspect-square w-full place-items-center overflow-hidden rounded-full border border-border bg-surface-elevated text-3xl font-semibold text-primary/70 transition group-hover:-translate-y-0.5 group-hover:border-accent/30 group-hover:text-primary"><span>{artist.name.trim().slice(0, 1).toUpperCase() || '?'}</span></div>
    <p className="mt-3 truncate text-sm font-semibold text-primary">{artist.name}</p><p className="mt-0.5 text-xs text-secondary">Artist</p>
  </button>;
}

function PlaylistCard({ playlist, onOpen }: { playlist: PlaylistSummary; onOpen: () => void }) {
  return <button type="button" onClick={onOpen} className="group min-w-[220px] rounded-2xl border border-border bg-surface/60 p-4 text-left transition hover:-translate-y-0.5 hover:bg-surface-elevated">
    <div className="grid aspect-[1.45] place-items-center rounded-xl bg-surface-elevated text-3xl text-primary/25 transition group-hover:text-primary/40"><MusicIcon className="size-9" /></div>
    <p className="mt-3 truncate text-sm font-semibold text-primary">{playlist.name}</p><p className="mt-1 truncate text-xs text-secondary">{playlist.isSmart ? 'Smart playlist' : 'Playlist'}</p>
  </button>;
}

function Carousel({ children, className = '' }: { children: React.ReactNode; className?: string }) {
  return <div className={`flex snap-x gap-4 overflow-x-auto pb-2 [scrollbar-width:none] [&::-webkit-scrollbar]:hidden ${className}`}>{children}</div>;
}

export function MusicHomeDashboardPage() {
  const navigate = useNavigate();
  const player = usePlayer();
  const [libraries, setLibraries] = useState<Library[] | null>(null);
  const [libraryId, setLibraryId] = useState('');
  const [tracks, setTracks] = useState<TrackSummary[]>([]);
  const [albums, setAlbums] = useState<AlbumSummary[]>([]);
  const [artists, setArtists] = useState<ArtistSummary[]>([]);
  const [playlists, setPlaylists] = useState<PlaylistSummary[]>([]);
  const [history, setHistory] = useState<MusicPlayHistory[]>([]);
  const [top, setTop] = useState<Array<{ trackId: string; plays: number; playedSeconds: number }>>([]);
  const [favorites, setFavorites] = useState<MusicFavorite[]>([]);
  const [overview, setOverview] = useState<MusicOverview | null>(null);
  const [layout, setLayout] = useState<LayoutMode>(() => localStorage.getItem('onidash.music.dashboard.layout') === 'bento' ? 'bento' : 'standard');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const load = async (id: string) => {
    try {
      const [t, al, ar, p, h, tt, f, o] = await Promise.all([
        fetchMusicTracks(id, { limit: 1000 }), fetchMusicAlbums(id, { limit: 500 }), fetchMusicArtists(id, { limit: 500 }),
        fetchMusicPlaylists(), fetchMusicHistory({ limit: 50 }), fetchMusicTopTracks(20), fetchMusicFavorites(), fetchMusicOverview(),
      ]);
      setTracks(t); setAlbums(al); setArtists(ar); setPlaylists(p); setHistory(h); setTop(tt); setFavorites(f); setOverview(o); setError(null);
    } catch (e) { setError(e instanceof Error ? e.message : 'Could not load the music dashboard.'); }
  };

  useEffect(() => {
    const controller = new AbortController();
    fetchLibraries(controller.signal).then(all => { setLibraries(all); if (all[0]) setLibraryId(current => current || all[0].id); }).catch(e => { if (!controller.signal.aborted) setError(e instanceof Error ? e.message : 'Could not load libraries.'); });
    return () => controller.abort();
  }, []);
  useEffect(() => { if (libraryId) void load(libraryId); }, [libraryId]);
  useEffect(() => { localStorage.setItem('onidash.music.dashboard.layout', layout); }, [layout]);

  const favoriteIds = useMemo(() => new Set(favorites.filter(f => f.entityType === 'track').map(f => f.entityId)), [favorites]);
  const historyTracks = useMemo(() => history.map(h => tracks.find(t => t.id === h.trackId)).filter(Boolean) as TrackSummary[], [history, tracks]);
  const topTracks = useMemo(() => top.map(x => tracks.find(t => t.id === x.trackId)).filter(Boolean) as TrackSummary[], [top, tracks]);
  const recentlyAdded = useMemo(() => [...tracks].sort((a, b) => (b.year ?? 0) - (a.year ?? 0)).slice(0, 12), [tracks]);
  const continueTrack = player.current ?? historyTracks[0] ?? recentlyAdded[0] ?? null;
  const continueHistory = continueTrack ? history.find(h => h.trackId === continueTrack.id) : undefined;
  const continueProgress = player.current?.id === continueTrack?.id ? player.position : (continueHistory?.playedSeconds ?? 0);

  const toggleFavorite = async (track: TrackSummary) => {
    try { if (favoriteIds.has(track.id)) await removeMusicFavorite('track', track.id); else await addMusicFavorite('track', track.id); await load(libraryId); }
    catch (e) { setError(e instanceof Error ? e.message : 'Could not update favorite.'); }
  };

  const rescan = async () => {
    setBusy(true); try { await reindexMusic(libraryId); await load(libraryId); } catch (e) { setError(e instanceof Error ? e.message : 'Could not refresh the library.'); } finally { setBusy(false); }
  };

  const openStudio = () => navigate('/music/studio');
  const play = (track: TrackSummary) => player.toggle(track);
  const setDashboardLayout = (value: LayoutMode) => setLayout(value);

  if (error) return <ErrorState title="Music dashboard unavailable" message={error} onRetry={() => libraryId && void load(libraryId)} />;
  if (!libraries) return <LoadingState label="Loading Music…" />;
  if (!libraryId) return <EmptyState icon={<MusicIcon className="size-6" />} title="No music library" description="Create a music library and scan your audio folder to get started." />;

  return <div className="pb-24">
    <header className="mb-7 flex flex-col gap-4 border-b border-border pb-5 xl:flex-row xl:items-end xl:justify-between">
      <div><p className="text-xs font-semibold uppercase tracking-[.2em] text-accent">Music</p><h1 className="mt-1 text-3xl font-bold tracking-[-.035em] text-primary sm:text-4xl">Your library</h1><p className="mt-2 text-sm text-secondary">Pick up where you left off, then explore the music you keep close.</p></div>
      <div className="flex flex-wrap items-center gap-2">
        <select aria-label="Music library" value={libraryId} onChange={e => setLibraryId(e.target.value)} className="rounded-xl border border-border bg-surface px-3 py-2 text-sm text-primary outline-none focus:border-accent/50">{libraries.map(l => <option key={l.id} value={l.id}>{l.name}</option>)}</select>
        <button type="button" disabled={busy} onClick={() => void rescan()} className="rounded-xl border border-border bg-surface px-3 py-2 text-sm text-secondary transition hover:bg-surface-elevated hover:text-primary">{busy ? 'Refreshing…' : 'Refresh'}</button>
        <div className="flex rounded-xl border border-border bg-surface p-1" aria-label="Dashboard layout">
          <button type="button" onClick={() => setDashboardLayout('standard')} className={`rounded-lg px-3 py-1.5 text-xs font-medium ${layout === 'standard' ? 'bg-primary text-background' : 'text-secondary hover:text-primary'}`}>Standard</button>
          <button type="button" onClick={() => setDashboardLayout('bento')} className={`rounded-lg px-3 py-1.5 text-xs font-medium ${layout === 'bento' ? 'bg-primary text-background' : 'text-secondary hover:text-primary'}`}>Bento</button>
        </div>
      </div>
    </header>

    {layout === 'standard' ? <div className="space-y-10">
      <ContinueHero track={continueTrack} progress={continueProgress} onPlay={() => continueTrack && play(continueTrack)} favorite={continueTrack ? favoriteIds.has(continueTrack.id) : false} onFavorite={() => continueTrack && void toggleFavorite(continueTrack)} />
      <DashboardStats overview={overview} />
      {historyTracks.length > 0 && <section><SectionHeader title="Recently played" count={historyTracks.length} onSeeAll={openStudio} /><Carousel>{historyTracks.slice(0, 12).map(t => <TrackCard key={t.id} track={t} active={player.current?.id === t.id} onPlay={() => play(t)} favorite={favoriteIds.has(t.id)} onFavorite={() => void toggleFavorite(t)} />)}</Carousel></section>}
      {topTracks.length > 0 && <section><SectionHeader title="Most played" count={topTracks.length} onSeeAll={openStudio} /><Carousel>{topTracks.slice(0, 12).map(t => <TrackCard key={t.id} track={t} active={player.current?.id === t.id} onPlay={() => play(t)} favorite={favoriteIds.has(t.id)} onFavorite={() => void toggleFavorite(t)} />)}</Carousel></section>}
      {recentlyAdded.length > 0 && <section><SectionHeader title="Recently added" count={recentlyAdded.length} onSeeAll={openStudio} /><Carousel>{recentlyAdded.map(a => <AlbumCard key={a.id} album={albums.find(al => al.id === a.albumId) ?? { id: a.albumId ?? a.id, title: a.albumTitle, artistName: a.artistName, year: a.year, hasCover: a.hasCover }} onOpen={openStudio} />)}</Carousel></section>}
      {albums.length > 0 && <section><SectionHeader title="Albums" count={overview?.albums ?? albums.length} onSeeAll={openStudio} /><Carousel>{albums.slice(0, 16).map(a => <AlbumCard key={a.id} album={a} onOpen={openStudio} />)}</Carousel></section>}
      {artists.length > 0 && <section><SectionHeader title="Artists" count={overview?.artists ?? artists.length} onSeeAll={openStudio} /><Carousel>{artists.slice(0, 16).map(a => <ArtistCard key={a.id} artist={a} onOpen={openStudio} />)}</Carousel></section>}
      {playlists.length > 0 && <section><SectionHeader title="Playlists" count={overview?.playlists ?? playlists.length} onSeeAll={openStudio} /><Carousel>{playlists.slice(0, 12).map(p => <PlaylistCard key={p.id} playlist={p} onOpen={openStudio} />)}</Carousel></section>}
    </div> : <BentoDashboard track={continueTrack} progress={continueProgress} history={historyTracks} top={topTracks} albums={albums} artists={artists} playlists={playlists} favorites={favoriteIds} player={player} onPlay={play} onFavorite={toggleFavorite} onSeeAll={openStudio} />}

    <div className="mt-10 flex flex-wrap items-center justify-between gap-3 rounded-2xl border border-border bg-surface/50 px-4 py-3">
      <div><p className="text-sm font-medium text-primary">Need the full music workspace?</p><p className="text-xs text-secondary">Open the detailed library, playlists, history and management views.</p></div>
      <button type="button" onClick={openStudio} className="rounded-xl bg-primary px-4 py-2 text-xs font-semibold text-background transition hover:opacity-90">Open music studio</button>
    </div>
    <MusicPlayerBar />
  </div>;
}

function ContinueHero({ track, progress, onPlay, favorite, onFavorite }: { track: TrackSummary | null; progress: number; onPlay: () => void; favorite: boolean; onFavorite: () => void }) {
  if (!track) return <section className="overflow-hidden rounded-3xl border border-border bg-surface p-7 sm:p-10"><p className="text-xs font-semibold uppercase tracking-[.18em] text-accent">Continue listening</p><h2 className="mt-2 text-2xl font-semibold text-primary">Nothing queued yet</h2><p className="mt-2 max-w-lg text-sm leading-6 text-secondary">Start playing something from your library and oniDash will remember where you left off.</p></section>;
  const duration = track.durationSeconds ?? 0;
  const safeProgress = Math.min(Math.max(progress, 0), duration || 0);
  const percent = duration > 0 ? (safeProgress / duration) * 100 : 0;
  const audioMeta = [track.genre, track.year ? String(track.year) : null, track.durationSeconds ? formatDuration(track.durationSeconds) : null].filter(Boolean).join(' · ');
  return <section className="relative overflow-hidden rounded-3xl border border-border bg-surface">
    <div className="absolute inset-0 opacity-[.06]" style={track.albumId ? { backgroundImage: `url(${albumCoverUrl(track.albumId)})`, backgroundPosition: 'center', backgroundSize: 'cover', filter: 'blur(28px)' } : undefined} />
    <div className="relative grid items-center gap-7 p-5 sm:p-8 lg:grid-cols-[minmax(230px,310px)_1fr] lg:p-10">
      <div className="overflow-hidden rounded-2xl border border-border bg-surface-elevated shadow-2xl"><Cover albumId={track.albumId} fallback={!track.hasCover} className="aspect-square w-full" /></div>
      <div className="min-w-0"><p className="text-xs font-semibold uppercase tracking-[.2em] text-accent">Continue listening</p><h2 className="mt-3 truncate text-3xl font-bold tracking-[-.04em] text-primary sm:text-5xl">{track.title}</h2><p className="mt-2 truncate text-base text-secondary">{track.artistName ?? 'Unknown artist'} <span className="text-tertiary">·</span> {track.albumTitle}</p><div className="mt-5 flex flex-wrap gap-x-4 gap-y-2 text-xs text-tertiary">{audioMeta && <span>{audioMeta}</span>}{track.trackNumber && <span>Track {track.trackNumber}</span>}{track.discNumber && <span>Disc {track.discNumber}</span>}{track.rating > 0 && <span>{'★'.repeat(Math.min(5, Math.round(track.rating)))} </span>}</div>
        <div className="mt-7"><div className="h-1.5 overflow-hidden rounded-full bg-primary/10"><div className="h-full rounded-full bg-accent transition-all" style={{ width: `${percent}%` }} /></div><div className="mt-2 flex justify-between text-[11px] tabular-nums text-tertiary"><span>{formatDuration(safeProgress)}</span><span>{formatDuration(duration)}</span></div></div>
        <div className="mt-5 flex flex-wrap gap-2"><button type="button" onClick={onPlay} className="rounded-xl bg-primary px-5 py-2.5 text-sm font-semibold text-background transition hover:opacity-90">▶ {percent > 0 ? 'Continue' : 'Play'}</button><button type="button" onClick={onFavorite} className="rounded-xl border border-border bg-surface-elevated px-4 py-2.5 text-sm text-primary transition hover:bg-surface">{favorite ? '♥ Favorited' : '♡ Favorite'}</button></div>
      </div>
    </div>
  </section>;
}

function DashboardStats({ overview }: { overview: MusicOverview | null }) {
  const stats = [['Tracks', overview?.tracks ?? 0], ['Albums', overview?.albums ?? 0], ['Artists', overview?.artists ?? 0], ['Listening', overview ? `${Math.floor(overview.listeningSeconds / 3600)}h` : '0h']];
  return <div className="grid grid-cols-2 gap-3 md:grid-cols-4">{stats.map(([label, value]) => <div key={String(label)} className="rounded-2xl border border-border bg-surface/60 px-4 py-4"><p className="text-xs text-tertiary">{label}</p><p className="mt-1 text-xl font-semibold tracking-tight text-primary">{value}</p></div>)}</div>;
}

function BentoDashboard({ track, progress, history, top, albums, artists, playlists, favorites, player, onPlay, onFavorite, onSeeAll }: { track: TrackSummary | null; progress: number; history: TrackSummary[]; top: TrackSummary[]; albums: AlbumSummary[]; artists: ArtistSummary[]; playlists: PlaylistSummary[]; favorites: Set<string>; player: ReturnType<typeof usePlayer>; onPlay: (track: TrackSummary) => void; onFavorite: (track: TrackSummary) => Promise<void>; onSeeAll: () => void }) {
  return <div className="grid auto-rows-[minmax(170px,auto)] grid-cols-1 gap-5 md:grid-cols-2 xl:grid-cols-4">
    <div className="md:col-span-2 xl:col-span-3"><ContinueHero track={track} progress={progress} onPlay={() => track && onPlay(track)} favorite={track ? favorites.has(track.id) : false} onFavorite={() => track && void onFavorite(track)} /></div>
    <BentoPanel title="Library" className="xl:col-span-1" onClick={onSeeAll}><div className="grid h-full grid-cols-2 gap-2 sm:grid-cols-4 xl:grid-cols-2"><StatMini label="Tracks" value={history.length + top.length > 0 ? 'Ready' : '—'} /><StatMini label="Albums" value={albums.length} /><StatMini label="Artists" value={artists.length} /><StatMini label="Playlists" value={playlists.length} /></div></BentoPanel>
    <BentoPanel title="Recently played" className="md:col-span-2 xl:col-span-2" onClick={onSeeAll}><MiniTrackList tracks={history.slice(0, 5)} player={player} onPlay={onPlay} /></BentoPanel>
    <BentoPanel title="Most played" className="md:col-span-2 xl:col-span-2" onClick={onSeeAll}><MiniTrackList tracks={top.slice(0, 5)} player={player} onPlay={onPlay} /></BentoPanel>
    <BentoPanel title="Albums" className="md:col-span-2 xl:col-span-2" onClick={onSeeAll}><Carousel>{albums.slice(0, 6).map(a => <AlbumCard key={a.id} album={a} onOpen={onSeeAll} />)}</Carousel></BentoPanel>
    <BentoPanel title="Artists" className="md:col-span-2 xl:col-span-2" onClick={onSeeAll}><Carousel>{artists.slice(0, 6).map(a => <ArtistCard key={a.id} artist={a} onOpen={onSeeAll} />)}</Carousel></BentoPanel>
    <BentoPanel title="Playlists" className="md:col-span-2 xl:col-span-4" onClick={onSeeAll}><Carousel>{playlists.slice(0, 8).map(p => <PlaylistCard key={p.id} playlist={p} onOpen={onSeeAll} />)}</Carousel></BentoPanel>
  </div>;
}

function BentoPanel({ title, className = '', children, onClick }: { title: string; className?: string; children: React.ReactNode; onClick?: () => void }) {
  return <section className={`min-w-0 overflow-hidden rounded-3xl border border-border bg-surface/70 p-5 ${className}`}><div className="mb-4 flex items-center justify-between"><h2 className="text-sm font-semibold text-primary">{title}</h2>{onClick && <button type="button" onClick={onClick} className="text-xs text-secondary hover:text-primary">See all</button>}</div>{children}</section>;
}

function StatMini({ label, value }: { label: string; value: string | number }) { return <div className="rounded-xl bg-surface-elevated p-3"><p className="text-[11px] text-tertiary">{label}</p><p className="mt-1 text-sm font-semibold text-primary">{value}</p></div>; }

function MiniTrackList({ tracks, player, onPlay }: { tracks: TrackSummary[]; player: ReturnType<typeof usePlayer>; onPlay: (track: TrackSummary) => void }) {
  return <div className="space-y-1">{tracks.map((track, index) => <button type="button" key={track.id} onClick={() => onPlay(track)} className="flex w-full items-center gap-3 rounded-xl px-2 py-2 text-left transition hover:bg-surface-elevated"><span className="w-5 text-center text-xs tabular-nums text-tertiary">{String(index + 1).padStart(2, '0')}</span><Cover albumId={track.albumId} fallback={!track.hasCover} className="size-10 shrink-0 rounded-lg" /><span className="min-w-0 flex-1"><span className={`block truncate text-sm font-medium ${player.current?.id === track.id ? 'text-accent' : 'text-primary'}`}>{track.title}</span><span className="block truncate text-xs text-secondary">{track.artistName ?? 'Unknown artist'}</span></span><span className="text-xs tabular-nums text-tertiary">{formatDuration(track.durationSeconds)}</span></button>)}</div>;
}
