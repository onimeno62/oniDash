import { useEffect, useMemo, useState } from 'react';
import { fetchLibraries, type Library } from '../api/libraries';
import {
  fetchMusicAlbums,
  fetchMusicArtists,
  fetchMusicFavorites,
  fetchMusicGenres,
  fetchMusicHistory,
  fetchMusicOverview,
  fetchMusicPlaylists,
  fetchMusicTopTracks,
  fetchMusicTracks,
  formatDuration,
  reindexMusic,
  type AlbumSummary,
  type ArtistSummary,
  type MusicOverview,
  type PlaylistSummary,
  type TrackSummary,
} from '../api/music';
import { usePlayer } from '../hooks/usePlayer';
import { MusicPlayerBar, QueueButton } from '../components/MusicPlayerBar';
import { MusicTrackActions } from '../components/MusicTrackActions';
import { MusicMetadataEditor } from '../components/MusicMetadataEditor';
import { ErrorState } from '../components/states/ErrorState';
import { LoadingState } from '../components/states/LoadingState';
import { EmptyState } from '../components/states/EmptyState';
import { MusicIcon, PlayIcon } from '../components/icons';

type View = 'home' | 'library' | 'artists' | 'albums' | 'genres' | 'playlists' | 'favorites' | 'history' | 'insights';

const navGroups: Array<{ label: string; items: Array<{ id: View; label: string; icon: string }> }> = [
  { label: 'Discover', items: [{ id: 'home', label: 'Home', icon: '⌂' }, { id: 'genres', label: 'Browse genres', icon: '◈' }] },
  { label: 'Library', items: [{ id: 'library', label: 'All songs', icon: '♫' }, { id: 'albums', label: 'Albums', icon: '▣' }, { id: 'artists', label: 'Artists', icon: '♙' }, { id: 'playlists', label: 'Playlists', icon: '≡' }] },
  { label: 'Collection', items: [{ id: 'favorites', label: 'Favorites', icon: '♡' }, { id: 'history', label: 'Recently played', icon: '◷' }] },
  { label: 'Analytics', items: [{ id: 'insights', label: 'Listening insights', icon: '⌁' }] },
];

