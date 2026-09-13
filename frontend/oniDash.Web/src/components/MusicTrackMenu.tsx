import { useEffect, useRef, useState } from 'react';
import { addTrackToMusicPlaylist, deleteMusicTrack, fetchMusicInfo, fetchMusicPlaylists, renameMusicTrack, type PlaylistSummary, type TrackSummary } from '../api/music';
import { MusicMetadataEditor } from './MusicMetadataEditor';
import { usePlayer } from '../hooks/usePlayer';

type Props = {
  track: TrackSummary;
  onChanged?: () => void;
  onViewAlbum?: () => void;
  onViewArtist?: () => void;
};

export function MusicTrackMenu({ track, onChanged, onViewAlbum, onViewArtist }: Props) {
  const player = usePlayer();
  const [open, setOpen] = useState(false);
  const [playlistOpen, setPlaylistOpen] = useState(false);
  const [playlists, setPlaylists] = useState<PlaylistSummary[]>([]);
  const [busy, setBusy] = useState(false);
  const [renameOpen, setRenameOpen] = useState(false);
  const [name, setName] = useState(track.title);
  const [infoOpen, setInfoOpen] = useState(false);
  const [info, setInfo] = useState<Awaited<ReturnType<typeof fetchMusicInfo>> | null>(null);
  const [error, setError] = useState<string | null>(null);
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!open) return;
    const close = (event: MouseEvent) => { if (!ref.current?.contains(event.target as Node)) setOpen(false); };
    document.addEventListener('mousedown', close);
    return () => document.removeEventListener('mousedown', close);
  }, [open]);

  const run = (action: () => void) => { setOpen(false); setError(null); action(); };
  const openPlaylists = async () => {
    setOpen(false); setPlaylistOpen(true); setError(null);
    try { setPlaylists(await fetchMusicPlaylists()); } catch (e) { setError(e instanceof Error ? e.message : 'Could not load playlists.'); }
  };
  const addPlaylist = async (id: string) => {
    setBusy(true); setError(null);
    try { await addTrackToMusicPlaylist(id, track.id); setPlaylistOpen(false); onChanged?.(); }
    catch (e) { setError(e instanceof Error ? e.message : 'Could not add track to playlist.'); }
    finally { setBusy(false); }
  };
  const saveRename = async () => {
    if (!name.trim()) return;
    setBusy(true); setError(null);
    try { await renameMusicTrack(track.id, name.trim()); setRenameOpen(false); onChanged?.(); }
    catch (e) { setError(e instanceof Error ? e.message : 'Could not rename track.'); }
    finally { setBusy(false); }
  };
  const showInfo = async () => {
    setOpen(false); setInfoOpen(true); setInfo(null); setError(null);
    try { setInfo(await fetchMusicInfo(track.id)); } catch (e) { setError(e instanceof Error ? e.message : 'Could not load track metadata.'); }
  };
  const deleteTrack = async (deleteFile: boolean) => {
    setOpen(false);
    const message = deleteFile ? `Delete “${track.title}” and its audio file permanently?` : `Remove “${track.title}” from the library?`;
    if (!window.confirm(message)) return;
    setBusy(true); setError(null);
    try { await deleteMusicTrack(track.id, deleteFile); onChanged?.(); }
    catch (e) { setError(e instanceof Error ? e.message : 'Could not delete track.'); }
    finally { setBusy(false); }
  };

  return <div ref={ref} className="relative shrink-0">
    <button type="button" aria-label={`More actions for ${track.title}`} aria-expanded={open} onClick={() => setOpen(v => !v)} className="grid size-8 place-items-center rounded-lg text-secondary transition hover:bg-surface-elevated hover:text-primary">⋮</button>
    {open && <div className="absolute right-0 top-10 z-40 w-56 overflow-hidden rounded-xl border border-border bg-surface p-1 shadow-pop" role="menu">
      <MenuItem label="Play next" onClick={() => run(() => player.playNext(track))} />
      <MenuItem label="Add to queue" onClick={() => run(() => player.enqueue(track))} />
      <div className="my-1 border-t border-border" />
      <MenuItem label="Add to playlist" onClick={() => void openPlaylists()} />
      <div className="my-1 border-t border-border" />
      <MenuItem label="Edit tags" onClick={() => { setOpen(false); setInfoOpen(false); }} render={<MusicMetadataEditor track={track} onSaved={() => onChanged?.()} />} />
      <MenuItem label="Rename" onClick={() => run(() => { setName(track.title); setRenameOpen(true); })} />
      <div className="my-1 border-t border-border" />
      <MenuItem label="View album" onClick={() => run(() => onViewAlbum?.())} disabled={!onViewAlbum} />
      <MenuItem label="View artist" onClick={() => run(() => onViewArtist?.())} disabled={!onViewArtist} />
      <MenuItem label="File location" onClick={() => void showInfo()} />
      <MenuItem label="Track metadata" onClick={() => void showInfo()} />
      <div className="my-1 border-t border-border" />
      <MenuItem label="Delete from library" danger onClick={() => void deleteTrack(false)} />
      <MenuItem label="Delete file" danger onClick={() => void deleteTrack(true)} />
    </div>}

    {playlistOpen && <Dialog title="Add to playlist" onClose={() => setPlaylistOpen(false)}>
      {playlists.length ? <div className="space-y-1">{playlists.map(p => <button key={p.id} type="button" disabled={busy} onClick={() => void addPlaylist(p.id)} className="flex w-full items-center justify-between rounded-lg px-3 py-2 text-left text-sm text-primary hover:bg-surface-elevated"><span className="truncate">{p.name}</span><span className="text-xs text-tertiary">{p.isSmart ? 'Smart' : 'Playlist'}</span></button>)}</div> : <p className="text-sm text-secondary">No playlists yet. Create one in the Music workspace.</p>}
      {error && <p role="alert" className="mt-3 text-sm text-danger">{error}</p>}
    </Dialog>}

    {renameOpen && <Dialog title="Rename track" onClose={() => setRenameOpen(false)}>
      <p className="text-sm text-secondary">Rename the audio file while keeping the library item linked.</p>
      <input autoFocus value={name} onChange={e => setName(e.target.value)} onKeyDown={e => { if (e.key === 'Enter') void saveRename(); }} className="mt-4 w-full rounded-lg border border-border bg-surface px-3 py-2.5 text-sm text-primary outline-none focus:border-accent/50" />
      <div className="mt-4 flex justify-end gap-2"><button type="button" onClick={() => setRenameOpen(false)} className="rounded-lg border border-border px-3 py-2 text-sm">Cancel</button><button type="button" disabled={busy || !name.trim()} onClick={() => void saveRename()} className="rounded-lg bg-primary px-3 py-2 text-sm font-semibold text-background">{busy ? 'Renaming…' : 'Rename'}</button></div>
      {error && <p role="alert" className="mt-3 text-sm text-danger">{error}</p>}
    </Dialog>}

    {infoOpen && <Dialog title="Track information" onClose={() => setInfoOpen(false)}>
      {!info && !error && <p className="text-sm text-secondary">Loading file information…</p>}
      {info && <div className="grid grid-cols-[110px_1fr] gap-x-4 gap-y-2 text-sm"><span className="text-tertiary">Title</span><span className="truncate text-primary">{info.title}</span><span className="text-tertiary">Artist</span><span className="truncate text-primary">{info.artistName ?? 'Unknown artist'}</span><span className="text-tertiary">Album</span><span className="truncate text-primary">{info.album}</span><span className="text-tertiary">Format</span><span className="text-primary">{info.extension}</span><span className="text-tertiary">Size</span><span className="text-primary">{Math.round(info.sizeBytes / 1024 / 1024 * 10) / 10} MB</span><span className="text-tertiary">Modified</span><span className="text-primary">{new Date(info.modifiedUtc).toLocaleString()}</span><span className="text-tertiary">Path</span><span className="break-all text-primary">{info.path}</span></div>}
      {info && <button type="button" onClick={() => void navigator.clipboard?.writeText(info.path)} className="mt-5 rounded-lg border border-border px-3 py-2 text-sm text-primary hover:bg-surface-elevated">Copy file path</button>}
      {error && <p role="alert" className="mt-3 text-sm text-danger">{error}</p>}
    </Dialog>}
  </div>;
}

