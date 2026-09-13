import { useEffect, useMemo, useState } from 'react';
import { fetchLibraries, type Library } from '../api/libraries';
import { addMusicFavorite, fetchMusicAlbums, fetchMusicArtists, fetchMusicFavorites, fetchMusicGenres, fetchMusicTracks, formatDuration, removeMusicFavorite, reindexMusic, type AlbumSummary, type ArtistSummary, type TrackSummary } from '../api/music';
import { MusicPlayerBar } from '../components/MusicPlayerBar';
import { MusicTrackMenu } from '../components/MusicTrackMenu';
import { MusicIcon } from '../components/icons';
import { ErrorState } from '../components/states/ErrorState';
import { LoadingState } from '../components/states/LoadingState';
import { EmptyState } from '../components/states/EmptyState';
import { usePlayer } from '../hooks/usePlayer';

type SortKey = 'title' | 'artist' | 'album' | 'year' | 'duration' | 'rating';

export function MusicLibraryIntelligencePage() {
  const player = usePlayer();
  const [libraries, setLibraries] = useState<Library[] | null>(null);
  const [libraryId, setLibraryId] = useState('');
  const [tracks, setTracks] = useState<TrackSummary[]>([]);
  const [albums, setAlbums] = useState<AlbumSummary[]>([]);
  const [artists, setArtists] = useState<ArtistSummary[]>([]);
  const [genres, setGenres] = useState<Array<{ genre: string; tracks: number }>>([]);
  const [favorites, setFavorites] = useState<Set<string>>(new Set());
  const [query, setQuery] = useState('');
  const [genre, setGenre] = useState('all');
  const [album, setAlbum] = useState('all');
  const [artist, setArtist] = useState('all');
  const [sort, setSort] = useState<SortKey>('title');
  const [descending, setDescending] = useState(false);
  const [selected, setSelected] = useState<Set<string>>(new Set());
  const [showDuplicates, setShowDuplicates] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const load = async (id: string) => {
    try {
      const [t, al, ar, g, f] = await Promise.all([
        fetchMusicTracks(id, { limit: 5000 }),
        fetchMusicAlbums(id, { limit: 1000 }),
        fetchMusicArtists(id, { limit: 1000 }),
        fetchMusicGenres(),
        fetchMusicFavorites(),
      ]);
      setTracks(t); setAlbums(al); setArtists(ar); setGenres(g); setFavorites(new Set(f.filter(x => x.entityType === 'track').map(x => x.entityId))); setSelected(new Set()); setError(null);
    } catch (e) { setError(e instanceof Error ? e.message : 'Could not load the music library.'); }
  };

  useEffect(() => {
    const controller = new AbortController();
    fetchLibraries(controller.signal).then(all => { setLibraries(all); if (all[0]) setLibraryId(v => v || all[0].id); }).catch(e => { if (!controller.signal.aborted) setError(e instanceof Error ? e.message : 'Could not load libraries.'); });
    return () => controller.abort();
  }, []);
  useEffect(() => { if (libraryId) void load(libraryId); }, [libraryId]);

  const duplicateIds = useMemo(() => {
    const groups = new Map<string, string[]>();
    tracks.forEach(t => {
      const key = `${t.title.trim().toLowerCase()}|${(t.artistName ?? '').trim().toLowerCase()}|${t.durationSeconds ?? 0}`;
      groups.set(key, [...(groups.get(key) ?? []), t.id]);
    });
    return new Set([...groups.values()].filter(ids => ids.length > 1).flat());
  }, [tracks]);

  const visible = useMemo(() => {
    const q = query.trim().toLowerCase();
    const result = tracks.filter(t => {
      if (genre !== 'all' && (t.genre ?? 'Unknown') !== genre) return false;
      if (album !== 'all' && t.albumId !== album) return false;
      if (artist !== 'all' && t.artistName !== artist) return false;
      if (showDuplicates && !duplicateIds.has(t.id)) return false;
      if (!q) return true;
      return `${t.title} ${t.artistName ?? ''} ${t.albumTitle} ${t.genre ?? ''}`.toLowerCase().includes(q);
    });
    result.sort((a, b) => {
      const av = sort === 'title' ? a.title : sort === 'artist' ? (a.artistName ?? '') : sort === 'album' ? a.albumTitle : sort === 'year' ? (a.year ?? 0) : sort === 'duration' ? (a.durationSeconds ?? 0) : a.rating;
      const bv = sort === 'title' ? b.title : sort === 'artist' ? (b.artistName ?? '') : sort === 'album' ? b.albumTitle : sort === 'year' ? (b.year ?? 0) : sort === 'duration' ? (b.durationSeconds ?? 0) : b.rating;
      return String(av).localeCompare(String(bv), undefined, { numeric: true, sensitivity: 'base' }) * (descending ? -1 : 1);
    });
    return result;
  }, [tracks, query, genre, album, artist, sort, descending, showDuplicates, duplicateIds]);

  const allVisibleSelected = visible.length > 0 && visible.every(t => selected.has(t.id));
  const toggleSelected = (id: string) => setSelected(current => { const next = new Set(current); next.has(id) ? next.delete(id) : next.add(id); return next; });
  const selectVisible = () => setSelected(current => { const next = new Set(current); if (allVisibleSelected) visible.forEach(t => next.delete(t.id)); else visible.forEach(t => next.add(t.id)); return next; });

  const bulkFavorite = async (favorite: boolean) => {
    if (!selected.size) return;
    setBusy(true);
    try {
      await Promise.all([...selected].map(id => favorite ? addMusicFavorite('track', id) : removeMusicFavorite('track', id)));
      await load(libraryId);
    } catch (e) { setError(e instanceof Error ? e.message : 'Could not update favorites.'); }
    finally { setBusy(false); }
  };
  const queueSelected = () => { const chosen = tracks.filter(t => selected.has(t.id)); if (chosen.length) player.enqueue(chosen[0]); chosen.slice(1).forEach(t => player.enqueue(t)); };
  const rescan = async () => { setBusy(true); try { await reindexMusic(libraryId); await load(libraryId); } catch (e) { setError(e instanceof Error ? e.message : 'Could not refresh the library.'); } finally { setBusy(false); } };

  if (error) return <ErrorState title="Music library unavailable" message={error} onRetry={() => libraryId && void load(libraryId)} />;
  if (!libraries) return <LoadingState label="Loading library…" />;
  if (!libraryId) return <EmptyState icon={<MusicIcon className="size-6" />} title="No music library" description="Create a music library and scan your audio folder to get started." />;

  return <div className="pb-24">
    <header className="sticky top-0 z-20 -mx-4 mb-6 border-b border-border bg-background/90 px-4 py-4 backdrop-blur-xl sm:-mx-6 sm:px-6 xl:-mx-8 xl:px-8">
      <div className="flex flex-col gap-4 xl:flex-row xl:items-center"><div className="min-w-0 flex-1"><p className="text-xs font-semibold uppercase tracking-[.2em] text-accent">Music library</p><h1 className="mt-1 text-3xl font-bold tracking-tight text-primary">Organize everything</h1><p className="mt-1 text-sm text-secondary">Search, filter, sort and manage your collection from one workspace.</p></div><div className="flex flex-wrap gap-2"><select aria-label="Library" value={libraryId} onChange={e => setLibraryId(e.target.value)} className="rounded-xl border border-border bg-surface px-3 py-2 text-sm text-primary">{libraries.map(l => <option key={l.id} value={l.id}>{l.name}</option>)}</select><button type="button" disabled={busy} onClick={() => void rescan()} className="rounded-xl border border-border bg-surface px-3 py-2 text-sm text-secondary hover:bg-surface-elevated">{busy ? 'Refreshing…' : 'Refresh'}</button></div></div>
      <div className="mt-4 grid gap-2 lg:grid-cols-[minmax(260px,1.8fr)_repeat(3,minmax(130px,1fr))_auto]"><input value={query} onChange={e => setQuery(e.target.value)} placeholder="Search title, artist, album, genre…" className="rounded-xl border border-border bg-surface px-3 py-2.5 text-sm text-primary outline-none focus:border-accent/50" /><select value={genre} onChange={e => setGenre(e.target.value)} aria-label="Genre" className="rounded-xl border border-border bg-surface px-3 py-2.5 text-sm text-primary"><option value="all">All genres</option>{genres.map(g => <option key={g.genre} value={g.genre}>{g.genre} · {g.tracks}</option>)}</select><select value={artist} onChange={e => setArtist(e.target.value)} aria-label="Artist" className="rounded-xl border border-border bg-surface px-3 py-2.5 text-sm text-primary"><option value="all">All artists</option>{artists.map(a => <option key={a.id} value={a.name}>{a.name}</option>)}</select><select value={album} onChange={e => setAlbum(e.target.value)} aria-label="Album" className="rounded-xl border border-border bg-surface px-3 py-2.5 text-sm text-primary"><option value="all">All albums</option>{albums.map(a => <option key={a.id} value={a.id}>{a.title}</option>)}</select><button type="button" onClick={() => setShowDuplicates(v => !v)} className={`rounded-xl border px-3 py-2.5 text-sm ${showDuplicates ? 'border-accent/40 bg-accent/10 text-accent' : 'border-border bg-surface text-secondary'}`}>Duplicates {duplicateIds.size ? `· ${duplicateIds.size}` : ''}</button></div>
    </header>

    <div className="mb-4 flex flex-wrap items-center justify-between gap-3"><div className="flex items-center gap-2 text-sm text-secondary"><input type="checkbox" checked={allVisibleSelected} onChange={selectVisible} aria-label="Select visible tracks" /><span>{visible.length} tracks</span>{selected.size > 0 && <span className="text-primary">· {selected.size} selected</span>}</div><div className="flex items-center gap-2"><select value={sort} onChange={e => setSort(e.target.value as SortKey)} aria-label="Sort tracks" className="rounded-lg border border-border bg-surface px-2.5 py-2 text-xs text-primary"><option value="title">Title</option><option value="artist">Artist</option><option value="album">Album</option><option value="year">Year</option><option value="duration">Duration</option><option value="rating">Rating</option></select><button type="button" onClick={() => setDescending(v => !v)} className="rounded-lg border border-border bg-surface px-3 py-2 text-xs text-secondary">{descending ? 'Descending' : 'Ascending'}</button></div></div>

    {selected.size > 0 && <div className="sticky bottom-20 z-30 mb-4 flex flex-wrap items-center gap-2 rounded-2xl border border-border bg-surface/95 p-3 shadow-pop backdrop-blur"><span className="mr-2 text-sm font-medium text-primary">{selected.size} selected</span><button type="button" disabled={busy} onClick={() => void bulkFavorite(true)} className="rounded-lg bg-primary px-3 py-2 text-xs font-semibold text-background">Favorite</button><button type="button" disabled={busy} onClick={() => void bulkFavorite(false)} className="rounded-lg border border-border px-3 py-2 text-xs text-secondary">Remove favorite</button><button type="button" onClick={queueSelected} className="rounded-lg border border-border px-3 py-2 text-xs text-secondary">Add to queue</button><button type="button" onClick={() => setSelected(new Set())} className="ml-auto rounded-lg px-3 py-2 text-xs text-tertiary hover:text-primary">Clear selection</button></div>}

    <div className="overflow-hidden rounded-2xl border border-border bg-surface/60"><div className="hidden grid-cols-[40px_minmax(0,1.8fr)_minmax(120px,1fr)_minmax(120px,1fr)_90px_90px_64px] gap-3 border-b border-border px-4 py-3 text-[10px] font-semibold uppercase tracking-[.14em] text-tertiary md:grid"><span /><span>Track</span><span>Artist</span><span>Album</span><span>Year</span><span>Length</span><span /></div>{visible.length === 0 ? <div className="p-12 text-center"><p className="font-medium text-primary">Nothing matches these filters</p><p className="mt-1 text-sm text-secondary">Try a broader search or clear one of the filters.</p></div> : <div>{visible.map(track => <TrackRow key={track.id} track={track} selected={selected.has(track.id)} favorite={favorites.has(track.id)} onSelect={() => toggleSelected(track.id)} onPlay={() => player.toggle(track)} onChanged={() => void load(libraryId)} />)}</div>}</div>
    <MusicPlayerBar />
  </div>;
}

