import { useEffect, useMemo, useState } from 'react';
import { fetchLibraries, type Library } from '../api/libraries';
import { fetchMusicAlbums, fetchMusicArtists, fetchMusicFavorites, fetchMusicGenres, fetchMusicHistory, fetchMusicOverview, fetchMusicPlaylists, fetchMusicTopTracks, fetchMusicTracks, formatDuration, reindexMusic, type AlbumSummary, type ArtistSummary, type MusicOverview, type PlaylistSummary, type TrackSummary } from '../api/music';
import { usePlayer } from '../hooks/usePlayer';
import { MusicPlayerBar, QueueButton } from '../components/MusicPlayerBar';
import { MusicTrackActions } from '../components/MusicTrackActions';
import { MusicMetadataEditor } from '../components/MusicMetadataEditor';
import { ErrorState } from '../components/states/ErrorState';
import { LoadingState } from '../components/states/LoadingState';
import { EmptyState } from '../components/states/EmptyState';
import { MusicIcon, PlayIcon } from '../components/icons';

type Tab = 'home' | 'library' | 'categories' | 'playlists' | 'insights' | 'favorites' | 'history';

export function MusicDashboardPage() {
  const [libraries, setLibraries] = useState<Library[] | null>(null);
  const [libraryId, setLibraryId] = useState('');
  const [tab, setTab] = useState<Tab>('home');
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
  const [selectedArtist, setSelectedArtist] = useState('');
  const [selectedGenre, setSelectedGenre] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const load = async (id: string) => {
    try {
      const [a, al, t, o, p, g, f, h, tt] = await Promise.all([
        fetchMusicArtists(id, { limit: 500 }), fetchMusicAlbums(id, { limit: 500 }), fetchMusicTracks(id, { limit: 1000 }),
        fetchMusicOverview(), fetchMusicPlaylists(), fetchMusicGenres(), fetchMusicFavorites(), fetchMusicHistory({ limit: 50 }), fetchMusicTopTracks(10),
      ]);
      setArtists(a); setAlbums(al); setTracks(t); setOverview(o); setPlaylists(p); setGenres(g); setFavorites(f); setHistory(h); setTop(tt); setError(null);
    } catch (e) { setError(e instanceof Error ? e.message : 'Could not load music dashboard.'); }
  };

  useEffect(() => {
    const controller = new AbortController();
    fetchLibraries(controller.signal).then((all) => { setLibraries(all); if (all[0]) setLibraryId((current) => current || all[0].id); }).catch((e) => { if (!controller.signal.aborted) setError(e instanceof Error ? e.message : 'Could not load libraries.'); });
    return () => controller.abort();
  }, []);
  useEffect(() => { if (libraryId) void load(libraryId); }, [libraryId]);

  const favoriteIds = useMemo(() => new Set(favorites.filter((f) => f.entityType === 'track').map((f) => f.entityId)), [favorites]);
  const filtered = useMemo(() => tracks.filter((t) => (!selectedArtist || t.artistName === selectedArtist) && (!selectedGenre || t.genre === selectedGenre) && (!search || `${t.title} ${t.artistName ?? ''} ${t.albumTitle} ${t.genre ?? ''}`.toLowerCase().includes(search.toLowerCase()))), [tracks, selectedArtist, selectedGenre, search]);
  const topTracks = useMemo(() => top.map((x) => tracks.find((t) => t.id === x.trackId)).filter(Boolean) as TrackSummary[], [top, tracks]);

  const rescan = async () => {
    setBusy(true);
    try { await reindexMusic(libraryId); await load(libraryId); } catch (e) { setError(e instanceof Error ? e.message : 'Could not rescan music.'); } finally { setBusy(false); }
  };

  if (error) return <ErrorState title="Music dashboard unavailable" message={error} onRetry={() => libraryId && void load(libraryId)} />;
  if (!libraries) return <LoadingState label="Loading Music…" />;
  if (!libraryId) return <EmptyState icon={<MusicIcon className="size-6" />} title="No music library" description="Create a music library and scan your audio folder to get started." />;

  return <div className="space-y-6 pb-24">
    <header className="rounded-3xl border border-border bg-surface p-6 shadow-card">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div><p className="text-xs font-semibold uppercase tracking-[.2em] text-accent">oniDash Music</p><h1 className="mt-2 text-3xl font-bold tracking-tight">Your music universe.</h1><p className="mt-1 text-sm text-secondary">Library, categories, playlists, playback, history and insights — all in one dashboard.</p></div>
        <div className="flex gap-2"><select aria-label="Music library" value={libraryId} onChange={(e) => setLibraryId(e.target.value)} className="rounded-xl border border-border bg-background px-3 py-2">{libraries.map((l) => <option key={l.id} value={l.id}>{l.name}</option>)}</select><button type="button" onClick={() => void rescan()} disabled={busy} className="rounded-xl bg-accent px-4 py-2 text-sm font-medium text-white">{busy ? 'Scanning…' : 'Scan library'}</button></div>
      </div>
      <nav className="mt-6 flex flex-wrap gap-2">{(['home', 'library', 'categories', 'playlists', 'insights', 'favorites', 'history'] as const).map((item) => <button key={item} type="button" onClick={() => setTab(item)} className={`rounded-xl px-4 py-2 text-sm font-medium capitalize ${tab === item ? 'bg-accent text-white' : 'border border-border bg-background text-secondary'}`}>{item}</button>)}</nav>
    </header>
    {tab === 'home' && <Home overview={overview} artists={artists} albums={albums} tracks={tracks} playlists={playlists} topTracks={topTracks} onTab={setTab} />}
    {tab === 'library' && <LibraryView artists={artists} albums={albums} tracks={filtered} search={search} setSearch={setSearch} selectedArtist={selectedArtist} setSelectedArtist={setSelectedArtist} favoriteIds={favoriteIds} onReload={() => void load(libraryId)} />}
    {tab === 'categories' && <CategoriesView genres={genres} tracks={tracks} onSelect={(genre) => { setSelectedGenre(genre); setTab('library'); }} />}
    {tab === 'playlists' && <PlaylistView playlists={playlists} />}
    {tab === 'insights' && <Insights overview={overview} genres={genres} topTracks={topTracks} />}
    {tab === 'favorites' && <LibraryView artists={artists} albums={albums} tracks={tracks.filter((t) => favoriteIds.has(t.id))} search={search} setSearch={setSearch} selectedArtist={selectedArtist} setSelectedArtist={setSelectedArtist} favoriteIds={favoriteIds} onReload={() => void load(libraryId)} />}
    {tab === 'history' && <HistoryView history={history} tracks={tracks} />}
    <MusicPlayerBar />
  </div>;
}

