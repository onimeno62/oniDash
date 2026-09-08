import { useEffect } from 'react';
import { Link, NavLink } from 'react-router-dom';
import { NAV_ITEMS } from './navigation';
import { CloseIcon } from './icons';

export function Brand() {
  return (
    <Link
      to="/"
      aria-label="oniDash home"
      className="flex h-16 shrink-0 items-center gap-3 border-b border-border px-4 lg:px-5"
    >
      <span
        aria-hidden
        className="grid size-9 shrink-0 place-items-center rounded-xl bg-gradient-to-br from-accent to-accent-hover font-bold text-white shadow-card"
      >
        o
      </span>
      <span className="hidden text-[15px] font-semibold tracking-tight lg:block">
        oniDash
      </span>
    </Link>
  );
}

interface SidebarLinkProps {
  item: (typeof NAV_ITEMS)[number];
  onNavigate?: () => void;
}

function SidebarLink({ item, onNavigate }: SidebarLinkProps) {
  const Icon = item.icon;
  return (
    <NavLink
      to={item.to}
      end={item.end}
      onClick={onNavigate}
      title={item.label}
      className={({ isActive }) =>
        `flex items-center justify-center gap-3 rounded-lg px-3 py-2.5 text-sm font-medium transition-colors lg:justify-start ${
          isActive
            ? 'bg-accent-soft text-accent'
            : 'text-secondary hover:bg-surface-hover hover:text-primary'
        }`
      }
    >
      <Icon className="shrink-0" />
      <span className="hidden lg:block">{item.label}</span>
    </NavLink>
  );
}

export function Sidebar() {
  return (
    <aside className="hidden w-[76px] shrink-0 flex-col border-r border-border bg-surface md:flex lg:w-64">
      <Brand />
      <nav aria-label="Primary" className="flex-1 space-y-1 overflow-y-auto px-2 py-4 lg:px-3">
        {NAV_ITEMS.map((item) => (
          <SidebarLink key={item.to} item={item} />
        ))}
      </nav>
      <div className="hidden px-5 pb-5 lg:block">
        <p className="text-xs text-muted">oniDash v0.6.0</p>
        <p className="text-xs text-muted">Phase 6 — Movies</p>
      </div>
    </aside>
  );
}

interface MobileNavProps {
  onClose: () => void;
}

/**
 * Off-canvas navigation for narrow viewports (<768px). Conditionally mounted;
 * closes on backdrop click, Escape, or link navigation.
 */
export function MobileNav({ onClose }: MobileNavProps) {
  useEffect(() => {
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        onClose();
      }
    };
    window.addEventListener('keydown', onKeyDown);
    return () => window.removeEventListener('keydown', onKeyDown);
  }, [onClose]);

  return (
    <div className="fixed inset-0 z-50 md:hidden">
      <button
        type="button"
        aria-label="Close navigation"
        onClick={onClose}
        className="animate-fade-in absolute inset-0 h-full w-full cursor-default bg-black/55 backdrop-blur-sm"
      />
      <div className="animate-drawer-in absolute inset-y-0 left-0 flex w-72 max-w-[85vw] flex-col border-r border-border bg-surface shadow-pop">
        <div className="flex h-16 items-center justify-between border-b border-border pl-1 pr-3">
          <Brand />
          <button type="button" aria-label="Close navigation" onClick={onClose} className="icon-btn">
            <CloseIcon />
          </button>
        </div>
        <nav aria-label="Primary" className="flex-1 space-y-1 overflow-y-auto px-3 py-4">
          {NAV_ITEMS.map((item) => (
            <SidebarLink key={item.to} item={item} onNavigate={onClose} />
          ))}
        </nav>
      </div>
    </div>
  );
}