export function MusicDashboardPage() {
  const [libraries, setLibraries] = useState<Library[] | null>(null);
  const [libraryId, setLibraryId] = useState('');
  const [view, setView] = useState<View>('home');
  const [artists, setArtists] = useState<ArtistSummary[]>([]);
  const [albums, setAlbums] = useState<AlbumSummary[]>([]);
  const [tracks, setTracks] = useState<TrackSummary[]>([]);
  const [playlists, setPlaylists] = useState<PlaylistSummary[]>([]);
  const [overview, setOverview] = useState<MusicOverview | null>(null);
  const [genres, setGenres] = useState<Array<{ genre: string; tracks: number }>>([]);
  const [favorites, setFavorites] = useState<Array<{ id: string; entityType: string; entityId: string }>>([]);
  const [history, setHistory] = useState<Array<{ id: string; trackId: string; playedSeconds: number; completionRatio: number; startedAtUtc: string }>>([]);
  const [top, setTop] = useState<Array<{ trackId: string; plays: number; playedSeconds: number }>>([]);
  const [search, setSearch] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const load = async (id: string) => {
    try {
      const [a, al, t, o, p, g, f, h, tt] = await Promise.all([
        fetchMusicArtists(id, { limit: 500 }),
        fetchMusicAlbums(id, { limit: 500 }),
        fetchMusicTracks(id, { limit: 1000 }),
        fetchMusicOverview(),
        fetchMusicPlaylists(),
        fetchMusicGenres(),
        fetchMusicFavorites(),
        fetchMusicHistory({ limit: 50 }),
        fetchMusicTopTracks(10),
      ]);
      setArtists(a); setAlbums(al); setTracks(t); setOverview(o); setPlaylists(p); setGenres(g); setFavorites(f); setHistory(h); setTop(tt); setError(null);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Could not load music dashboard.');
    }
  };

  useEffect(() => {
    const controller = new AbortController();
    fetchLibraries(controller.signal).then((all) => {
      setLibraries(all);
      if (all[0]) setLibraryId((current) => current || all[0].id);
    }).catch((e) => {
      if (!controller.signal.aborted) setError(e instanceof Error ? e.message : 'Could not load libraries.');
    });
    return () => controller.abort();
  }, []);

  useEffect(() => { if (libraryId) void load(libraryId); }, [libraryId]);

  const favoriteIds = useMemo(() => new Set(favorites.filter((f) => f.entityType === 'track').map((f) => f.entityId)), [favorites]);
  const filteredTracks = useMemo(() => {
    const q = search.trim().toLowerCase();
    return tracks.filter((t) => !q || `${t.title} ${t.artistName ?? ''} ${t.albumTitle} ${t.genre ?? ''}`.toLowerCase().includes(q));
  }, [tracks, search]);
  const topTracks = useMemo(() => top.map((x) => tracks.find((t) => t.id === x.trackId)).filter(Boolean) as TrackSummary[], [top, tracks]);
  const spotlight = albums[0];
  const recentAlbums = albums.slice(0, 6);
  const popularArtists = artists.slice(0, 8);

  const rescan = async () => {
    setBusy(true);
    try { await reindexMusic(libraryId); await load(libraryId); } catch (e) { setError(e instanceof Error ? e.message : 'Could not rescan music.'); } finally { setBusy(false); }
  };

  if (error) return <ErrorState title="Music dashboard unavailable" message={error} onRetry={() => libraryId && void load(libraryId)} />;
  if (!libraries) return <LoadingState label="Loading Music…" />;
  if (!libraryId) return <EmptyState icon={<MusicIcon className="size-6" />} title="No music library" description="Create a music library and scan your audio folder to get started." />;

  return (
    <div className="min-h-[calc(100vh-1rem)] pb-24">
      <div className="grid min-h-[calc(100vh-2rem)] grid-cols-1 overflow-hidden rounded-[28px] border border-border bg-[#090a0f] shadow-pop lg:grid-cols-[230px_minmax(0,1fr)]">
        <aside className="hidden border-r border-white/5 bg-[#101117] lg:flex lg:flex-col">
          <div className="flex items-center gap-3 border-b border-white/5 px-5 py-5">
            <div className="grid size-9 place-items-center rounded-xl bg-accent text-white shadow-lg"><MusicIcon className="size-5" /></div>
            <div><p className="font-semibold text-white">oniDash</p><p className="text-[11px] text-white/40">MUSIC LIBRARY</p></div>
          </div>
          <nav className="flex-1 space-y-6 overflow-y-auto px-3 py-6">
            {navGroups.map((group) => <div key={group.label}>
              <p className="px-3 pb-2 text-[10px] font-semibold uppercase tracking-[.18em] text-white/30">{group.label}</p>
              <div className="space-y-1">{group.items.map((item) => <button key={item.id} type="button" onClick={() => setView(item.id)} className={`flex w-full items-center gap-3 rounded-xl px-3 py-2.5 text-left text-sm transition ${view === item.id ? 'bg-accent text-white shadow-lg shadow-accent/20' : 'text-white/55 hover:bg-white/5 hover:text-white'}`}><span className="w-5 text-center text-base">{item.icon}</span>{item.label}</button>)}</div>
            </div>)}
          </nav>
          <div className="m-3 rounded-2xl border border-white/5 bg-white/[.03] p-3">
            <div className="flex items-center justify-between"><span className="text-xs text-white/50">Library</span><span className="size-2 rounded-full bg-success" /></div>
            <p className="mt-2 truncate text-sm font-medium text-white">{libraries.find((l) => l.id === libraryId)?.name ?? 'Music'}</p>
            <button type="button" onClick={() => void rescan()} disabled={busy} className="mt-3 w-full rounded-lg bg-white/5 px-3 py-2 text-xs text-white/70 hover:bg-white/10">{busy ? 'Scanning…' : 'Refresh library'}</button>
          </div>
        </aside>

        <main className="min-w-0 bg-[radial-gradient(circle_at_70%_-10%,rgba(124,106,245,.12),transparent_35%),#090a0f]">
          <header className="sticky top-0 z-20 flex items-center gap-3 border-b border-white/5 bg-[#090a0f]/90 px-4 py-4 backdrop-blur-xl sm:px-6">
            <div className="lg:hidden"><MusicIcon className="size-5 text-accent" /></div>
            <div className="relative min-w-0 flex-1 max-w-2xl"><span className="pointer-events-none absolute left-4 top-1/2 -translate-y-1/2 text-white/35">⌕</span><input value={search} onChange={(e) => setSearch(e.target.value)} placeholder="Search your music…" className="w-full rounded-xl border border-white/5 bg-white/[.045] py-2.5 pl-10 pr-4 text-sm text-white outline-none placeholder:text-white/30 focus:border-accent/50" /></div>
            <select aria-label="Music library" value={libraryId} onChange={(e) => setLibraryId(e.target.value)} className="hidden rounded-xl border border-white/5 bg-white/[.045] px-3 py-2.5 text-sm text-white/70 md:block">{libraries.map((l) => <option key={l.id} value={l.id} className="bg-[#11131a]">{l.name}</option>)}</select>
            <button type="button" aria-label="Refresh library" onClick={() => void rescan()} disabled={busy} className="icon-btn text-white/60 hover:text-white">↻</button>
            <div className="hidden size-8 place-items-center rounded-full bg-gradient-to-br from-accent to-fuchsia-500 text-xs font-bold text-white sm:grid">O</div>
          </header>

          <div className="space-y-10 p-4 sm:p-6 xl:p-8">
            {view === 'home' && <HomeView spotlight={spotlight} overview={overview} albums={recentAlbums} artists={popularArtists} topTracks={topTracks} playlists={playlists} onNavigate={setView} />}
            {view === 'library' && <LibraryView tracks={filteredTracks} albums={albums} favoriteIds={favoriteIds} onReload={() => void load(libraryId)} />}
            {view === 'artists' && <ArtistsView artists={artists} />}
            {view === 'albums' && <AlbumsView albums={albums} />}
            {view === 'genres' && <GenresView genres={genres} tracks={tracks} />}
            {view === 'playlists' && <PlaylistsView playlists={playlists} />}
            {view === 'favorites' && <LibraryView tracks={tracks.filter((t) => favoriteIds.has(t.id))} albums={albums} favoriteIds={favoriteIds} onReload={() => void load(libraryId)} title="Your favorites" />}
            {view === 'history' && <HistoryView history={history} tracks={tracks} />}
            {view === 'insights' && <InsightsView overview={overview} genres={genres} topTracks={topTracks} />}
          </div>
        </main>
      </div>
      <MusicPlayerBar />
    </div>
  );
}

