import { AlertIcon, RefreshIcon } from '../icons';

interface ErrorStateProps {
  title: string;
  message?: string;
  onRetry?: () => void;
}

/**
 * Errors offer retry and a plain-language description (docs/SCREEN-SPEC.md).
 */
export function ErrorState({ title, message, onRetry }: ErrorStateProps) {
  return (
    <section
      role="alert"
      className="flex flex-col items-center rounded-2xl border border-danger/30 bg-danger/5 px-6 py-12 text-center"
    >
      <div aria-hidden className="mb-4 grid size-12 place-items-center rounded-xl bg-danger/10 text-danger">
        <AlertIcon />
      </div>
      <h3 className="text-base font-semibold tracking-tight">{title}</h3>
      {message && (
        <p className="mt-2 max-w-md text-sm leading-relaxed text-secondary">{message}</p>
      )}
      {onRetry && (
        <button
          type="button"
          onClick={onRetry}
          className="mt-6 inline-flex items-center gap-2 rounded-lg border border-border bg-surface-elevated px-4 py-2 text-sm font-medium text-primary transition-colors hover:bg-surface-hover focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent"
        >
          <RefreshIcon className="size-4" />
          Retry
        </button>
      )}
    </section>
  );
}
