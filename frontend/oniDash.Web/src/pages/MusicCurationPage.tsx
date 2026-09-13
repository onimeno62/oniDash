import { useEffect, useMemo, useState } from 'react';
import { fetchLibraries, type Library } from '../api/libraries';
import { addMusicFavorite, deleteMusicTrack, fetchMusicInfo, fetchMusicTracks, fetchMusicMetadata, formatDuration, updateMusicMetadata, type MusicMetadata, type TrackSummary } from '../api/music';
import { ErrorState } from '../components/states/ErrorState';
import { LoadingState } from '../components/states/LoadingState';
import { EmptyState } from '../components/states/EmptyState';
import { MusicPlayerBar } from '../components/MusicPlayerBar';
import { MusicTrackMenu } from '../components/MusicTrackMenu';
import { usePlayer } from '../hooks/usePlayer';

export function MusicCurationPage() {
  const player = usePlayer();
  const [libraries, setLibraries] = useState<Library[]>([]);
  const [libraryId, setLibraryId] = useState('');
  const [tracks, setTracks] = useState<TrackSummary[]>([]);
  const [selected, setSelected] = useState<Set<string>>(new Set());
  const [keep, setKeep] = useState<Set<string>>(new Set());
  const [query, setQuery] = useState('');
  const [genre, setGenre] = useState('');
  const [artist, setArtist] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [metadata, setMetadata] = useState<MusicMetadata | null>(null);
  const [inspected, setInspected] = useState<TrackSummary | null>(null);
  const [edit, setEdit] = useState({ artist: '', albumArtist: '', album: '', genre: '', year: '', track: '', disc: '' });

  const load = async (id: string) => {
    try { setTracks(await fetchMusicTracks(id, { limit: 5000 })); setSelected(new Set()); setKeep(new Set()); setError(null); }
    catch (e) { setError(e instanceof Error ? e.message : 'Could not load the library.'); }
  };
  useEffect(() => { fetchLibraries().then(xs => { setLibraries(xs); setLibraryId(xs[0]?.id ?? ''); }).catch(e => setError(e instanceof Error ? e.message : 'Could not load libraries.')); }, []);
  useEffect(() => { if (libraryId) void load(libraryId); }, [libraryId]);

  const groups = useMemo(() => {
    const map = new Map<string, TrackSummary[]>();
    for (const t of tracks) {
      const key = `${t.title.trim().toLowerCase()}|${(t.artistName ?? '').trim().toLowerCase()}|${Math.round(t.durationSeconds ?? 0)}`;
      map.set(key, [...(map.get(key) ?? []), t]);
    }
    return [...map.values()].filter(g => g.length > 1);
  }, [tracks]);

  const filtered = useMemo(() => tracks.filter(t => (!query || `${t.title} ${t.artistName ?? ''} ${t.albumTitle}`.toLowerCase().includes(query.toLowerCase())) && (!genre || (t.genre ?? '') === genre) && (!artist || (t.artistName ?? '') === artist)), [tracks, query, genre, artist]);
  const genres = [...new Set(tracks.map(t => t.genre).filter(Boolean))] as string[];
  const artists = [...new Set(tracks.map(t => t.artistName).filter(Boolean))] as string[];

  const inspect = async (track: TrackSummary) => {
    setInspected(track); setMetadata(null);
    try { setMetadata(await fetchMusicMetadata(track.id)); setEdit({ artist: '', albumArtist: '', album: '', genre: '', year: '', track: '', disc: '' }); }
    catch (e) { setError(e instanceof Error ? e.message : 'Could not read metadata.'); }
  };
  const applyMetadata = async () => {
    if (!selected.size) return;
    setBusy(true);
    try {
      for (const id of selected) {
        const current = await fetchMusicMetadata(id);
        await updateMusicMetadata(id, { ...current, trackArtist: edit.artist || current.trackArtist, albumArtist: edit.albumArtist || current.albumArtist, album: edit.album || current.album, genre: edit.genre || current.genre, year: edit.year ? Number(edit.year) : current.year, trackNumber: edit.track ? Number(edit.track) : current.trackNumber, discNumber: edit.disc ? Number(edit.disc) : current.discNumber, confirmed: true });
      }
      await load(libraryId); setInspected(null);
    } catch (e) { setError(e instanceof Error ? e.message : 'Could not update metadata.'); }
    finally { setBusy(false); }
  };
  const resolve = async () => {
    const victims = groups.flatMap(g => g.filter(t => !keep.has(t.id)).map(t => t.id));
    if (!victims.length || !confirm(`Remove ${victims.length} duplicate track(s) from the library?`)) return;
    setBusy(true);
    try { await Promise.all(victims.map(id => deleteMusicTrack(id, false))); await load(libraryId); }
    catch (e) { setError(e instanceof Error ? e.message : 'Could not resolve duplicates.'); }
    finally { setBusy(false); }
  };

  if (error && !tracks.length) return <ErrorState title="Music curation unavailable" message={error} onRetry={() => libraryId && void load(libraryId)} />;
  if (!libraries.length) return <LoadingState label="Loading curation workspace…" />;

  return <div className="space-y-6 pb-24">
    <header className="rounded-3xl border border-border bg-surface-elevated p-6 shadow-card sm:p-8">
      <div className="flex flex-col gap-5 lg:flex-row lg:items-end lg:justify-between"><div><p className="text-xs font-semibold uppercase tracking-[.22em] text-accent">Phase 5 · Music intelligence</p><h1 className="mt-2 text-3xl font-bold tracking-tight">Curate your library</h1><p className="mt-2 max-w-2xl text-sm leading-6 text-secondary">Resolve duplicates, inspect file metadata, and make consistent edits across a selection without leaving the library.</p></div><select value={libraryId} onChange={e => setLibraryId(e.target.value)} className="rounded-xl border border-border bg-surface px-3 py-2.5 text-sm"><option value="">Choose library</option>{libraries.map(l => <option key={l.id} value={l.id}>{l.name}</option>)}</select></div>
    </header>
    {error && <div className="rounded-xl border border-danger/30 bg-danger/5 px-4 py-3 text-sm text-danger">{error}</div>}
    <section className="grid gap-3 sm:grid-cols-3"><Stat label="Tracks" value={tracks.length} /><Stat label="Duplicate groups" value={groups.length} /><Stat label="Potential duplicates" value={groups.reduce((n,g) => n + g.length, 0)} /></section>
    <section className="rounded-2xl border border-border bg-surface p-5">
      <div className="flex flex-col gap-3 lg:flex-row"><input value={query} onChange={e => setQuery(e.target.value)} placeholder="Search tracks…" className="flex-1 rounded-xl border border-border bg-surface-elevated px-3 py-2.5 text-sm" /><select value={artist} onChange={e => setArtist(e.target.value)} className="rounded-xl border border-border bg-surface-elevated px-3 py-2.5 text-sm"><option value="">All artists</option>{artists.sort().map(a => <option key={a} value={a}>{a}</option>)}</select><select value={genre} onChange={e => setGenre(e.target.value)} className="rounded-xl border border-border bg-surface-elevated px-3 py-2.5 text-sm"><option value="">All genres</option>{genres.sort().map(g => <option key={g} value={g}>{g}</option>)}</select></div>
      <div className="mt-4 overflow-hidden rounded-xl border border-border"><div className="grid grid-cols-[36px_minmax(0,1.5fr)_minmax(120px,1fr)_minmax(120px,1fr)_90px_70px] gap-3 border-b border-border px-3 py-2 text-[10px] font-semibold uppercase tracking-wider text-tertiary"><span/><span>Track</span><span>Artist</span><span>Album</span><span>Length</span><span/></div>{filtered.slice(0, 500).map(t => <div key={t.id} className="grid grid-cols-[36px_minmax(0,1.5fr)_minmax(120px,1fr)_minmax(120px,1fr)_90px_70px] items-center gap-3 border-b border-border px-3 py-3 last:border-0"><input type="checkbox" checked={selected.has(t.id)} onChange={() => setSelected(s => { const n = new Set(s); n.has(t.id) ? n.delete(t.id) : n.add(t.id); return n; })} aria-label={`Select ${t.title}`} /><button onClick={() => player.toggle(t)} className="truncate text-left text-sm font-medium text-primary">{t.title}</button><span className="truncate text-xs text-secondary">{t.artistName ?? 'Unknown'}</span><span className="truncate text-xs text-secondary">{t.albumTitle}</span><span className="text-xs text-tertiary">{formatDuration(t.durationSeconds)}</span><span className="flex justify-end gap-1"><button onClick={() => void inspect(t)} className="rounded-lg px-2 py-1 text-xs text-accent">Inspect</button><MusicTrackMenu track={t} onChanged={() => void load(libraryId)} /></span></div>)}</div>
    </section>
    {selected.size > 0 && <section className="rounded-2xl border border-accent/30 bg-accent/5 p-5"><div className="flex flex-col gap-4 lg:flex-row lg:items-end"><div className="flex-1"><p className="text-sm font-semibold">Bulk metadata · {selected.size} tracks</p><p className="mt-1 text-xs text-secondary">Only filled fields are changed. Existing values are preserved for blank fields.</p></div>{[['artist','Track artist'],['albumArtist','Album artist'],['album','Album'],['genre','Genre'],['year','Year'],['track','Track #'],['disc','Disc #']].map(([key,label]) => <input key={key} value={edit[key as keyof typeof edit]} onChange={e => setEdit(x => ({ ...x, [key]: e.target.value }))} placeholder={label} className="w-full rounded-lg border border-border bg-surface px-2.5 py-2 text-xs lg:w-28" />)}<button disabled={busy} onClick={() => void applyMetadata()} className="rounded-lg bg-primary px-4 py-2 text-xs font-semibold text-background">{busy ? 'Applying…' : 'Apply'}</button></div></section>}
    <section className="rounded-2xl border border-border bg-surface p-5"><div className="flex flex-wrap items-center justify-between gap-3"><div><h2 className="font-semibold">Duplicate resolution</h2><p className="mt-1 text-xs text-secondary">Choose the keeper in each group. Resolution removes duplicate entries from the library, not the underlying files.</p></div><button disabled={busy || !keep.size} onClick={() => void resolve()} className="rounded-lg border border-border px-3 py-2 text-xs font-semibold">Resolve duplicates</button></div>{groups.length === 0 ? <EmptyState title="No duplicates detected" description="The current library has no exact title, artist and duration matches." /> : <div className="mt-4 space-y-3">{groups.map(group => <div key={group[0].id} className="rounded-xl border border-border bg-surface-elevated p-3"><div className="mb-2 text-xs font-semibold text-primary">{group[0].title} · {group[0].artistName ?? 'Unknown'} · {formatDuration(group[0].durationSeconds)}</div><div className="grid gap-2 sm:grid-cols-2 lg:grid-cols-3">{group.map(t => <label key={t.id} className={`flex cursor-pointer items-center gap-2 rounded-lg border p-2 text-xs ${keep.has(t.id) ? 'border-accent bg-accent/10' : 'border-border'}`}><input type="radio" name={`keep-${group[0].id}`} checked={keep.has(t.id)} onChange={() => setKeep(s => new Set([...s].filter(id => !group.some(x => x.id === id)).concat(t.id)))} /><span className="min-w-0 flex-1 truncate">{t.title} · {t.albumTitle}</span></label>)}</div></div>)}</div>}</section>
    {inspected && <MetadataDrawer track={inspected} metadata={metadata} onClose={() => setInspected(null)} />}
    <MusicPlayerBar />
  </div>;
}
function Stat({ label, value }: { label: string; value: number }) { return <div className="rounded-2xl border border-border bg-surface p-4 shadow-card"><p className="text-xs text-muted">{label}</p><p className="mt-1 text-2xl font-bold">{value}</p></div>; }
function MetadataDrawer({ track, metadata, onClose }: { track: TrackSummary; metadata: MusicMetadata | null; onClose: () => void }) { const [info, setInfo] = useState<{ path: string; sizeBytes: number; modifiedUtc: string; extension: string } | null>(null); useEffect(() => { fetchMusicInfo(track.id).then(setInfo).catch(() => setInfo(null)); }, [track.id]); return <div className="fixed inset-0 z-50 bg-black/50" onMouseDown={e => e.target === e.currentTarget && onClose()}><aside className="ml-auto h-full w-full max-w-lg overflow-y-auto border-l border-border bg-surface p-6 shadow-pop"><div className="flex items-center justify-between"><div><p className="text-xs uppercase tracking-wider text-accent">Metadata inspector</p><h2 className="mt-1 text-xl font-bold">{track.title}</h2></div><button onClick={onClose} className="text-2xl text-muted">×</button></div>{metadata ? <div className="mt-6 grid grid-cols-2 gap-3">{Object.entries(metadata).map(([k,v]) => <Info key={k} label={k} value={v == null ? '—' : String(v)} />)}</div> : <LoadingState label="Reading tags…" />}{info && <div className="mt-6 rounded-xl border border-border bg-surface-elevated p-4 text-xs"><p className="font-semibold">File</p><p className="mt-2 break-all text-secondary">{info.path}</p><p className="mt-2 text-secondary">{(info.sizeBytes / 1024 / 1024).toFixed(2)} MB · {info.extension.toUpperCase()} · modified {new Date(info.modifiedUtc).toLocaleString()}</p></div>}</aside></div>; }
function Info({ label, value }: { label: string; value: string }) { return <div className="rounded-xl border border-border bg-surface-elevated p-3"><p className="text-[10px] uppercase tracking-wider text-tertiary">{label}</p><p className="mt-1 break-words text-sm text-primary">{value}</p></div>; }