function SectionHeading({ title, subtitle, action, onAction }: { title: string; subtitle?: string; action?: string; onAction?: () => void }) {
  return <div className="mb-4 flex items-end justify-between gap-4"><div><h2 className="text-xl font-semibold tracking-tight text-white">{title}</h2>{subtitle && <p className="mt-1 text-sm text-white/40">{subtitle}</p>}</div>{action && <button type="button" onClick={onAction} className="text-xs font-medium text-accent hover:text-accent-hover">{action} →</button>}</div>;
}

function HomeView({ spotlight, overview, albums, artists, topTracks, playlists, onNavigate }: { spotlight?: AlbumSummary; overview: MusicOverview | null; albums: AlbumSummary[]; artists: ArtistSummary[]; topTracks: TrackSummary[]; playlists: PlaylistSummary[]; onNavigate: (view: View) => void }) {
  return <div className="space-y-10">
    <section className="relative overflow-hidden rounded-[28px] border border-white/10 bg-gradient-to-br from-[#251d48] via-[#171528] to-[#0e1017] p-6 sm:p-8 xl:p-10">
      <div className="absolute -right-24 -top-32 size-80 rounded-full bg-accent/20 blur-3xl" />
      <div className="relative grid items-center gap-8 md:grid-cols-[minmax(0,1fr)_240px]">
        <div><p className="text-[11px] font-semibold uppercase tracking-[.24em] text-accent">Your music universe</p><h1 className="mt-3 max-w-2xl text-4xl font-bold tracking-[-.04em] text-white sm:text-5xl">Everything you love,<br /><span className="text-white/50">beautifully organized.</span></h1><p className="mt-4 max-w-xl text-sm leading-6 text-white/55">A local-first listening space for your albums, artists, playlists and history — with the power to manage the files behind your collection.</p><div className="mt-7 flex flex-wrap gap-3"><button type="button" onClick={() => onNavigate('library')} className="rounded-xl bg-white px-5 py-2.5 text-sm font-semibold text-[#11131a] hover:bg-white/90">Explore library</button><button type="button" onClick={() => onNavigate('playlists')} className="rounded-xl border border-white/10 bg-white/5 px-5 py-2.5 text-sm font-medium text-white hover:bg-white/10">Open playlists</button></div></div>
        {spotlight ? <div className="mx-auto w-full max-w-[220px] rotate-2 rounded-2xl border border-white/10 bg-white/5 p-2 shadow-2xl shadow-black/40"><AlbumArt album={spotlight} className="rounded-xl" /><div className="px-2 pb-1 pt-3"><p className="truncate text-sm font-semibold text-white">{spotlight.title}</p><p className="truncate text-xs text-white/40">{spotlight.artistName ?? 'Unknown artist'}</p></div></div> : <div className="grid h-48 place-items-center rounded-2xl border border-dashed border-white/10 text-white/30"><MusicIcon className="size-12" /></div>}
      </div>
    </section>

    <section className="grid grid-cols-2 gap-3 md:grid-cols-4">
      <Metric label="Tracks" value={overview?.tracks ?? 0} detail="in your library" />
      <Metric label="Albums" value={overview?.albums ?? 0} detail="ready to play" />
      <Metric label="Artists" value={overview?.artists ?? 0} detail="in your collection" />
      <Metric label="Listening" value={overview ? `${Math.floor(overview.listeningSeconds / 3600)}h` : '0h'} detail="total history" />
    </section>

    <section><SectionHeading title="Recently added" subtitle="Freshly discovered in your library" action="View all" onAction={() => onNavigate('albums')} /><div className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-6">{albums.map((album) => <AlbumCard key={album.id} album={album} />)}</div></section>
    <section><SectionHeading title="Popular right now" subtitle="Your most-played tracks" action="See all" onAction={() => onNavigate('library')} /><TrackList tracks={topTracks} compact /></section>

    <div className="grid gap-6 xl:grid-cols-[1.4fr_1fr]">
      <section className="rounded-2xl border border-white/5 bg-white/[.025] p-5"><SectionHeading title="Artists" action="All artists" onAction={() => onNavigate('artists')} /><div className="grid grid-cols-2 gap-3 sm:grid-cols-4">{artists.slice(0, 8).map((artist) => <ArtistCard key={artist.id} artist={artist} />)}</div></section>
      <section className="rounded-2xl border border-white/5 bg-white/[.025] p-5"><SectionHeading title="Your playlists" action="Manage" onAction={() => onNavigate('playlists')} /><div className="space-y-2">{playlists.slice(0, 5).map((playlist, index) => <div key={playlist.id} className="flex items-center gap-3 rounded-xl p-2 hover:bg-white/5"><div className="grid size-10 place-items-center rounded-lg bg-gradient-to-br from-accent/60 to-fuchsia-500/40 text-white">{index + 1}</div><div className="min-w-0 flex-1"><p className="truncate text-sm font-medium text-white">{playlist.name}</p><p className="text-xs text-white/35">{playlist.isSmart ? 'Smart playlist' : 'Manual playlist'}</p></div><span className="text-white/25">›</span></div>)}{!playlists.length && <p className="py-8 text-center text-sm text-white/30">No playlists yet.</p>}</div></section>
    </div>
  </div>;
}