function MenuItem({ label, onClick, disabled = false, danger = false, render }: { label: string; onClick?: () => void; disabled?: boolean; danger?: boolean; render?: React.ReactNode }) {
  if (render) return <div role="menuitem" className="flex items-center rounded-lg px-3 py-1.5 text-sm text-primary">{render}</div>;
  return <button type="button" role="menuitem" disabled={disabled} onClick={onClick} className={`w-full rounded-lg px-3 py-2 text-left text-sm transition disabled:cursor-not-allowed disabled:opacity-35 ${danger ? 'text-danger hover:bg-danger/10' : 'text-primary hover:bg-surface-elevated'}`}>{label}</button>;
}

function Dialog({ title, children, onClose }: { title: string; children: React.ReactNode; onClose: () => void }) {
  return <div className="fixed inset-0 z-[60] grid place-items-center bg-black/60 p-4 backdrop-blur-sm" role="dialog" aria-modal="true" aria-label={title}><div className="w-full max-w-lg rounded-2xl border border-border bg-surface p-5 shadow-pop"><div className="flex items-center justify-between gap-4"><h2 className="text-lg font-semibold text-primary">{title}</h2><button type="button" aria-label="Close" onClick={onClose} className="grid size-8 place-items-center rounded-lg text-secondary hover:bg-surface-elevated hover:text-primary">×</button></div><div className="mt-5">{children}</div></div></div>;
}
