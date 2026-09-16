import { useRef } from 'react';
import type { TrackSummary } from '../../api/music';
import { TrackCard } from './TrackCard';

export function TrackCarousel({
  title,
  subtitle,
  tracks,
  currentPlayingId,
  isPlaying,
  onPlay,
}: {
  title: string;
  subtitle?: string;
  tracks: TrackSummary[];
  currentPlayingId?: string | null;
  isPlaying?: boolean;
  onPlay: (track: TrackSummary) => void;
}) {
  const scrollRef = useRef<HTMLDivElement | null>(null);

  if (!tracks.length) return null;

  const scroll = (direction: 'left' | 'right') => {
    if (scrollRef.current) {
      const scrollAmount = direction === 'left' ? -420 : 420;
      scrollRef.current.scrollBy({ left: scrollAmount, behavior: 'smooth' });
    }
  };

  return (
    <section className="relative flex flex-col space-y-3.5">
      {/* Header with Title and Scroll Controls */}
      <div className="flex items-end justify-between">
        <div>
          <h2 className="text-xl font-bold tracking-tight text-primary">
            {title}
          </h2>
          {subtitle && (
            <p className="mt-0.5 text-xs text-secondary">{subtitle}</p>
          )}
        </div>

        <div className="flex items-center gap-2">
          <span className="hidden text-xs font-semibold tabular-nums text-tertiary sm:inline">
            {tracks.length} tracks
          </span>
          <div className="flex items-center gap-1 rounded-xl border border-border/70 bg-surface/70 p-1">
            <button
              type="button"
              aria-label={`Scroll ${title} backward`}
              onClick={() => scroll('left')}
              className="flex size-7 items-center justify-center rounded-lg text-secondary transition-colors hover:bg-surface-elevated hover:text-primary active:scale-95"
            >
              ‹
            </button>
            <button
              type="button"
              aria-label={`Scroll ${title} forward`}
              onClick={() => scroll('right')}
              className="flex size-7 items-center justify-center rounded-lg text-secondary transition-colors hover:bg-surface-elevated hover:text-primary active:scale-95"
            >
              ›
            </button>
          </div>
        </div>
      </div>

      {/* Smooth Horizontal Carousel */}
      <div
        ref={scrollRef}
        className="flex snap-x snap-mandatory gap-4 overflow-x-auto pb-4 pt-1 [scrollbar-width:none] [&::-webkit-scrollbar]:hidden"
      >
        {tracks.map((track) => (
          <TrackCard
            key={track.id}
            track={track}
            isPlaying={isPlaying && currentPlayingId === track.id}
            onPlay={() => onPlay(track)}
          />
        ))}
      </div>
    </section>
  );
}