function Metric({ label, value, detail }: { label: string; value: string | number; detail: string }) { return <div className="rounded-2xl border border-white/5 bg-white/[.025] p-4"><p className="text-xs text-white/35">{label}</p><p className="mt-2 text-2xl font-bold tracking-tight text-white">{value}</p><p className="mt-1 text-[11px] text-white/25">{detail}</p></div>; }

function AlbumArt({ album, className = '' }: { album: AlbumSummary; className?: string }) { return album.hasCover ? <img src={`/api/music/albums/${album.id}/cover`} alt={album.title} className={`aspect-square w-full object-cover ${className}`} /> : <div className={`grid aspect-square w-full place-items-center bg-gradient-to-br from-accent/50 via-[#24253b] to-[#11131a] ${className}`}><MusicIcon className="size-10 text-white/50" /></div>; }
function AlbumCard({ album }: { album: AlbumSummary }) { return <button type="button" className="group min-w-0 text-left"><div className="relative overflow-hidden rounded-2xl shadow-lg shadow-black/20"><AlbumArt album={album} className="transition duration-300 group-hover:scale-[1.04]" /><span className="absolute bottom-2 right-2 grid size-9 translate-y-2 place-items-center rounded-full bg-white text-black opacity-0 shadow-xl transition group-hover:translate-y-0 group-hover:opacity-100">▶</span></div><p className="mt-3 truncate text-sm font-medium text-white">{album.title}</p><p className="truncate text-xs text-white/35">{album.artistName ?? 'Unknown artist'}{album.year ? ` · ${album.year}` : ''}</p></button>; }
function ArtistCard({ artist }: { artist: ArtistSummary }) { return <button type="button" className="group flex min-w-0 items-center gap-3 rounded-xl p-2 text-left hover:bg-white/5"><div className="grid size-11 shrink-0 place-items-center rounded-full bg-gradient-to-br from-accent/70 to-fuchsia-500/40 text-sm font-semibold text-white">{artist.name.slice(0, 1).toUpperCase()}</div><p className="truncate text-sm font-medium text-white/80 group-hover:text-white">{artist.name}</p></button>; }

