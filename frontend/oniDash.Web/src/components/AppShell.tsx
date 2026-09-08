import { useEffect, useState } from 'react';
import { Outlet, useLocation } from 'react-router-dom';
import { MobileNav, Sidebar } from './Sidebar';
import { TopBar } from './TopBar';
import { pageTitleFor } from './navigation';

/**
 * The application shell owns global navigation and the viewport only — never
 * catalogue business logic (docs/COMPONENT-SPEC.md).
 */
export function AppShell() {
  const [navOpen, setNavOpen] = useState(false);
  const { pathname } = useLocation();

  useEffect(() => {
    setNavOpen(false);
  }, [pathname]);

  useEffect(() => {
    document.title = `${pageTitleFor(pathname)} — oniDash`;
  }, [pathname]);

  return (
    <div className="flex h-dvh overflow-hidden bg-background text-primary">
      <a
        href="#main-content"
        className="sr-only focus:not-sr-only focus:absolute focus:left-4 focus:top-4 focus:z-50 focus:rounded-lg focus:bg-surface-elevated focus:px-4 focus:py-2 focus:text-sm focus:font-medium focus:shadow-pop"
      >
        Skip to content
      </a>

      <Sidebar />
      {navOpen && <MobileNav onClose={() => setNavOpen(false)} />}

      <div className="flex min-w-0 flex-1 flex-col">
        <TopBar onMenu={() => setNavOpen(true)} />
        <main id="main-content" className="flex-1 overflow-y-auto">
          <div className="mx-auto w-full max-w-6xl px-4 py-6 md:px-8 md:py-10 2xl:max-w-7xl">
            <Outlet />
          </div>
        </main>
      </div>
    </div>
  );
}
