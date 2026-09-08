import type { ReactNode } from 'react';

interface EmptyStateProps {
  icon?: ReactNode;
  title: string;
  description?: string;
  action?: ReactNode;
}

/**
 * Empty states explain what is empty and the next useful action (docs/SCREEN-SPEC.md).
 */
export function EmptyState({ icon, title, description, action }: EmptyStateProps) {
  return (
    <section className="flex flex-col items-center rounded-2xl border border-dashed border-border-strong bg-surface/50 px-6 py-14 text-center">
      {icon && (
        <div aria-hidden className="mb-4 grid size-12 place-items-center rounded-xl bg-accent-soft text-accent">
          {icon}
        </div>
      )}
      <h3 className="text-base font-semibold tracking-tight">{title}</h3>
      {description && (
        <p className="mt-2 max-w-md text-sm leading-relaxed text-secondary">{description}</p>
      )}
      {action && <div className="mt-6">{action}</div>}
    </section>
  );
}