function TrackRow({ track, selected, favorite, onSelect, onPlay, onChanged }: { track: TrackSummary; selected: boolean; favorite: boolean; onSelect: () => void; onPlay: () => void; onChanged: () => void }) {
  return <div className={`grid items-center gap-3 border-b border-border px-4 py-3 last:border-b-0 md:grid-cols-[40px_minmax(0,1.8fr)_minmax(120px,1fr)_minmax(120px,1fr)_90px_90px_64px] ${selected ? 'bg-accent/5' : 'hover:bg-surface-elevated/50'}`}><input type="checkbox" checked={selected} onChange={onSelect} aria-label={`Select ${track.title}`} /><button type="button" onClick={onPlay} className="min-w-0 text-left"><span className="flex min-w-0 items-center gap-3"><span className="hidden size-10 shrink-0 overflow-hidden rounded-lg bg-surface-elevated sm:block">{track.albumId && track.hasCover ? <img src={`/api/music/albums/${track.albumId}/cover`} alt="" className="h-full w-full object-cover" /> : <span className="grid h-full place-items-center text-primary/20">♫</span>}</span><span className="min-w-0"><span className="block truncate text-sm font-medium text-primary">{track.title}</span><span className="block truncate text-xs text-tertiary md:hidden">{track.artistName ?? 'Unknown artist'} · {track.albumTitle}</span></span></span></button><span className="hidden truncate text-sm text-secondary md:block">{track.artistName ?? 'Unknown artist'}</span><span className="hidden truncate text-sm text-secondary md:block">{track.albumTitle}</span><span className="hidden text-sm text-tertiary md:block">{track.year ?? '—'}</span><span className="hidden text-xs tabular-nums text-tertiary md:block">{formatDuration(track.durationSeconds)}</span><span className="flex items-center justify-end gap-1"><button type="button" aria-label={favorite ? `Remove ${track.title} from favorites` : `Favorite ${track.title}`} onClick={async () => { try { favorite ? await removeMusicFavorite('track', track.id) : await addMusicFavorite('track', track.id); onChanged(); } catch {} }} className={`grid size-8 place-items-center rounded-lg ${favorite ? 'text-accent' : 'text-tertiary hover:text-primary'}`}>{favorite ? '♥' : '♡'}</button><MusicTrackMenu track={track} onChanged={onChanged} /></span></div>;
}