function LibraryView({ tracks, albums, favoriteIds, onReload, title = 'All songs' }: { tracks: TrackSummary[]; albums: AlbumSummary[]; favoriteIds: Set<string>; onReload: () => void; title?: string }) {
  return <div className="space-y-8"><div><p className="text-[11px] font-semibold uppercase tracking-[.2em] text-accent">Your collection</p><h1 className="mt-2 text-3xl font-bold tracking-tight text-white">{title}</h1><p className="mt-2 text-sm text-white/40">{tracks.length} tracks · {albums.length} albums</p></div><section><SectionHeading title="Albums" /><div className="grid grid-cols-2 gap-4 sm:grid-cols-4 lg:grid-cols-6">{albums.slice(0, 18).map((album) => <AlbumCard key={album.id} album={album} />)}</div></section><section><SectionHeading title="Tracks" subtitle="Play, queue, edit metadata or manage files" /><TrackList tracks={tracks} favoriteIds={favoriteIds} onReload={onReload} /></section></div>;
}

function TrackList({ tracks, favoriteIds, onReload, compact = false }: { tracks: TrackSummary[]; favoriteIds?: Set<string>; onReload?: () => void; compact?: boolean }) { const { current, toggle, playQueue } = usePlayer(); return <div className="overflow-hidden rounded-2xl border border-white/5 bg-white/[.025]"><div className="hidden grid-cols-[40px_minmax(0,1fr)_minmax(120px,.6fr)_80px_100px] gap-3 border-b border-white/5 px-4 py-3 text-[10px] font-semibold uppercase tracking-wider text-white/25 sm:grid"><span>#</span><span>Track</span><span>Album</span><span>Time</span><span /></div>{tracks.length === 0 ? <div className="p-8 text-center text-sm text-white/30">No tracks found.</div> : tracks.slice(0, compact ? 8 : undefined).map((track, index) => { const active = current?.track.id === track.id; return <div key={track.id} className={`grid items-center gap-3 px-3 py-2.5 sm:grid-cols-[40px_minmax(0,1fr)_minmax(120px,.6fr)_80px_100px] sm:px-4 ${active ? 'bg-accent/10' : 'hover:bg-white/[.035]'}`}><button type="button" aria-label={active ? `Pause ${track.title}` : `Play ${track.title}`} onClick={() => active ? toggle(track) : playQueue(tracks, index)} className={`grid size-8 place-items-center rounded-full ${active ? 'bg-accent text-white' : 'text-white/25 hover:bg-white/10 hover:text-white'}`}>{active ? 'Ⅱ' : String(index + 1).padStart(2, '0')}</button><div className="min-w-0"><p className="truncate text-sm font-medium text-white">{track.title}</p><p className="truncate text-xs text-white/35">{track.artistName ?? 'Unknown artist'}{favoriteIds?.has(track.id) ? ' · ♥' : ''}</p></div><p className="hidden truncate text-xs text-white/35 sm:block">{track.albumTitle}</p><span className="text-right text-xs tabular-nums text-white/30">{formatDuration(track.durationSeconds)}</span><div className="flex justify-end gap-1"><QueueButton track={track} />{!compact && <><MusicMetadataEditor track={track} onSaved={onReload ?? (() => undefined)} /><MusicTrackActions track={track} onChanged={onReload ?? (() => undefined)} /></>}</div></div>; })}</div>; }

function ArtistsView({ artists }: { artists: ArtistSummary[] }) { return <div><SectionHeading title="Artists" subtitle={`${artists.length} artists in your library`} /><div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">{artists.map((artist) => <ArtistCard key={artist.id} artist={artist} />)}</div></div>; }
function AlbumsView({ albums }: { albums: AlbumSummary[] }) { return <div><SectionHeading title="Albums" subtitle={`${albums.length} albums in your library`} /><div className="grid grid-cols-2 gap-5 sm:grid-cols-3 lg:grid-cols-5 xl:grid-cols-6">{albums.map((album) => <AlbumCard key={album.id} album={album} />)}</div></div>; }
function GenresView({ genres, tracks }: { genres: Array<{ genre: string; tracks: number }>; tracks: TrackSummary[] }) { const fallback = tracks.reduce<Record<string, number>>((a, t) => { if (t.genre) a[t.genre] = (a[t.genre] ?? 0) + 1; return a; }, {}); const data = genres.length ? genres : Object.entries(fallback).map(([genre, count]) => ({ genre, tracks: count })).sort((a, b) => b.tracks - a.tracks); const max = Math.max(1, ...data.map((x) => x.tracks)); return <div><SectionHeading title="Browse by genre" subtitle="Explore the character of your collection" /><div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">{data.map((genre, index) => <div key={genre.genre} className="relative overflow-hidden rounded-2xl border border-white/5 bg-gradient-to-br from-white/[.06] to-white/[.015] p-5"><div className="absolute -right-8 -top-8 size-24 rounded-full bg-accent/10 blur-2xl" /><p className="relative text-lg font-semibold text-white">{genre.genre}</p><p className="relative mt-1 text-sm text-white/35">{genre.tracks} tracks</p><div className="mt-5 h-1 rounded-full bg-white/5"><div className="h-1 rounded-full bg-accent" style={{ width: `${Math.max(4, genre.tracks / max * 100)}%` }} /></div><span className="absolute right-4 top-5 text-xs text-white/20">{String(index + 1).padStart(2, '0')}</span></div>)}</div>{!data.length && <div className="rounded-2xl border border-dashed border-white/10 p-10 text-center text-sm text-white/30">Scan tagged audio to populate genres.</div>}</div>; }
function PlaylistsView({ playlists }: { playlists: PlaylistSummary[] }) { return <div><SectionHeading title="Playlists" subtitle="Manual and smart collections for every mood" action="Create playlist" /><div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">{playlists.map((playlist, index) => <div key={playlist.id} className="group rounded-2xl border border-white/5 bg-white/[.025] p-3 hover:border-accent/30"><div className="grid aspect-square place-items-center rounded-xl bg-gradient-to-br from-accent/70 via-fuchsia-500/40 to-[#161823] text-4xl font-bold text-white/80 shadow-inner"><span>{index + 1}</span></div><p className="mt-3 truncate font-medium text-white">{playlist.name}</p><p className="mt-1 text-xs text-white/35">{playlist.isSmart ? 'Smart playlist' : 'Manual playlist'}</p></div>)}{!playlists.length && <div className="rounded-2xl border border-dashed border-white/10 p-10 text-sm text-white/30">No playlists yet.</div>}</div></div>; }
function HistoryView({ history, tracks }: { history: Array<{ trackId: string; playedSeconds: number; completionRatio: number; startedAtUtc: string }>; tracks: TrackSummary[] }) { return <div><SectionHeading title="Recently played" subtitle="Your latest listening activity" /><div className="space-y-2">{history.map((entry) => { const track = tracks.find((t) => t.id === entry.trackId); if (!track) return null; return <div key={`${entry.trackId}-${entry.startedAtUtc}`} className="flex items-center gap-4 rounded-xl border border-white/5 bg-white/[.02] px-4 py-3"><div className="grid size-9 place-items-center rounded-lg bg-accent/10 text-accent">♫</div><div className="min-w-0 flex-1"><p className="truncate text-sm font-medium text-white">{track.title}</p><p className="truncate text-xs text-white/35">{track.artistName ?? 'Unknown artist'} · {Math.round(entry.completionRatio * 100)}% played</p></div><span className="hidden text-xs text-white/25 sm:block">{new Date(entry.startedAtUtc).toLocaleDateString()}</span></div>; })}</div></div>; }
function InsightsView({ overview, genres, topTracks }: { overview: MusicOverview | null; genres: Array<{ genre: string; tracks: number }>; topTracks: TrackSummary[] }) { const max = Math.max(1, ...genres.map((x) => x.tracks)); return <div className="space-y-8"><div><p className="text-[11px] font-semibold uppercase tracking-[.2em] text-accent">Analytics</p><h1 className="mt-2 text-3xl font-bold text-white">Your listening, at a glance.</h1><p className="mt-2 text-sm text-white/40">Simple signals from the music you actually play.</p></div><div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4"><Metric label="Listening time" value={overview ? `${Math.floor(overview.listeningSeconds / 3600)}h` : '0h'} detail="recorded history" /><Metric label="Favorites" value={overview?.favorites ?? 0} detail="saved items" /><Metric label="Top tracks" value={topTracks.length} detail="most played" /><Metric label="Genres" value={genres.length} detail="represented" /></div><div className="grid gap-6 lg:grid-cols-2"><section className="rounded-2xl border border-white/5 bg-white/[.025] p-5"><SectionHeading title="Genre profile" /><div className="space-y-4">{genres.slice(0, 8).map((genre) => <div key={genre.genre}><div className="mb-1 flex justify-between text-xs"><span className="text-white/65">{genre.genre}</span><span className="text-white/25">{genre.tracks}</span></div><div className="h-2 rounded-full bg-white/5"><div className="h-2 rounded-full bg-accent" style={{ width: `${genre.tracks / max * 100}%` }} /></div></div>)}</div></section><section className="rounded-2xl border border-white/5 bg-white/[.025] p-5"><SectionHeading title="Top tracks" /><TrackList tracks={topTracks} compact /></section></div></div>; }
