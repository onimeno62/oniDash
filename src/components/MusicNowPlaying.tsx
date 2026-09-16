import { useEffect, useMemo, useState } from 'react';
import { fetchMusicLyrics, saveMusicLyrics, formatDuration, albumCoverUrl, type TrackSummary } from '../api/music';
import { usePlayer } from '../hooks/usePlayer';
import { PlayIcon } from './icons';

type LyricLine = { time: number; text: string };

function parseLyrics(text: string): LyricLine[] {
  const lines: LyricLine[] = [];
  for (const raw of text.split(/\r?\n/)) {
    const matches = [...raw.matchAll(/\[(\d{1,3}):(\d{2})(?:[.:](\d{1,3}))?\]/g)];
    const lyric = raw.replace(/\[(\d{1,3}):(\d{2})(?:[.:](\d{1,3}))?\]/g, '').trim();
    for (const match of matches) {
      const minutes = Number(match[1]);
      const seconds = Number(match[2]);
      const fraction = match[3] ? Number(`0.${match[3].padEnd(3, '0')}`) : 0;
      lines.push({ time: minutes * 60 + seconds + fraction, text: lyric });
    }
  }
  return lines.sort((a, b) => a.time - b.time);
}

export function MusicNowPlaying({ open, onClose }: { open: boolean; onClose: () => void }) {
  const player = usePlayer();
  const track = player.current;
  const [mode, setMode] = useState<'player' | 'queue' | 'lyrics'>('player');
  const [lyrics, setLyrics] = useState<{ text: string; synchronized: boolean } | null>(null);
  const [lyricsError, setLyricsError] = useState<string | null>(null);
  const [loadingLyrics, setLoadingLyrics] = useState(false);
  const [editingLyrics, setEditingLyrics] = useState(false);
  const [lyricsDraft, setLyricsDraft] = useState('');
  const [savingLyrics, setSavingLyrics] = useState(false);

  useEffect(() => {
    if (!open || !track || mode !== 'lyrics') return;
    let cancelled = false;
    setLoadingLyrics(true);
    setLyricsError(null);
    setEditingLyrics(false);
    void fetchMusicLyrics(track.id).then((value) => {
      if (!cancelled) { setLyrics(value); setLyricsDraft(value.text); }
    }).catch((error) => {
      if (!cancelled) setLyricsError(error instanceof Error ? error.message : 'Lyrics are unavailable.');
    }).finally(() => {
      if (!cancelled) setLoadingLyrics(false);
    });
    return () => { cancelled = true; };
  }, [open, track?.id, mode]);

  const parsedLyrics = useMemo(() => lyrics?.synchronized ? parseLyrics(lyrics.text) : [], [lyrics]);
  const activeLine = useMemo(() => {
    if (!parsedLyrics.length) return -1;
    let index = -1;
    for (let i = 0; i < parsedLyrics.length; i += 1) {
      if (parsedLyrics[i].time <= player.position + 0.15) index = i;
      else break;
    }
    return index;
  }, [parsedLyrics, player.position]);

  useEffect(() => {
    if (mode !== 'lyrics' || activeLine < 0) return;
    const element = document.getElementById(`lyric-line-${activeLine}`);
    element?.scrollIntoView({ behavior: 'smooth', block: 'center' });
  }, [activeLine, mode]);

  if (!open || !track) return null;
  const close = () => { setMode('player'); setEditingLyrics(false); onClose(); };
  const saveLyrics = async () => {
    setSavingLyrics(true); setLyricsError(null);
    try {
      const synchronized = parseLyrics(lyricsDraft).length > 0;
      await saveMusicLyrics(track.id, lyricsDraft, synchronized);
      setLyrics({ text: lyricsDraft, synchronized });
      setEditingLyrics(false);
    } catch (error) {
      setLyricsError(error instanceof Error ? error.message : 'Could not save lyrics.');
    } finally { setSavingLyrics(false); }
  };

  return <div className="fixed inset-0 z-[70] flex items-end justify-center bg-black/70 p-0 backdrop-blur-md sm:items-center sm:p-6" role="dialog" aria-modal="true" aria-label="Now playing">
    <div className="relative flex max-h-[92vh] w-full max-w-6xl flex-col overflow-hidden rounded-t-[30px] border border-white/10 bg-[#0b0c12] shadow-2xl sm:rounded-[30px]">
      <div className="flex items-center justify-between border-b border-white/5 px-5 py-4 sm:px-7">
        <div><p className="text-[10px] font-semibold uppercase tracking-[.22em] text-accent">Now playing</p><p className="mt-1 text-xs text-white/40">{track.artistName ?? 'Unknown artist'} · {track.albumTitle}</p></div>
        <button type="button" onClick={close} className="grid size-9 place-items-center rounded-full bg-white/5 text-white/60 hover:bg-white/10 hover:text-white" aria-label="Close">×</button>
      </div>

      <div className="flex min-h-0 flex-1 flex-col lg:grid lg:grid-cols-[minmax(0,1fr)_380px]">
        <div className="relative flex min-h-0 flex-col items-center justify-center overflow-hidden px-6 py-8 sm:px-12">
          <div className="pointer-events-none absolute inset-0 bg-[radial-gradient(circle_at_50%_20%,rgba(124,106,245,.24),transparent_45%)]" />
          <div className="relative w-full max-w-[420px]">
            <div className="aspect-square overflow-hidden rounded-[28px] border border-white/10 bg-white/5 shadow-2xl shadow-black/50">
              {track.hasCover && track.albumId ? <img src={albumCoverUrl(track.albumId)} alt="" className="h-full w-full object-cover" /> : <div className="grid h-full place-items-center text-white/20"><span className="text-7xl">♫</span></div>}
            </div>
          </div>
          <div className="relative mt-7 w-full max-w-[560px]">
            <div className="flex items-end justify-between gap-4"><div className="min-w-0"><h2 className="truncate text-2xl font-semibold tracking-tight text-white sm:text-3xl">{track.title}</h2><p className="mt-1 truncate text-sm text-white/45">{track.artistName ?? 'Unknown artist'} · {track.albumTitle}</p></div><span className="shrink-0 rounded-full border border-white/10 bg-white/5 px-3 py-1 text-xs text-white/50">{track.rating > 0 ? `${track.rating}/5` : 'Unrated'}</span></div>
            <div className="mt-7"><input aria-label="Playback position" type="range" min={0} max={Math.max(1, Math.floor(player.duration))} value={Math.min(Math.floor(player.position), Math.max(1, Math.floor(player.duration)))} onChange={(event) => player.seek(Number(event.target.value))} className="w-full accent-[rgb(124,106,245)]" /><div className="mt-2 flex justify-between text-[11px] tabular-nums text-white/35"><span>{formatDuration(player.position)}</span><span>{formatDuration(player.duration)}</span></div></div>
            <div className="mt-5 flex items-center justify-center gap-3 sm:gap-5"><button type="button" onClick={player.previous} aria-label="Previous track" className="grid size-10 place-items-center rounded-full text-white/60 hover:bg-white/5 hover:text-white">|‹</button><button type="button" onClick={() => player.setShuffle(!player.shuffle)} aria-label="Toggle shuffle" className={`grid size-9 place-items-center rounded-full ${player.shuffle ? 'bg-accent/20 text-accent' : 'text-white/40 hover:text-white'}`}>⇄</button><button type="button" onClick={() => player.toggle(track)} aria-label={player.playing ? 'Pause' : 'Play'} className="grid size-14 place-items-center rounded-full bg-white text-[#0b0c12] shadow-xl hover:scale-105"><PlayIcon className="size-6" /></button><button type="button" onClick={player.cycleRepeat} aria-label="Cycle repeat" className={`grid size-9 place-items-center rounded-full ${player.repeatMode !== 'off' ? 'bg-accent/20 text-accent' : 'text-white/40 hover:text-white'}`}>↻</button><button type="button" onClick={player.next} aria-label="Next track" className="grid size-10 place-items-center rounded-full text-white/60 hover:bg-white/5 hover:text-white">›|</button></div>
            <div className="mt-5 flex justify-center gap-2"><button type="button" onClick={() => setMode('player')} className={`rounded-full px-4 py-2 text-xs ${mode === 'player' ? 'bg-white/10 text-white' : 'text-white/40 hover:text-white'}`}>Player</button><button type="button" onClick={() => setMode('queue')} className={`rounded-full px-4 py-2 text-xs ${mode === 'queue' ? 'bg-white/10 text-white' : 'text-white/40'}`}>Queue · {player.queue.length}</button><button type="button" onClick={() => setMode('lyrics')} className={`rounded-full px-4 py-2 text-xs ${mode === 'lyrics' ? 'bg-white/10 text-white' : 'text-white/40'}`}>Lyrics</button></div>
          </div>
        </div>

        <aside className="min-h-0 border-t border-white/5 bg-white/[.025] lg:border-l lg:border-t-0">
          {mode === 'lyrics' ? <LyricsPanel loading={loadingLyrics} error={lyricsError} lyrics={lyrics} lines={parsedLyrics} activeLine={activeLine} editing={editingLyrics} draft={lyricsDraft} onDraft={setLyricsDraft} onEdit={() => setEditingLyrics(true)} onCancel={() => { setEditingLyrics(false); setLyricsDraft(lyrics?.text ?? ''); }} onSave={() => void saveLyrics()} saving={savingLyrics} onSeek={player.seek} /> : mode === 'queue' ? <QueuePanel /> : <NowPlayingInfo track={track} />}
        </aside>
      </div>
    </div>
  </div>;
}

