import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState, type ReactNode } from 'react';
import { trackStreamUrl, type TrackSummary } from '../api/music';

type RepeatMode = 'off' | 'track' | 'queue';

interface PlayerState {
  current: { track: TrackSummary } | null;
  playing: boolean;
  position: number;
  duration: number;
  queue: TrackSummary[];
  queueIndex: number;
  repeatMode: RepeatMode;
  shuffle: boolean;
  toggle: (track: TrackSummary) => void;
  playQueue: (tracks: TrackSummary[], index?: number) => void;
  enqueue: (track: TrackSummary) => void;
  next: () => void;
  previous: () => void;
  seek: (seconds: number) => void;
  setShuffle: (enabled: boolean) => void;
  cycleRepeat: () => void;
  stop: () => void;
}

const PlayerContext = createContext<PlayerState | null>(null);
const STORAGE_KEY = 'onidash.player.v1';

type PersistedPlayer = {
  queue: TrackSummary[];
  queueIndex: number;
  position: number;
  repeatMode: RepeatMode;
  shuffle: boolean;
};

function readPersisted(): PersistedPlayer {
  try {
    const parsed = JSON.parse(localStorage.getItem(STORAGE_KEY) ?? 'null') as Partial<PersistedPlayer> | null;
    if (parsed && Array.isArray(parsed.queue)) {
      return {
        queue: parsed.queue,
        queueIndex: Math.max(0, Math.min(parsed.queueIndex ?? 0, parsed.queue.length - 1)),
        position: Math.max(0, parsed.position ?? 0),
        repeatMode: parsed.repeatMode === 'track' || parsed.repeatMode === 'queue' ? parsed.repeatMode : 'off',
        shuffle: parsed.shuffle === true,
      };
    }
  } catch {
    // Corrupt local player state should never prevent the app from loading.
  }
  return { queue: [], queueIndex: 0, position: 0, repeatMode: 'off', shuffle: false };
}

