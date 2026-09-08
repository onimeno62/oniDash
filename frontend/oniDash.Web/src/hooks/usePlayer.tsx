import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState, type ReactNode } from 'react';
import { trackStreamUrl, type TrackSummary } from '../api/music';

interface PlayerState {
  /** Currently loaded track, if any. */
  current: { track: TrackSummary } | null;
  playing: boolean;
  /** Playback position / total duration in seconds (0 while unknown). */
  position: number;
  duration: number;
  /** Starts (or pauses/resumes) a track. */
  toggle: (track: TrackSummary) => void;
  seek: (seconds: number) => void;
  stop: () => void;
}

const PlayerContext = createContext<PlayerState | null>(null);

/**
 * App-wide single audio player: one <audio> element, context-exposed state and
 * controls. Stream URLs come from the local API (range-enabled).
 */
export function PlayerProvider({ children }: { children: ReactNode }) {
  const audioRef = useRef<HTMLAudioElement | null>(null);
  const [current, setCurrent] = useState<TrackSummary | null>(null);
  const [playing, setPlaying] = useState(false);
  const [position, setPosition] = useState(0);
  const [duration, setDuration] = useState(0);

  const toggle = useCallback((track: TrackSummary) => {
    const audio = audioRef.current;
    if (!audio) {
      return;
    }

    if (current?.id === track.id) {
      if (audio.paused) {
        // Optimistic: onPause/onError correct this if playback never starts.
        setPlaying(true);
        void audio.play().catch(() => setPlaying(false));
      } else {
        audio.pause();
      }
      return;
    }

    setCurrent(track);
    setPosition(0);
    setDuration(track.durationSeconds ?? 0);
    setPlaying(true);
    audio.src = trackStreamUrl(track.id);
    void audio.play().catch(() => setPlaying(false));
  }, [current?.id]);

  const seek = useCallback((seconds: number) => {
    const audio = audioRef.current;
    if (audio && Number.isFinite(seconds)) {
      audio.currentTime = seconds;
    }
  }, []);

  const stop = useCallback(() => {
    const audio = audioRef.current;
    if (audio) {
      audio.pause();
      audio.removeAttribute('src');
      audio.load();
    }
    setCurrent(null);
    setPlaying(false);
    setPosition(0);
    setDuration(0);
  }, []);

  useEffect(() => () => audioRef.current?.pause(), []);

  const value = useMemo<PlayerState>(
    () => ({ current: current ? { track: current } : null, playing, position, duration, toggle, seek, stop }),
    [current, playing, position, duration, toggle, seek, stop],
  );

  return (
    <PlayerContext.Provider value={value}>
      {children}
      <audio
        ref={audioRef}
        preload="none"
        onPlay={() => setPlaying(true)}
        onPause={() => setPlaying(false)}
        onEnded={() => {
          setPlaying(false);
          setPosition(0);
        }}
        onTimeUpdate={(event) => setPosition(event.currentTarget.currentTime)}
        onLoadedMetadata={(event) => setDuration(event.currentTarget.duration || 0)}
        onError={() => {
          setPlaying(false);
        }}
      />
    </PlayerContext.Provider>
  );
}

export function usePlayer(): PlayerState {
  const context = useContext(PlayerContext);
  if (!context) {
    throw new Error('usePlayer must be used inside <PlayerProvider>.');
  }

  return context;
}
