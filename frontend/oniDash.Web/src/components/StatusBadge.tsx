import type { ReactNode } from 'react';

type Tone = 'success' | 'danger' | 'warning' | 'neutral' | 'accent';

const TONE_CLASSES: Record<Tone, string> = {
  success: 'border-success/30 bg-success/10 text-success',
  danger: 'border-danger/30 bg-danger/10 text-danger',
  warning: 'border-warning/30 bg-warning/10 text-warning',
  neutral: 'border-border bg-surface-hover text-secondary',
  accent: 'border-accent/30 bg-accent-soft text-accent',
};

interface StatusBadgeProps {
  tone: Tone;
  children: ReactNode;
}

export function StatusBadge({ tone, children }: StatusBadgeProps) {
  return (
    <span
      className={`inline-flex items-center gap-1.5 rounded-full border px-2.5 py-1 text-xs font-medium ${TONE_CLASSES[tone]}`}
    >
      <span aria-hidden className="size-1.5 rounded-full bg-current" />
      {children}
    </span>
  );
}