export function PlayerProvider({ children }: { children: ReactNode }) {
  const audioRef = useRef<HTMLAudioElement | null>(null);
  const initial = useMemo(readPersisted, []);
  const [queue, setQueue] = useState<TrackSummary[]>(initial.queue);
  const [queueIndex, setQueueIndex] = useState(initial.queueIndex);
  const [current, setCurrent] = useState<TrackSummary | null>(initial.queue[initial.queueIndex] ?? null);
  const [playing, setPlaying] = useState(false);
  const [position, setPosition] = useState(initial.position);
  const [duration, setDuration] = useState(initial.queue[initial.queueIndex]?.durationSeconds ?? 0);
  const [repeatMode, setRepeatMode] = useState<RepeatMode>(initial.repeatMode);
  const [shuffle, setShuffleState] = useState(initial.shuffle);
  const saveTimer = useRef<number | undefined>(undefined);

  const persist = useCallback((next: Partial<PersistedPlayer> = {}) => {
    window.clearTimeout(saveTimer.current);
    saveTimer.current = window.setTimeout(() => {
      const state: PersistedPlayer = {
        queue: next.queue ?? queue,
        queueIndex: next.queueIndex ?? queueIndex,
        position: next.position ?? position,
        repeatMode: next.repeatMode ?? repeatMode,
        shuffle: next.shuffle ?? shuffle,
      };
      localStorage.setItem(STORAGE_KEY, JSON.stringify(state));
    }, 250);
  }, [position, queue, queueIndex, repeatMode, shuffle]);

  const loadTrack = useCallback((track: TrackSummary, index: number, autoplay = true, startPosition = 0) => {
    const audio = audioRef.current;
    setCurrent(track);
    setQueueIndex(index);
    setPosition(startPosition);
    setDuration(track.durationSeconds ?? 0);
    if (!audio) return;
    audio.src = trackStreamUrl(track.id);
    audio.currentTime = startPosition;
    if (autoplay) {
      setPlaying(true);
      void audio.play().catch(() => setPlaying(false));
    }
  }, []);

  const playQueue = useCallback((tracks: TrackSummary[], index = 0) => {
    if (tracks.length === 0) return;
    const safeIndex = Math.max(0, Math.min(index, tracks.length - 1));
    setQueue(tracks);
    loadTrack(tracks[safeIndex], safeIndex);
    persist({ queue: tracks, queueIndex: safeIndex, position: 0 });
  }, [loadTrack, persist]);

  const toggle = useCallback((track: TrackSummary) => {
    const audio = audioRef.current;
    if (!audio) return;
    if (current?.id === track.id) {
      if (audio.paused) {
        setPlaying(true);
        void audio.play().catch(() => setPlaying(false));
      } else audio.pause();
      return;
    }
    const existing = queue.findIndex((item) => item.id === track.id);
    if (existing >= 0) loadTrack(track, existing);
    else playQueue([track]);
  }, [current?.id, loadTrack, playQueue, queue]);

  const enqueue = useCallback((track: TrackSummary) => {
    setQueue((items) => items.some((item) => item.id === track.id) ? items : [...items, track]);
  }, []);

  const next = useCallback(() => {
    if (queue.length === 0) return;
    if (repeatMode === 'track' && current) {
      loadTrack(current, queueIndex);
      return;
    }
    let nextIndex = queueIndex + 1;
    if (nextIndex >= queue.length) {
      if (repeatMode !== 'queue') { setPlaying(false); return; }
      nextIndex = 0;
    }
    loadTrack(queue[nextIndex], nextIndex);
  }, [current, loadTrack, queue, queueIndex, repeatMode]);

  const previous = useCallback(() => {
    if (audioRef.current && audioRef.current.currentTime > 5) {
      audioRef.current.currentTime = 0;
      setPosition(0);
      return;
    }
    const previousIndex = Math.max(0, queueIndex - 1);
    if (queue[previousIndex]) loadTrack(queue[previousIndex], previousIndex);
  }, [loadTrack, queue, queueIndex]);

  const seek = useCallback((seconds: number) => {
    const audio = audioRef.current;
    if (audio && Number.isFinite(seconds)) { audio.currentTime = seconds; setPosition(seconds); }
  }, []);

  const setShuffle = useCallback((enabled: boolean) => setShuffleState(enabled), []);
  const cycleRepeat = useCallback(() => setRepeatMode((mode) => mode === 'off' ? 'track' : mode === 'track' ? 'queue' : 'off'), []);

  const stop = useCallback(() => {
    const audio = audioRef.current;
    if (audio) { audio.pause(); audio.removeAttribute('src'); audio.load(); }
    setCurrent(null); setPlaying(false); setPosition(0); setDuration(0);
    setQueueIndex(0); setQueue([]); persist({ queue: [], queueIndex: 0, position: 0 });
  }, [persist]);

  useEffect(() => () => { audioRef.current?.pause(); window.clearTimeout(saveTimer.current); }, []);
  useEffect(() => { persist(); }, [persist]);

  const value = useMemo<PlayerState>(() => ({
    current: current ? { track: current } : null, playing, position, duration, queue, queueIndex,
    repeatMode, shuffle, toggle, playQueue, enqueue, next, previous, seek, setShuffle, cycleRepeat, stop,
  }), [current, playing, position, duration, queue, queueIndex, repeatMode, shuffle, toggle, playQueue, enqueue, next, previous, seek, setShuffle, cycleRepeat, stop]);

  return <PlayerContext.Provider value={value}>
    {children}
    <audio ref={audioRef} preload="none" onPlay={() => setPlaying(true)} onPause={() => setPlaying(false)} onEnded={next}
      onTimeUpdate={(event) => { setPosition(event.currentTarget.currentTime); persist({ position: event.currentTarget.currentTime }); }}
      onLoadedMetadata={(event) => setDuration(event.currentTarget.duration || 0)} onError={() => setPlaying(false)} />
  </PlayerContext.Provider>;
}

export function usePlayer(): PlayerState {
  const context = useContext(PlayerContext);
  if (!context) throw new Error('usePlayer must be used inside <PlayerProvider>.');
  return context;
}
