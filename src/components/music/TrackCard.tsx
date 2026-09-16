import { useState } from 'react';
import { albumCoverUrl, formatDuration, type TrackSummary } from '../../api/music';
import { PlayIcon } from '../icons';
import { MusicTrackMenu } from '../MusicTrackMenu';

export function TrackCard({
  track,
  isPlaying,
  onPlay,
}: {
  track: TrackSummary;
  isPlaying?: boolean;
  onPlay: () => void;
}) {
  const [imgError, setImgError] = useState(false);

  return (
    <article
      id={`track-card-${track.id}`}
      className="group relative flex w-[190px] shrink-0 snap-start flex-col rounded-2xl border border-border/80 bg-surface/85 p-3.5 shadow-sm transition-all duration-300 hover:-translate-y-1 hover:border-accent/40 hover:bg-surface-elevated hover:shadow-xl sm:w-[210px]"
    >
      {/* Artwork Box with Ambient Hover Glow */}
      <div className="relative aspect-square w-full overflow-hidden rounded-xl bg-surface-elevated shadow-inner">
        {track.albumId && track.hasCover && !imgError ? (
          <img
            src={albumCoverUrl(track.albumId)}
            alt={track.albumTitle ?? track.title}
            loading="lazy"
            onError={() => setImgError(true)}
            className="h-full w-full object-cover transition-transform duration-500 ease-out group-hover:scale-105"
          />
        ) : (
          <div className="flex h-full w-full flex-col items-center justify-center bg-gradient-to-br from-surface-elevated to-surface p-4 text-center">
            <span className="text-3xl opacity-30">🎵</span>
            <span className="mt-2 line-clamp-1 text-[11px] font-medium text-tertiary">
              {track.albumTitle ?? 'Audio'}
            </span>
          </div>
        )}

        {/* Gradient Scrim */}
        <div className="pointer-events-none absolute inset-0 bg-gradient-to-t from-black/60 via-transparent to-transparent opacity-0 transition-opacity duration-300 group-hover:opacity-100" />

        {/* Floating Play Button */}
        <button
          type="button"
          id={`play-btn-${track.id}`}
          onClick={onPlay}
          aria-label={`${isPlaying ? 'Pause' : 'Play'} ${track.title}`}
          className={`absolute bottom-2.5 right-2.5 flex size-11 items-center justify-center rounded-full bg-accent text-white shadow-lg transition-all duration-300 hover:scale-110 active:scale-95 ${
            isPlaying
              ? 'opacity-100 ring-2 ring-white/50'
              : 'translate-y-2 opacity-0 group-hover:translate-y-0 group-hover:opacity-100'
          }`}
        >
          {isPlaying ? (
            <span className="flex items-center gap-0.5">
              <span className="h-3.5 w-1 animate-pulse rounded-full bg-white" />
              <span className="h-4 w-1 animate-pulse rounded-full bg-white [animation-delay:150ms]" />
              <span className="h-3 w-1 animate-pulse rounded-full bg-white [animation-delay:300ms]" />
            </span>
          ) : (
            <PlayIcon className="size-5 translate-x-0.5 fill-current" />
          )}
        </button>

        {/* Top Badges: Rating or Genre */}
        {track.genre && (
          <span className="absolute left-2 top-2 rounded-md bg-black/50 px-2 py-0.5 text-[10px] font-medium uppercase tracking-wider text-white/80 backdrop-blur-md">
            {track.genre}
          </span>
        )}
      </div>

      {/* Metadata & Actions */}
      <div className="mt-3 flex items-start justify-between gap-1.5">
        <div className="min-w-0 flex-1">
          <p
            className={`truncate text-sm font-semibold tracking-tight transition-colors ${
              isPlaying ? 'text-accent' : 'text-primary group-hover:text-accent'
            }`}
            title={track.title}
          >
            {track.title}
          </p>
          <p className="mt-0.5 truncate text-xs text-secondary" title={track.artistName ?? 'Unknown artist'}>
            {track.artistName ?? 'Unknown artist'}
          </p>
          <div className="mt-1.5 flex items-center gap-1.5 text-[11px] text-tertiary">
            <span className="truncate max-w-[120px]">{track.albumTitle ?? 'Single'}</span>
            <span>·</span>
            <span className="tabular-nums">{formatDuration(track.durationSeconds)}</span>
          </div>
        </div>

        {/* Track Menu Dropdown */}
        <div className="shrink-0 -mr-1.5">
          <MusicTrackMenu track={track} onChanged={() => undefined} />
        </div>
      </div>
    </article>
  );
}