function Home({ overview, artists, albums, tracks, playlists, topTracks, onTab }: { overview: MusicOverview | null; artists: ArtistSummary[]; albums: AlbumSummary[]; tracks: TrackSummary[]; playlists: PlaylistSummary[]; topTracks: TrackSummary[]; onTab: (tab: Tab) => void }) {
  return <div className="space-y-6"><div className="grid grid-cols-2 gap-3 md:grid-cols-5">{[['Tracks', overview?.tracks ?? tracks.length], ['Albums', overview?.albums ?? albums.length], ['Artists', overview?.artists ?? artists.length], ['Playlists', overview?.playlists ?? playlists.length], ['Listening', overview ? `${Math.round(overview.listeningSeconds / 3600)}h` : '0h']].map(([k, v]) => <div key={String(k)} className="rounded-2xl border border-border bg-surface p-4"><p className="text-xs text-muted">{k}</p><p className="mt-1 text-2xl font-bold">{v}</p></div>)}</div><section className="rounded-2xl border border-border bg-surface p-5"><div><h2 className="text-lg font-semibold">Quick access</h2><p className="text-sm text-secondary">Jump directly into your collection.</p></div><div className="mt-5 grid gap-3 sm:grid-cols-2 lg:grid-cols-4">{[['All music', `${tracks.length} tracks`, 'library'], ['Categories', 'Genres and curated views', 'categories'], ['Playlists', `${playlists.length} playlists`, 'playlists'], ['Artists', `${artists.length} artists`, 'library']].map(([title, value, target]) => <button key={title} type="button" onClick={() => onTab(target as Tab)} className="rounded-2xl border border-border bg-surface p-4 text-left hover:border-accent"><p className="font-semibold">{title}</p><p className="mt-1 text-xs text-muted">{value}</p></button>)}</div></section><section><h2 className="mb-3 text-lg font-semibold">Top tracks</h2><TrackList tracks={topTracks} /></section></div>;
}

