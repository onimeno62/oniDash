import { createContext, useContext, useMemo, useSyncExternalStore, type ReactNode } from 'react';
import { playerService, type PlayerSnapshot, type RepeatMode } from '../player/playerService';
import type { TrackSummary } from '../api/music';

export type { RepeatMode };
export type PlayerState = PlayerSnapshot & {
  toggle: (track: TrackSummary) => void;
  playQueue: (tracks: TrackSummary[], index?: number) => void;
  enqueue: (track: TrackSummary) => void;
  playNext: (track: TrackSummary) => void;
  next: () => void;
  previous: () => void;
  seek: (seconds: number) => void;
  setShuffle: (enabled: boolean) => void;
  cycleRepeat: () => void;
  setGapless: (enabled: boolean) => void;
  setVolume: (value: number) => void;
  setMuted: (muted: boolean) => void;
  stop: () => void;
  clearQueue: () => void;
  removeFromQueue: (trackId: string) => void;
  audioElement: HTMLAudioElement;
};

const PlayerContext = createContext<PlayerState | null>(null);

export function PlayerProvider({ children }: { children: ReactNode }) {
  const snapshot = useSyncExternalStore(listener => playerService.subscribe(listener), () => playerService.snapshot(), () => playerService.snapshot());
  const value = useMemo<PlayerState>(() => ({
    ...snapshot,
    toggle: track => playerService.toggle(track),
    playQueue: (tracks, index) => playerService.playQueue(tracks, index),
    enqueue: track => playerService.enqueue(track),
    playNext: track => playerService.playNext(track),
    next: () => void playerService.advance(),
    previous: () => playerService.previous(),
    seek: seconds => playerService.seek(seconds),
    setShuffle: enabled => playerService.setShuffle(enabled),
    cycleRepeat: () => playerService.cycleRepeat(),
    setGapless: enabled => playerService.setGapless(enabled),
    setVolume: volume => playerService.setVolume(volume),
    setMuted: muted => playerService.setMuted(muted),
    stop: () => playerService.stop(),
    clearQueue: () => playerService.clearQueue(),
    removeFromQueue: trackId => playerService.removeFromQueue(trackId),
    audioElement: playerService.getAudioElement(),
  }), [snapshot]);
  return <PlayerContext.Provider value={value}>{children}</PlayerContext.Provider>;
}

export function usePlayer(): PlayerState {
  const context = useContext(PlayerContext);
  if (!context) throw new Error('usePlayer must be used inside <PlayerProvider>.');
  return context;
}
