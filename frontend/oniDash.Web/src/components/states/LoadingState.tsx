interface LoadingStateProps {
  /** Accessible label announced to screen readers while content loads. */
  label?: string;
}

function SkeletonLine({ className }: { className?: string }) {
  return <div aria-hidden className={`animate-pulse rounded-md bg-surface-hover ${className ?? ''}`} />;
}

/**
 * Skeleton placeholder whose layout mirrors the final content (docs/SCREEN-SPEC.md).
 */
export function LoadingState({ label = 'Loading…' }: LoadingStateProps) {
  return (
    <div
      role="status"
      aria-live="polite"
      className="rounded-2xl border border-border bg-surface p-6 shadow-card"
    >
      <span className="sr-only">{label}</span>
      <div className="space-y-4">
        <div className="flex items-center gap-4">
          <SkeletonLine className="size-12 rounded-xl" />
          <div className="flex-1 space-y-2">
            <SkeletonLine className="h-4 w-1/3" />
            <SkeletonLine className="h-3 w-1/2" />
          </div>
        </div>
        <div className="grid gap-3 sm:grid-cols-2">
          <SkeletonLine className="h-14 rounded-lg" />
          <SkeletonLine className="h-14 rounded-lg" />
        </div>
        <SkeletonLine className="h-3 w-2/3" />
      </div>
    </div>
  );
}