function LibraryView({ artists, albums, tracks, search, setSearch, selectedArtist, setSelectedArtist, favoriteIds, onReload }: { artists: ArtistSummary[]; albums: AlbumSummary[]; tracks: TrackSummary[]; search: string; setSearch: (v: string) => void; selectedArtist: string; setSelectedArtist: (v: string) => void; favoriteIds: Set<string>; onReload: () => void }) {
  return <div className="space-y-6"><div className="flex flex-wrap gap-2"><input value={search} onChange={(e) => setSearch(e.target.value)} placeholder="Search tracks, albums, artists, genres…" className="min-w-64 flex-1 rounded-xl border border-border bg-surface px-4 py-2" /><select value={selectedArtist} onChange={(e) => setSelectedArtist(e.target.value)} className="rounded-xl border border-border bg-surface px-3 py-2"><option value="">All artists</option>{artists.map((a) => <option key={a.id} value={a.name}>{a.name}</option>)}</select></div><section><h2 className="mb-3 text-lg font-semibold">Albums</h2><div className="grid grid-cols-2 gap-4 sm:grid-cols-4 lg:grid-cols-6">{albums.slice(0, 12).map((a) => <div key={a.id} className="overflow-hidden rounded-2xl border border-border bg-surface">{a.hasCover ? <img src={`/api/music/albums/${a.id}/cover`} alt={a.title} className="aspect-square w-full object-cover" /> : <div className="grid aspect-square place-items-center"><MusicIcon className="size-10 text-accent" /></div>}<div className="p-3"><p className="truncate text-sm font-medium">{a.title}</p><p className="truncate text-xs text-muted">{a.artistName ?? 'Unknown'}</p></div></div>)}</div></section><section><h2 className="mb-3 text-lg font-semibold">Tracks <span className="text-sm font-normal text-muted">{tracks.length}</span></h2><TrackList tracks={tracks} favoriteIds={favoriteIds} onReload={onReload} /></section></div>;
}

function CategoriesView({ genres, tracks, onSelect }: { genres: Array<{ genre: string; tracks: number }>; tracks: TrackSummary[]; onSelect: (genre: string) => void }) {
  const fallback = useMemo(() => tracks.reduce<Record<string, number>>((acc, track) => { if (track.genre) acc[track.genre] = (acc[track.genre] ?? 0) + 1; return acc; }, {}), [tracks]);
  const categories = genres.length ? genres : Object.entries(fallback).map(([genre, count]) => ({ genre, tracks: count })).sort((a, b) => b.tracks - a.tracks);
  return <section><h2 className="text-xl font-semibold">Music categories</h2><p className="mt-1 text-sm text-secondary">Use genres as library categories and jump straight into a filtered collection.</p><div className="mt-5 grid gap-3 sm:grid-cols-2 lg:grid-cols-4">{categories.map((category) => <button key={category.genre} type="button" onClick={() => onSelect(category.genre)} className="rounded-2xl border border-border bg-surface p-5 text-left hover:border-accent"><p className="font-semibold">{category.genre}</p><p className="mt-1 text-sm text-muted">{category.tracks} tracks</p><div className="mt-4 h-1.5 rounded-full bg-background"><div className="h-1.5 rounded-full bg-accent" style={{ width: `${Math.min(100, category.tracks / Math.max(1, categories[0]?.tracks ?? 1) * 100)}%` }} /></div></button>)}{!categories.length && <div className="rounded-2xl border border-dashed border-border p-8 text-sm text-muted">No genre categories are available yet. Scan tagged audio to populate them.</div>}</div></section>;
}

