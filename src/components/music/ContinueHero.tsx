import { albumCoverUrl, formatDuration, type TrackSummary } from '../../api/music';
import { PlayIcon } from '../icons';

export function ContinueHero({
  track,
  progress,
  isPlaying,
  onPlay,
}: {
  track: TrackSummary | null;
  progress: number;
  isPlaying?: boolean;
  onPlay: () => void;
}) {
  if (!track) {
    return (
      <section
        id="continue-hero-empty"
        className="relative overflow-hidden rounded-3xl border border-border/70 bg-gradient-to-br from-surface via-surface to-surface-elevated/40 p-8 shadow-sm md:p-10"
      >
        <div className="flex items-center gap-2">
          <span className="h-1.5 w-1.5 rounded-full bg-accent animate-ping" />
          <p className="text-xs font-semibold uppercase tracking-[0.2em] text-accent">
            Current Session
          </p>
        </div>
        <h2 className="mt-3 text-2xl font-bold tracking-tight text-primary md:text-3xl">
          Ready to listen
        </h2>
        <p className="mt-2 max-w-xl text-sm leading-relaxed text-secondary">
          Pick any track, album, or playlist below to begin your listening session. oniDash will keep your place and track statistics automatically.
        </p>
      </section>
    );
  }

  const duration = track.durationSeconds ?? 0;
  const percent = duration ? Math.min(100, Math.max(0, (progress / duration) * 100)) : 0;
  const cover = track.albumId ? albumCoverUrl(track.albumId) : null;

  return (
    <section
      id="continue-hero"
      className="group relative overflow-hidden rounded-3xl border border-border/80 bg-gradient-to-r from-surface/95 via-surface to-surface-elevated/60 p-6 shadow-xl transition-all duration-300 md:p-8 lg:p-10"
    >
      {/* Dynamic Ambient Background Blur */}
      {cover && (
        <div
          className="pointer-events-none absolute inset-0 -z-10 bg-cover bg-center opacity-[0.14] blur-3xl saturate-150 transition-opacity duration-700 group-hover:opacity-20"
          style={{ backgroundImage: `url(${cover})` }}
          aria-hidden="true"
        />
      )}

      {/* Warm Ambient Vignette Gradient */}
      <div className="pointer-events-none absolute inset-0 -z-10 bg-radial-at-tr from-accent/10 via-transparent to-transparent" />

      <div className="grid items-center gap-6 md:gap-8 lg:grid-cols-[260px_1fr]">
        {/* Album Artwork Stage */}
        <div className="relative mx-auto aspect-square w-full max-w-[260px] overflow-hidden rounded-2xl border border-white/10 shadow-2xl transition-transform duration-500 group-hover:scale-[1.02]">
          {cover ? (
            <img
              src={cover}
              alt={track.albumTitle ?? track.title}
              className="h-full w-full object-cover"
            />
          ) : (
            <div className="flex h-full w-full items-center justify-center bg-surface-elevated text-4xl text-primary/20">
              🎵
            </div>
          )}

          {/* Glowing Vinyl Center Ring Indicator when Playing */}
          {isPlaying && (
            <div className="absolute inset-0 flex items-center justify-center bg-black/20 backdrop-blur-[1px]">
              <span className="flex size-14 items-center justify-center rounded-full bg-accent/90 text-white shadow-2xl backdrop-blur-md">
                <span className="flex items-end gap-1 h-5">
                  <span className="h-4 w-1 animate-pulse rounded-full bg-white" />
                  <span className="h-6 w-1 animate-pulse rounded-full bg-white [animation-delay:150ms]" />
                  <span className="h-3 w-1 animate-pulse rounded-full bg-white [animation-delay:300ms]" />
                  <span className="h-5 w-1 animate-pulse rounded-full bg-white [animation-delay:450ms]" />
                </span>
              </span>
            </div>
          )}
        </div>

        {/* Content Details */}
        <div className="min-w-0">
          <div className="flex flex-wrap items-center gap-2.5">
            <span className="inline-flex items-center gap-1.5 rounded-full bg-accent/15 px-3 py-1 text-xs font-semibold text-accent ring-1 ring-inset ring-accent/30">
              <span className="size-1.5 rounded-full bg-accent" />
              {isPlaying ? 'Now playing' : 'Continue listening'}
            </span>
            {track.genre && (
              <span className="rounded-full bg-surface-elevated px-2.5 py-0.5 text-xs text-secondary">
                {track.genre}
              </span>
            )}
            {track.year && (
              <span className="text-xs tabular-nums text-tertiary">
                Released {track.year}
              </span>
            )}
          </div>

          <h1 className="mt-3 truncate text-2xl font-extrabold tracking-tight text-primary sm:text-3xl lg:text-4xl">
            {track.title}
          </h1>
          <p className="mt-2 truncate text-base font-medium text-secondary">
            {track.artistName ?? 'Unknown artist'}
            <span className="mx-2 text-tertiary">·</span>
            <span className="text-tertiary">{track.albumTitle ?? 'Single'}</span>
          </p>

          {/* Progress Indicator Bar */}
          <div className="mt-6 space-y-2">
            <div className="relative h-2 w-full overflow-hidden rounded-full bg-border/60">
              <div
                className="h-full rounded-full bg-gradient-to-r from-accent to-accent-hover transition-all duration-300"
                style={{ width: `${percent}%` }}
              />
            </div>
            <div className="flex justify-between text-xs tabular-nums text-tertiary">
              <span>{formatDuration(progress)}</span>
              <span className="font-medium text-secondary/80">
                {percent > 0 ? `${Math.round(percent)}% played` : 'Not started'}
              </span>
              <span>{formatDuration(duration)}</span>
            </div>
          </div>

          {/* Controls row */}
          <div className="mt-6 flex flex-wrap items-center gap-4">
            <button
              type="button"
              id="resume-playback-btn"
              onClick={onPlay}
              className="inline-flex items-center gap-2.5 rounded-xl bg-accent px-6 py-3 text-sm font-semibold text-white shadow-lg shadow-accent/20 transition-all hover:bg-accent-hover hover:scale-[1.02] active:scale-[0.98]"
            >
              {isPlaying ? (
                <>
                  <span className="text-base leading-none">⏸</span>
                  <span>Pause track</span>
                </>
              ) : (
                <>
                  <PlayIcon className="size-4 fill-current" />
                  <span>{progress > 0 ? 'Resume track' : 'Play now'}</span>
                </>
              )}
            </button>

            <span className="text-xs text-tertiary">
              Lossless Master Audio · Local Library
            </span>
          </div>
        </div>
      </div>
    </section>
  );
}