function NowPlayingInfo({ track }: { track: TrackSummary }) {
  return <div className="h-full overflow-auto p-6 sm:p-7"><p className="text-[10px] font-semibold uppercase tracking-[.2em] text-white/30">Track details</p><div className="mt-5 space-y-5"><Info label="Artist" value={track.artistName ?? 'Unknown artist'} /><Info label="Album" value={track.albumTitle || 'Unknown album'} /><Info label="Genre" value={track.genre ?? 'Unknown genre'} /><Info label="Release year" value={track.year ? String(track.year) : 'Unknown'} /><Info label="Track" value={track.trackNumber ? String(track.trackNumber) : '—'} /><Info label="Duration" value={formatDuration(track.durationSeconds)} /></div><div className="mt-8 rounded-2xl border border-white/5 bg-white/[.03] p-4"><p className="text-sm font-medium text-white">Local library</p><p className="mt-1 text-xs leading-5 text-white/40">This track is playing directly from your oniDash library. No streaming service is required.</p></div></div>;
}

function QueuePanel() {
  const player = usePlayer();
  return <div className="flex h-full min-h-0 flex-col p-5 sm:p-6"><div className="flex items-center justify-between"><div><p className="text-[10px] font-semibold uppercase tracking-[.2em] text-white/30">Up next</p><h3 className="mt-1 text-lg font-semibold text-white">Playback queue</h3></div><button type="button" onClick={player.clearQueue} disabled={!player.queue.length} className="text-xs text-white/35 hover:text-white disabled:opacity-30">Clear</button></div>{player.queue.length ? <ol className="mt-5 min-h-0 flex-1 space-y-1 overflow-auto">{player.queue.map((item, index) => <li key={`${item.id}-${index}`}><button type="button" onClick={() => player.playQueue(player.queue, index)} className={`flex w-full items-center gap-3 rounded-xl p-2.5 text-left hover:bg-white/5 ${player.queueIndex === index ? 'bg-accent/10' : ''}`}><span className="w-5 text-center text-[11px] tabular-nums text-white/25">{index + 1}</span>{item.hasCover && item.albumId ? <img src={albumCoverUrl(item.albumId)} alt="" className="size-10 rounded-lg object-cover" /> : <div className="grid size-10 place-items-center rounded-lg bg-white/5 text-white/20">♫</div>}<span className="min-w-0 flex-1"><span className="block truncate text-sm text-white">{item.title}</span><span className="block truncate text-xs text-white/35">{item.artistName ?? 'Unknown artist'}</span></span><span className="text-[11px] tabular-nums text-white/30">{formatDuration(item.durationSeconds)}</span></button></li>)}</ol> : <div className="grid flex-1 place-items-center text-center"><div><p className="text-sm text-white/60">Queue is empty</p><p className="mt-1 text-xs text-white/30">Use Play next or Add to queue from a track menu.</p></div></div>}</div>;
}