function TrackList({ tracks, favoriteIds, onReload }: { tracks: TrackSummary[]; favoriteIds?: Set<string>; onReload?: () => void }) { const { current, toggle, playQueue } = usePlayer(); return <ul className="divide-y divide-border overflow-hidden rounded-2xl border border-border bg-surface">{tracks.length === 0 ? <li className="p-6 text-sm text-muted">No tracks found.</li> : tracks.map((track) => { const active = current?.track.id === track.id; return <li key={track.id} className="flex items-center gap-3 px-4 py-3"><button type="button" aria-label={active ? `Pause ${track.title}` : `Play ${track.title}`} onClick={() => active ? toggle(track) : playQueue(tracks, tracks.indexOf(track))} className={`icon-btn ${active ? 'text-accent' : ''}`}><PlayIcon className="size-4" /></button><span className="min-w-0 flex-1"><span className="block truncate text-sm font-medium">{track.title}</span><span className="block truncate text-xs text-muted">{track.artistName ?? 'Unknown'} · {track.albumTitle}{track.genre ? ` · ${track.genre}` : ''}{favoriteIds?.has(track.id) ? ' · ♥' : ''}</span></span><span className="hidden text-xs text-muted sm:block">{formatDuration(track.durationSeconds)}</span><QueueButton track={track} /><MusicMetadataEditor track={track} onSaved={onReload ?? (() => undefined)} /><MusicTrackActions track={track} onChanged={onReload ?? (() => undefined)} /></li>; })}</ul>; }

function PlaylistView({ playlists }: { playlists: PlaylistSummary[] }) { return <section><h2 className="text-xl font-semibold">Playlists</h2><p className="mt-1 text-sm text-secondary">Manual and smart playlists stay inside the Music dashboard.</p><div className="mt-4 grid gap-4 md:grid-cols-3">{playlists.map((p) => <div key={p.id} className="rounded-2xl border border-border bg-surface p-5"><p className="font-semibold">{p.name}</p><p className="mt-1 text-xs text-muted">{p.isSmart ? 'Smart' : 'Manual'} playlist</p></div>)}{!playlists.length && <div className="rounded-2xl border border-dashed border-border p-8 text-sm text-muted">No playlists yet. Use the existing Music playlist controls to create one.</div>}</div></section>; }

function Insights({ overview, genres, topTracks }: { overview: MusicOverview | null; genres: Array<{ genre: string; tracks: number }>; topTracks: TrackSummary[] }) { const max = Math.max(1, ...genres.map((x) => x.tracks)); return <div className="space-y-6"><div><h2 className="text-xl font-semibold">Music insights</h2><p className="text-sm text-secondary">Understand your collection and listening habits.</p></div><div className="grid gap-4 md:grid-cols-3">{[['Listening time', `${Math.round((overview?.listeningSeconds ?? 0) / 3600)}h`], ['Tracks', String(overview?.tracks ?? 0)], ['Top tracks', String(topTracks.length)]].map(([k, v]) => <div key={k} className="rounded-2xl border border-border bg-surface p-5"><p className="text-xs text-muted">{k}</p><p className="mt-2 text-3xl font-bold">{v}</p></div>)}</div><section className="rounded-2xl border border-border bg-surface p-5"><h3 className="font-semibold">Genres</h3><div className="mt-4 space-y-3">{genres.slice(0, 12).map((g) => <div key={g.genre}><div className="flex justify-between text-sm"><span>{g.genre}</span><span className="text-muted">{g.tracks}</span></div><div className="mt-1 h-2 rounded-full bg-background"><div className="h-2 rounded-full bg-accent" style={{ width: `${g.tracks / max * 100}%` }} /></div></div>)}</div></section></div>; }

function HistoryView({ history, tracks }: { history: Array<{ id: string; trackId: string; playedSeconds: number; completionRatio: number; startedAtUtc: string }>; tracks: TrackSummary[] }) { return <section><h2 className="text-xl font-semibold">Listening history</h2><ul className="mt-4 divide-y divide-border rounded-2xl border border-border bg-surface">{history.map((h) => { const track = tracks.find((x) => x.id === h.trackId); return <li key={h.id} className="flex justify-between px-4 py-3"><span><p className="text-sm font-medium">{track?.title ?? h.trackId}</p><p className="text-xs text-muted">{new Date(h.startedAtUtc).toLocaleString()}</p></span><span className="text-xs text-muted">{Math.round(h.completionRatio * 100)}% · {formatDuration(h.playedSeconds)}</span></li>; })}{!history.length && <li className="p-6 text-sm text-muted">No listening history yet.</li>}</ul></section>; }
