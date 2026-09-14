import { useState } from 'react';
import { formatDuration, type TrackSummary } from '../api/music';
import { usePlayer } from '../hooks/usePlayer';
import { PlayIcon } from './icons';
import { MusicNowPlaying } from './MusicNowPlaying';
import { MusicVisualizer } from './MusicVisualizer';

export function MusicPlayerBar() {
  const { current, playing, position, duration, queue, repeatMode, shuffle, volume, muted, toggle, next, previous, seek, setShuffle, cycleRepeat, setVolume, setMuted, stop } = usePlayer();
  const [expanded, setExpanded] = useState(false);
  if (!current) return null;
  return <>
    <div className="fixed inset-x-0 bottom-0 z-40 border-t border-white/10 bg-[#0d0e14]/95 shadow-2xl backdrop-blur-xl" data-testid="player-bar">
      <div className="mx-auto flex max-w-[1500px] items-center gap-2 px-3 py-2.5 sm:px-5">
        <button type="button" onClick={() => setExpanded(true)} className="hidden min-w-0 flex-1 items-center gap-3 text-left sm:flex">
          {current.hasCover && current.albumId ? <img src={`/api/music/albums/${current.albumId}/cover`} alt="" className="size-10 rounded-lg object-cover" /> : <div className="grid size-10 place-items-center rounded-lg bg-white/5 text-white/30">♫</div>}
          <span className="min-w-0"><span className="block truncate text-sm font-medium text-white">{current.title}</span><span className="block truncate text-xs text-white/40">{current.artistName ?? 'Unknown artist'} · {current.albumTitle}</span></span>
        </button>
        <div className="hidden w-28 shrink-0 lg:block"><MusicVisualizer mode="bars" height={30} /></div>
        <button type="button" aria-label="Previous track" onClick={previous} className="icon-btn">‹</button>
        <button type="button" aria-label={playing ? 'Pause' : 'Play'} onClick={() => toggle(current)} className="grid size-10 shrink-0 place-items-center rounded-full bg-white text-[#0d0e14] hover:scale-105"><PlayIcon className="size-4" /></button>
        <button type="button" aria-label="Next track" onClick={next} className="icon-btn">›</button>
        <div className="hidden min-w-0 flex-[1.4] items-center gap-3 md:flex"><span className="w-10 text-right text-[11px] tabular-nums text-white/30">{formatDuration(position)}</span><input aria-label="Seek" type="range" min={0} max={Math.max(1, Math.floor(duration))} value={Math.min(Math.floor(position), Math.max(1, Math.floor(duration)))} onChange={(event) => seek(Number(event.target.value))} className="min-w-0 flex-1 accent-[rgb(124,106,245)]" /><span className="w-10 text-[11px] tabular-nums text-white/30">{formatDuration(duration)}</span></div>
        <button type="button" aria-label={`Shuffle ${shuffle ? 'on' : 'off'}`} aria-pressed={shuffle} onClick={() => setShuffle(!shuffle)} className={`icon-btn hidden sm:inline-grid ${shuffle ? 'text-accent' : ''}`}>⇄</button>
        <button type="button" aria-label={`Repeat ${repeatMode}`} onClick={cycleRepeat} className={`icon-btn hidden sm:inline-grid ${repeatMode !== 'off' ? 'text-accent' : ''}`}>↻</button>
        <button type="button" aria-label={muted ? 'Unmute' : 'Mute'} onClick={() => setMuted(!muted)} className="icon-btn hidden sm:inline-grid">{muted ? '🔇' : '🔊'}</button>
        <input aria-label="Volume" type="range" min={0} max={1} step={0.01} value={muted ? 0 : volume} onChange={(event) => setVolume(Number(event.target.value))} className="hidden w-20 accent-[rgb(124,106,245)] lg:block" />
        <button type="button" onClick={() => setExpanded(true)} className="icon-btn" aria-label={`Open player and queue (${queue.length})`}>☷</button>
        <button type="button" aria-label="Stop" onClick={stop} className="icon-btn hidden sm:inline-grid">×</button>
      </div>
    </div>
    <MusicNowPlaying open={expanded} onClose={() => setExpanded(false)} />
  </>;
}

export function QueueButton({ track }: { track: TrackSummary }) { const { enqueue, playNext } = usePlayer(); return <span className="flex shrink-0 gap-1"><button type="button" aria-label={`Play ${track.title} next`} onClick={() => playNext(track)} className="icon-btn">↟</button><button type="button" aria-label={`Add ${track.title} to queue`} onClick={() => enqueue(track)} className="icon-btn">＋</button></span>; }