function LyricsPanel({ loading, error, lyrics, lines, activeLine, editing, draft, onDraft, onEdit, onCancel, onSave, saving, onSeek }: { loading: boolean; error: string | null; lyrics: { text: string; synchronized: boolean } | null; lines: LyricLine[]; activeLine: number; editing: boolean; draft: string; onDraft: (value: string) => void; onEdit: () => void; onCancel: () => void; onSave: () => void; saving: boolean; onSeek: (seconds: number) => void }) {
  return <div className="flex h-full min-h-0 flex-col p-5 sm:p-6"><div className="flex items-center justify-between gap-3"><div><p className="text-[10px] font-semibold uppercase tracking-[.2em] text-white/30">Lyrics</p><p className="mt-1 text-xs text-white/35">{lyrics?.synchronized ? 'Synced · click a line to seek' : 'Plain text'}</p></div>{!loading && !error && <button type="button" onClick={editing ? onCancel : onEdit} className="rounded-lg border border-white/10 px-3 py-1.5 text-xs text-white/55 hover:bg-white/5 hover:text-white">{editing ? 'Cancel' : 'Edit'}</button>}</div>{loading ? <div className="mt-8 space-y-3">{Array.from({ length: 9 }).map((_, index) => <div key={index} className="h-4 animate-pulse rounded bg-white/5" />)}</div> : error ? <div className="mt-8 rounded-2xl border border-red-400/10 bg-red-400/5 p-4 text-sm text-white/50">{error}</div> : editing ? <div className="flex min-h-0 flex-1 flex-col"><textarea value={draft} onChange={(event) => onDraft(event.target.value)} spellCheck={false} aria-label="Lyrics" className="mt-5 min-h-[320px] flex-1 resize-none rounded-2xl border border-white/10 bg-black/20 p-4 font-mono text-sm leading-6 text-white/75 outline-none focus:border-accent/50" placeholder="Paste plain text or LRC timestamps, for example [01:23.45] First line" /><div className="mt-3 flex items-center justify-between gap-3"><p className="text-[11px] text-white/30">LRC timestamps are detected automatically.</p><button type="button" disabled={saving} onClick={onSave} className="rounded-xl bg-accent px-4 py-2 text-xs font-semibold text-white disabled:opacity-50">{saving ? 'Saving…' : 'Save lyrics'}</button></div></div> : lyrics?.synchronized && lines.length ? <div className="mt-5 min-h-0 flex-1 overflow-auto pr-2">{lines.map((line, index) => <button key={`${line.time}-${index}`} id={`lyric-line-${index}`} type="button" onClick={() => onSeek(line.time)} className={`block w-full rounded-xl px-3 py-2 text-left text-base leading-7 transition ${index === activeLine ? 'bg-accent/10 text-white' : 'text-white/35 hover:bg-white/5 hover:text-white/70'}`} aria-current={index === activeLine ? 'true' : undefined}>{line.text || '♪'}</button>)}</div> : lyrics?.text ? <div className="mt-5 min-h-0 flex-1 overflow-auto whitespace-pre-wrap rounded-2xl border border-white/5 bg-white/[.02] p-4 text-base leading-7 text-white/65">{lyrics.text}</div> : <div className="mt-8 rounded-2xl border border-dashed border-white/10 p-6 text-sm text-white/35">No lyrics are available for this track. Use Edit to add plain text or synchronized LRC lyrics.</div>}</div>;
}

function Info({ label, value }: { label: string; value: string }) { return <div><p className="text-[10px] uppercase tracking-[.16em] text-white/25">{label}</p><p className="mt-1 truncate text-sm text-white/75">{value}</p></div>; }
